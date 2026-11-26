import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, forkJoin, of } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { ExamService } from '../../core/services/exam.service';
import { ExamResultService } from '../../core/services/exam-result.service';
import { SubjectService } from '../../core/services/subject.service';
import { StudentService } from '../../core/services/student.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { EXAM_TYPES, Exam } from '../../core/models/exam.model';
import { ExamResult } from '../../core/models/exam-result.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';
import { Student } from '../../core/models/student.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';
import { AcademicYear, AcademicYearSemesters } from '../../core/models/academic-year.model';

/** One student's result in one exam */
interface ResultLine {
  result: ExamResult;
  exam: Exam;
  student: Student | undefined;
  /** Score as % of the full mark */
  percent: number;
}

/** Results of a group (subject / exam / student) */
interface Stats {
  count: number;
  passed: number;
  failed: number;
  /** Average % */
  average: number;
  highest: number;
  lowest: number;
  passRate: number;
}

interface NamedStats extends Stats {
  key: string;
  name: string;
  detail: string;
}

/** Grade bands of a percentage */
const BANDS: { key: string; min: number; css: string }[] = [
  { key: 'excellent', min: 90, css: 'b5' },
  { key: 'veryGood', min: 75, css: 'b4' },
  { key: 'good', min: 65, css: 'b3' },
  { key: 'pass', min: 50, css: 'b2' },
  { key: 'weak', min: 0, css: 'b1' }
];

function stats(lines: ResultLine[]): Stats {
  const n = lines.length;
  const passed = lines.filter(l => l.result.isPassed).length;
  const percents = lines.map(l => l.percent);
  return {
    count: n,
    passed,
    failed: n - passed,
    average: n ? percents.reduce((a, b) => a + b, 0) / n : 0,
    highest: n ? Math.max(...percents) : 0,
    lowest: n ? Math.min(...percents) : 0,
    passRate: n ? Math.round((passed * 100) / n) : 0
  };
}

/**
 * Report: students' exam results — /reports/exam-results
 * Results of the exams of an academic year / semester, by class, subject, exam type and dates: pass rate and average,
 * the grade bands, by subject, by exam and by student (lowest average first); Excel export (every result) and print.
 */
@Component({
  selector: 'app-exam-results-report',
  standalone: false,
  styleUrl: './exam-results-report.component.scss',
  templateUrl: './exam-results-report.component.html',
})
export class ExamResultsReportComponent implements OnInit, OnDestroy {
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);
  currentLanguage = signal('en');

  private exams = signal<Exam[]>([]);
  private results = signal<ExamResult[]>([]);
  private studentMap = signal<Map<string, Student>>(new Map());
  subjects = signal<SchoolSubject[]>([]);
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  sections = signal<Section[]>([]);
  academicYears = signal<AcademicYear[]>([]);

  yearFilter = signal('');
  semesterFilter = signal('');
  stageFilter = signal('');
  gradeFilter = signal('');
  sectionFilter = signal('');
  subjectFilter = signal('');
  /** 0 = every type */
  typeFilter = signal(0);
  dateFrom = signal('');
  dateTo = signal('');
  searchTerm = signal('');
  /** Students whose average % is below this "need support" */
  threshold = signal(50);
  supportOnly = signal(false);

  readonly examTypes = EXAM_TYPES;
  readonly bands = BANDS;
  readonly thresholds = [75, 65, 60, 50, 40];
  readonly generatedOn = new Date();
  private destroy$ = new Subject<void>();

  semesters = computed(() => AcademicYearSemesters.ordered(this.academicYears().find(y => y.id === this.yearFilter())));
  filterGrades = computed(() => this.grades().filter(g => !this.stageFilter() || g.stageId === this.stageFilter()));
  filterSections = computed(() => (this.gradeFilter() ? this.sections().filter(s => s.gradeId === this.gradeFilter()) : []));
  /** Subjects of the chosen grade (all when none) */
  filterSubjects = computed(() => this.subjects().filter(s => !this.gradeFilter() || s.gradeId === this.gradeFilter()));

  /** Every result after the filters */
  lines = computed<ResultLine[]>(() => {
    const exams = new Map(this.exams().map(e => [e.id, e]));
    const students = this.studentMap();
    const year = this.yearFilter();
    const semester = this.semesterFilter();
    const stage = this.stageFilter();
    const grade = this.gradeFilter();
    const section = this.sectionFilter();
    const subject = this.subjectFilter();
    const type = this.typeFilter();
    const from = this.dateFrom();
    const to = this.dateTo();
    const term = this.searchTerm().trim().toLowerCase();
    const stageGrades = stage ? new Set(this.filterGrades().map(g => g.id)) : null;
    const out: ResultLine[] = [];
    for (const r of this.results()) {
      const e = exams.get(r.examId);
      if (!e) continue;
      const d = (e.examDate || '').slice(0, 10);
      if (year && e.academicYearId !== year) continue;
      if (semester && e.semesterId !== semester) continue;
      if (stageGrades && !stageGrades.has(e.gradeId)) continue;
      if (grade && e.gradeId !== grade) continue;
      const s = students.get(r.studentId);
      // A class: its own exams, and the grade's exams for every class (the student's class)
      if (section && (e.sectionId ? e.sectionId !== section : s?.sectionId !== section)) continue;
      if (subject && e.subjectId !== subject) continue;
      if (type && e.examType !== type) continue;
      if (from && d < from) continue;
      if (to && d > to) continue;
      if (term) {
        const text = [s?.fullNameAr, s?.fullNameEn, s?.nationalId, this.subjectName(e.subjectId)].join(' ').toLowerCase();
        if (!text.includes(term)) continue;
      }
      out.push({ result: r, exam: e, student: s, percent: e.weight > 0 ? (r.score * 100) / e.weight : 0 });
    }
    return out;
  });

  summary = computed(() => ({
    ...stats(this.lines()),
    exams: new Set(this.lines().map(l => l.exam.id)).size,
    students: new Set(this.lines().map(l => l.result.studentId)).size
  }));

  bandCounts = computed(() => {
    const counts = BANDS.map(() => 0);
    for (const l of this.lines()) counts[BANDS.findIndex(b => l.percent >= b.min)]++;
    return counts;
  });

  bySubject = computed<NamedStats[]>(() =>
    this.group(l => l.exam.subjectId, l => this.subjectName(l.exam.subjectId), () => '').sort((a, b) => b.count - a.count));

  /** Per exam, newest first */
  byExam = computed<NamedStats[]>(() =>
    this.group(
      l => l.exam.id,
      l => this.subjectName(l.exam.subjectId),
      l => [this.examDate(l.exam), this.typeLabel(l.exam), this.examClass(l.exam)].filter(Boolean).join(' · ')
    ).sort((a, b) => b.detail.localeCompare(a.detail)));

  /** Per student, lowest average first */
  byStudent = computed<NamedStats[]>(() =>
    this.group(l => l.result.studentId, l => this.studentName(l.student), l => this.studentClass(l.student))
      .sort((a, b) => a.average - b.average));

  visibleStudents = computed(() => this.byStudent().filter(s => !this.supportOnly() || s.average < this.threshold()));
  supportCount = computed(() => this.byStudent().filter(s => s.average < this.threshold()).length);

  hasFilters = computed(() =>
    !!(this.yearFilter() || this.semesterFilter() || this.stageFilter() || this.gradeFilter() || this.sectionFilter()
      || this.subjectFilter() || this.typeFilter() || this.dateFrom() || this.dateTo() || this.searchTerm()));

  constructor(
    private examService: ExamService,
    private resultService: ExamResultService,
    private subjectService: SubjectService,
    private studentService: StudentService,
    private stageService: StageService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private academicYearService: AcademicYearService,
    private languageService: LanguageService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$.pipe(takeUntil(this.destroy$)).subscribe(l => this.currentLanguage.set(l));
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    let failed = false;
    const list = <T>(obs: Observable<{ status: number; data: T[] }>) =>
      obs.pipe(
        map(r => (r.status === 200 && r.data ? r.data : [])),
        catchError(() => {
          failed = true;
          return of([] as T[]);
        })
      );
    forkJoin({
      exams: list(this.examService.getAll()),
      results: list(this.resultService.getAll()),
      subjects: list(this.subjectService.getAll()),
      students: list(this.studentService.getAll()),
      stages: list(this.stageService.getAllStages()),
      grades: list(this.gradeService.getAllGrades()),
      sections: list(this.sectionService.getAll()),
      years: list(this.academicYearService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe(r => {
        this.exams.set(r.exams);
        this.results.set(r.results);
        this.subjects.set(r.subjects);
        this.studentMap.set(new Map(r.students.map(s => [s.id, s])));
        this.stages.set(r.stages);
        this.grades.set(r.grades);
        this.sections.set(r.sections);
        this.academicYears.set(r.years);
        // The newest year by default
        if (!this.yearFilter() && r.years.length) {
          const newest = [...r.years].sort((a, b) => (b.startDate || '').localeCompare(a.startDate || ''))[0];
          if (r.exams.some(e => e.academicYearId === newest.id)) this.yearFilter.set(newest.id);
        }
        if (failed) this.errorMessage.set(this.translate.instant('reports.loadFailed'));
        this.isLoading.set(false);
      });
  }

  setYear(id: string): void {
    this.yearFilter.set(id);
    this.semesterFilter.set('');
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
    if (this.subjectFilter() && !this.filterSubjects().some(s => s.id === this.subjectFilter())) this.subjectFilter.set('');
  }

  clearFilters(): void {
    this.yearFilter.set('');
    this.semesterFilter.set('');
    this.stageFilter.set('');
    this.gradeFilter.set('');
    this.sectionFilter.set('');
    this.subjectFilter.set('');
    this.typeFilter.set(0);
    this.dateFrom.set('');
    this.dateTo.set('');
    this.searchTerm.set('');
  }

  print(): void {
    window.print();
  }

  /** Every result of the selection, as a CSV file for Excel (UTF-8 with BOM) */
  exportCsv(): void {
    const t = (key: string) => this.translate.instant(key);
    const header = [
      t('reports.examResults.examDate'), t('reports.examResults.subject'), t('reports.examResults.type'), t('reports.examResults.class'),
      t('reports.examResults.student'), t('parents.nationalId'), t('reports.examResults.score'), t('reports.examResults.fullMark'),
      t('reports.examResults.percent'), t('reports.examResults.result')
    ];
    const lines = [...this.lines()]
      .sort((a, b) => (b.exam.examDate || '').localeCompare(a.exam.examDate || ''))
      .map(l => [
        this.examDate(l.exam), this.subjectName(l.exam.subjectId), this.typeLabel(l.exam), this.examClass(l.exam),
        this.studentName(l.student), l.student?.nationalId ?? '', l.result.score, l.exam.weight, `${Math.round(l.percent)}%`,
        t(l.result.isPassed ? 'reports.examResults.passed' : 'reports.examResults.failed')
      ]);
    const cell = (v: unknown) => {
      const s = String(v ?? '');
      return /[",\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
    };
    const csv = '﻿' + [header, ...lines].map(row => row.map(cell).join(',')).join('\r\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = `exam-results-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  /** Green from 75%, amber from 50%, red below */
  scoreClass(percent: number): string {
    return percent >= 75 ? 'good' : percent >= 50 ? 'warn' : 'bad';
  }

  pct(part: number, total: number): number {
    return total > 0 ? (part * 100) / total : 0;
  }

  round(n: number): string {
    return `${Math.round(n)}%`;
  }

  subjectName(id: string): string {
    const s = this.subjects().find(x => x.id === id);
    return s ? this.nameOf(s) : '-';
  }

  typeLabel(e: Exam): string {
    const key = EXAM_TYPES.find(t => t.value === e.examType)?.key;
    const label = key ? this.translate.instant(key) : '';
    return e.month ? `${label} (${this.translate.instant('exams.months.' + e.month)})` : label;
  }

  examDate(e: Exam): string {
    return (e.examDate || '').slice(0, 10);
  }

  examClass(e: Exam): string {
    const grade = this.nameOf(this.grades().find(g => g.id === e.gradeId));
    const section = e.sectionId ? this.nameOf(this.sections().find(s => s.id === e.sectionId)) : this.translate.instant('reports.examResults.wholeGrade');
    return [grade, section].filter(Boolean).join(' / ');
  }

  studentName(s: Student | undefined): string {
    if (!s) return this.translate.instant('reports.reviews.unknown');
    return (this.currentLanguage() === 'ar' ? s.fullNameAr || s.fullNameEn : s.fullNameEn || s.fullNameAr) || '';
  }

  studentClass(s: Student | undefined): string {
    if (!s) return '';
    return [this.nameOf(this.grades().find(g => g.id === s.gradeId)), this.nameOf(this.sections().find(x => x.id === s.sectionId))]
      .filter(Boolean).join(' / ');
  }

  nameOf(item: { nameAr: string; nameEn: string } | undefined): string {
    return item ? ((this.currentLanguage() === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr) || '') : '';
  }

  private group(keyOf: (l: ResultLine) => string, nameOf: (l: ResultLine) => string, detailOf: (l: ResultLine) => string): NamedStats[] {
    const groups = new Map<string, ResultLine[]>();
    for (const l of this.lines()) groups.set(keyOf(l), [...(groups.get(keyOf(l)) ?? []), l]);
    return [...groups.entries()].map(([key, list]) => ({ key, name: nameOf(list[0]), detail: detailOf(list[0]), ...stats(list) }));
  }
}
