import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, Subscription, forkJoin, of } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { StudentEvaluationService } from '../../core/services/student-evaluation.service';
import { StudentService } from '../../core/services/student.service';
import { TeacherService } from '../../core/services/teacher.service';
import { SubjectService } from '../../core/services/subject.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { HEALTH_STATUSES, HealthStatus, StudentEvaluation } from '../../core/models/attendance.model';
import { Student } from '../../core/models/student.model';
import { Teacher } from '../../core/models/teacher.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';

/** Averages of a set of reviews (ratings 1..5) */
interface Scores {
  count: number;
  focus: number;
  interaction: number;
  behavior: number;
  /** Mean of the three */
  overall: number;
  tired: number;
  sick: number;
}

interface NamedScores extends Scores {
  key: string;
  name: string;
  detail: string;
}

interface ReviewLine {
  e: StudentEvaluation;
  student: string;
  className: string;
  subject: string;
  teacher: string;
  overall: number;
}

type QuickRange = '' | 'thisWeek' | 'thisMonth' | 'lastMonth' | 'thisYear';

function iso(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function overallOf(e: StudentEvaluation): number {
  return (e.focus + e.interaction + e.behavior) / 3;
}

function scores(list: StudentEvaluation[]): Scores {
  const n = list.length;
  const avg = (pick: (e: StudentEvaluation) => number) => (n ? list.reduce((s, e) => s + pick(e), 0) / n : 0);
  return {
    count: n,
    focus: avg(e => e.focus),
    interaction: avg(e => e.interaction),
    behavior: avg(e => e.behavior),
    overall: avg(overallOf),
    tired: list.filter(e => e.healthStatus === HealthStatus.Tired).length,
    sick: list.filter(e => e.healthStatus === HealthStatus.Sick).length
  };
}

/**
 * Report: students' reviews — /reports/reviews
 * The teachers' lesson evaluations of a period (focus / interaction / behaviour 1..5, health, comment), by the
 * students' class, teacher and subject: averages, the spread of ratings, by subject / teacher / student (lowest
 * first, "needs support" threshold) and the latest comments; Excel export (every review) and print.
 */
@Component({
  selector: 'app-reviews-report',
  standalone: false,
  styleUrl: './reviews-report.component.scss',
  templateUrl: './reviews-report.component.html',
})
export class ReviewsReportComponent implements OnInit, OnDestroy {
  isLoading = signal(true);
  isLoadingReviews = signal(false);
  errorMessage = signal<string | null>(null);
  currentLanguage = signal('en');

  private reviews = signal<StudentEvaluation[]>([]);
  private studentMap = signal<Map<string, Student>>(new Map());
  teachers = signal<Teacher[]>([]);
  subjects = signal<SchoolSubject[]>([]);
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  sections = signal<Section[]>([]);

  dateFrom = signal('');
  dateTo = signal('');
  quickRange = signal<QuickRange>('thisMonth');
  stageFilter = signal('');
  gradeFilter = signal('');
  sectionFilter = signal('');
  teacherFilter = signal('');
  subjectFilter = signal('');
  searchTerm = signal('');
  /** Students whose average is below this "need support" */
  threshold = signal(3);
  supportOnly = signal(false);

  readonly thresholds = [4, 3.5, 3, 2.5, 2];
  readonly ratings = [1, 2, 3, 4, 5];
  readonly generatedOn = new Date();
  private destroy$ = new Subject<void>();
  private reviewsSub?: Subscription;

  filterGrades = computed(() => this.grades().filter(g => !this.stageFilter() || g.stageId === this.stageFilter()));
  filterSections = computed(() => (this.gradeFilter() ? this.sections().filter(s => s.gradeId === this.gradeFilter()) : []));

  /** The reviews after the filters (class = the student's current class) */
  filtered = computed(() => {
    const students = this.studentMap();
    const stage = this.stageFilter();
    const grade = this.gradeFilter();
    const section = this.sectionFilter();
    const teacher = this.teacherFilter();
    const subject = this.subjectFilter();
    const stageGrades = stage ? new Set(this.filterGrades().map(g => g.id)) : null;
    return this.reviews().filter(e => {
      const s = students.get(e.studentId);
      if (stageGrades && !(s && stageGrades.has(s.gradeId))) return false;
      if (grade && s?.gradeId !== grade) return false;
      if (section && s?.sectionId !== section) return false;
      if (teacher && e.teacherId !== teacher) return false;
      if (subject && e.subjectId !== subject) return false;
      return true;
    });
  });

  summary = computed(() => ({ ...scores(this.filtered()), students: new Set(this.filtered().map(e => e.studentId)).size }));

  /** How many reviews gave each rating 1..5, per criterion */
  distribution = computed(() => {
    const list = this.filtered();
    const row = (key: string, pick: (e: StudentEvaluation) => number) => ({
      key,
      counts: this.ratings.map(r => list.filter(e => pick(e) === r).length)
    });
    return [row('focus', e => e.focus), row('interaction', e => e.interaction), row('behavior', e => e.behavior)];
  });

  bySubject = computed<NamedScores[]>(() =>
    this.group(e => e.subjectId, id => this.subjectName(id), () => '').sort((a, b) => b.count - a.count));

  byTeacher = computed<NamedScores[]>(() =>
    this.group(e => e.teacherId, id => this.teacherName(id), () => '').sort((a, b) => b.count - a.count));

  /** Per student, lowest average first */
  byStudent = computed<NamedScores[]>(() =>
    this.group(e => e.studentId, id => this.studentName(id), id => this.className(id)).sort((a, b) => a.overall - b.overall));

  visibleStudents = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const limit = this.threshold();
    return this.byStudent().filter(s =>
      (!this.supportOnly() || s.overall < limit) && (!term || `${s.name} ${s.detail}`.toLowerCase().includes(term)));
  });

  supportCount = computed(() => this.byStudent().filter(s => s.overall < this.threshold()).length);

  /** Latest reviews with a teacher's comment */
  latestComments = computed<ReviewLine[]>(() =>
    this.filtered().filter(e => (e.comment ?? '').trim()).slice(0, 8).map(e => this.line(e)));

  constructor(
    private evaluationService: StudentEvaluationService,
    private studentService: StudentService,
    private teacherService: TeacherService,
    private subjectService: SubjectService,
    private stageService: StageService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private languageService: LanguageService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$.pipe(takeUntil(this.destroy$)).subscribe(l => this.currentLanguage.set(l));
    this.setQuickRange('thisMonth', false);
    this.loadLookups();
  }

  ngOnDestroy(): void {
    this.reviewsSub?.unsubscribe();
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadLookups(): void {
    const list = <T>(obs: Observable<{ status: number; data: T[] }>) =>
      obs.pipe(
        map(r => (r.status === 200 && r.data ? r.data : [])),
        catchError(() => of([] as T[]))
      );
    forkJoin({
      students: list(this.studentService.getAll()),
      teachers: list(this.teacherService.getAll()),
      subjects: list(this.subjectService.getAll()),
      stages: list(this.stageService.getAllStages()),
      grades: list(this.gradeService.getAllGrades()),
      sections: list(this.sectionService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe(r => {
        this.studentMap.set(new Map(r.students.map(s => [s.id, s])));
        this.teachers.set(r.teachers);
        this.subjects.set(r.subjects);
        this.stages.set(r.stages);
        this.grades.set(r.grades);
        this.sections.set(r.sections);
        this.isLoading.set(false);
        this.loadReviews();
      });
  }

  loadReviews(): void {
    if (!this.dateFrom() || !this.dateTo()) return;
    this.reviewsSub?.unsubscribe();
    this.isLoadingReviews.set(true);
    this.errorMessage.set(null);
    this.reviewsSub = this.evaluationService.getReport(this.dateFrom(), this.dateTo()).subscribe({
      next: r => {
        this.reviews.set(r.status === 200 && r.data ? r.data : []);
        this.isLoadingReviews.set(false);
      },
      error: () => {
        this.reviews.set([]);
        this.isLoadingReviews.set(false);
        this.errorMessage.set(this.translate.instant('reports.reviews.loadFailed'));
      }
    });
  }

  setQuickRange(range: QuickRange, reload = true): void {
    this.quickRange.set(range);
    const now = new Date();
    if (range === 'thisWeek') {
      const start = new Date(now.getFullYear(), now.getMonth(), now.getDate() - ((now.getDay() + 1) % 7));
      this.dateFrom.set(iso(start));
      this.dateTo.set(iso(new Date(start.getFullYear(), start.getMonth(), start.getDate() + 6)));
    } else if (range === 'thisMonth') {
      this.dateFrom.set(iso(new Date(now.getFullYear(), now.getMonth(), 1)));
      this.dateTo.set(iso(new Date(now.getFullYear(), now.getMonth() + 1, 0)));
    } else if (range === 'lastMonth') {
      this.dateFrom.set(iso(new Date(now.getFullYear(), now.getMonth() - 1, 1)));
      this.dateTo.set(iso(new Date(now.getFullYear(), now.getMonth(), 0)));
    } else if (range === 'thisYear') {
      this.dateFrom.set(iso(new Date(now.getFullYear(), 0, 1)));
      this.dateTo.set(iso(new Date(now.getFullYear(), 11, 31)));
    }
    if (reload) this.loadReviews();
  }

  setDate(which: 'from' | 'to', value: string): void {
    this.quickRange.set('');
    (which === 'from' ? this.dateFrom : this.dateTo).set(value);
    this.loadReviews();
  }

  setStage(id: string): void {
    this.stageFilter.set(id);
    if (this.gradeFilter() && !this.filterGrades().some(g => g.id === this.gradeFilter())) {
      this.gradeFilter.set('');
      this.sectionFilter.set('');
    }
  }

  setGrade(id: string): void {
    this.gradeFilter.set(id);
    this.sectionFilter.set('');
  }

  print(): void {
    window.print();
  }

  /** Every review of the selection, as a CSV file for Excel (UTF-8 with BOM) */
  exportCsv(): void {
    const t = (key: string) => this.translate.instant(key);
    const header = [
      t('reports.reviews.date'), t('reports.reviews.student'), t('reports.reviews.class'), t('reports.reviews.subject'),
      t('reports.reviews.teacher'), t('reports.reviews.focus'), t('reports.reviews.interaction'), t('reports.reviews.behavior'),
      t('reports.reviews.average'), t('reports.reviews.health'), t('reports.reviews.comment')
    ];
    const lines = this.filtered().map(e => {
      const l = this.line(e);
      return [e.date, l.student, l.className, l.subject, l.teacher, e.focus, e.interaction, e.behavior, l.overall.toFixed(1),
        this.healthLabel(e.healthStatus), e.comment ?? ''];
    });
    const cell = (v: unknown) => {
      const s = String(v ?? '');
      return /[",\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
    };
    const csv = '﻿' + [header, ...lines].map(row => row.map(cell).join(',')).join('\r\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = `reviews-${this.dateFrom()}_${this.dateTo()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  /** Green from 4, amber from 3, red below */
  scoreClass(score: number): string {
    return score >= 4 ? 'good' : score >= 3 ? 'warn' : 'bad';
  }

  pct(part: number, total: number): number {
    return total > 0 ? (part * 100) / total : 0;
  }

  fixed(n: number): string {
    return n ? n.toFixed(1) : '-';
  }

  healthLabel(h: HealthStatus): string {
    const key = HEALTH_STATUSES.find(s => s.value === h)?.key;
    return key ? this.translate.instant(key) : '';
  }

  studentName(id: string): string {
    const s = this.studentMap().get(id);
    if (!s) return this.translate.instant('reports.reviews.unknown');
    return (this.currentLanguage() === 'ar' ? s.fullNameAr || s.fullNameEn : s.fullNameEn || s.fullNameAr) || '';
  }

  className(studentId: string): string {
    const s = this.studentMap().get(studentId);
    if (!s) return '';
    const grade = this.grades().find(g => g.id === s.gradeId);
    const section = this.sections().find(x => x.id === s.sectionId);
    return [this.nameOf(grade), this.nameOf(section)].filter(Boolean).join(' / ');
  }

  teacherName(id: string): string {
    const t = this.teachers().find(x => x.id === id);
    if (!t) return this.translate.instant('reports.reviews.unknown');
    return (this.currentLanguage() === 'ar' ? t.fullNameAr || t.fullNameEn : t.fullNameEn || t.fullNameAr) || '';
  }

  subjectName(id: string): string {
    const s = this.subjects().find(x => x.id === id);
    return s ? this.nameOf(s) : this.translate.instant('reports.reviews.unknown');
  }

  nameOf(item: { nameAr: string; nameEn: string } | undefined): string {
    return item ? ((this.currentLanguage() === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr) || '') : '';
  }

  private line(e: StudentEvaluation): ReviewLine {
    return {
      e,
      student: this.studentName(e.studentId),
      className: this.className(e.studentId),
      subject: this.subjectName(e.subjectId),
      teacher: this.teacherName(e.teacherId),
      overall: overallOf(e)
    };
  }

  private group(keyOf: (e: StudentEvaluation) => string, nameOf: (key: string) => string, detailOf: (key: string) => string): NamedScores[] {
    const groups = new Map<string, StudentEvaluation[]>();
    for (const e of this.filtered()) groups.set(keyOf(e), [...(groups.get(keyOf(e)) ?? []), e]);
    return [...groups.entries()].map(([key, list]) => ({ key, name: nameOf(key), detail: detailOf(key), ...scores(list) }));
  }
}
