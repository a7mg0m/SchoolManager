import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, Subscription, forkJoin, of } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { DailyAttendanceService } from '../../core/services/daily-attendance.service';
import { StudentService } from '../../core/services/student.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { AttendanceReportRow, DailyAttendanceStatus } from '../../core/models/attendance.model';
import { Student } from '../../core/models/student.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';
import { AcademicYear } from '../../core/models/academic-year.model';

/** Counts of a set of school days */
interface Tally {
  days: number;
  present: number;
  partial: number;
  absent: number;
  excused: number;
  lateLessons: number;
  absentLessons: number;
  /** (present + partial) / (days - excused), 0..100; null without countable days */
  rate: number | null;
}

interface StudentRow extends Tally {
  studentId: string;
  name: string;
  nationalId: string;
  grade: string;
  section: string;
}

interface ClassRow extends Tally {
  sectionId: string;
  grade: string;
  section: string;
  students: number;
}

interface DayRow extends Tally {
  date: string;
}

type QuickRange = 'thisWeek' | 'thisMonth' | 'lastMonth' | 'thisYear';

function iso(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function tally(rows: AttendanceReportRow[]): Tally {
  const t: Tally = { days: rows.length, present: 0, partial: 0, absent: 0, excused: 0, lateLessons: 0, absentLessons: 0, rate: null };
  for (const r of rows) {
    if (r.status === DailyAttendanceStatus.Present) t.present++;
    else if (r.status === DailyAttendanceStatus.PartiallyPresent) t.partial++;
    else if (r.status === DailyAttendanceStatus.Absent) t.absent++;
    else t.excused++;
    t.lateLessons += r.lateLessons;
    t.absentLessons += r.absentLessons;
  }
  const countable = t.days - t.excused;
  t.rate = countable > 0 ? Math.round(((t.present + t.partial) * 100) / countable) : null;
  return t;
}

/**
 * Report: students attendance — /reports/attendance
 * The recorded school days of a period (this month by default), by academic year / stage / grade / class:
 * the overall rate and day counts, day by day, by class and by student (lowest rate first, with a "needs attention"
 * threshold); export to Excel (CSV) and print.
 */
@Component({
  selector: 'app-attendance-report',
  standalone: false,
  styleUrl: './attendance-report.component.scss',
  templateUrl: './attendance-report.component.html',
})
export class AttendanceReportComponent implements OnInit, OnDestroy {
  isLoading = signal(true);
  isLoadingDays = signal(false);
  errorMessage = signal<string | null>(null);
  currentLanguage = signal('en');

  private rows = signal<AttendanceReportRow[]>([]);
  private students = signal<Map<string, Student>>(new Map());
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  sections = signal<Section[]>([]);
  academicYears = signal<AcademicYear[]>([]);

  dateFrom = signal('');
  dateTo = signal('');
  quickRange = signal<QuickRange | ''>('thisMonth');
  yearFilter = signal('');
  stageFilter = signal('');
  gradeFilter = signal('');
  sectionFilter = signal('');
  searchTerm = signal('');
  /** Students below this rate are "needs attention" */
  threshold = signal(80);
  attentionOnly = signal(false);

  readonly thresholds = [95, 90, 80, 70, 60];
  readonly generatedOn = new Date();
  private destroy$ = new Subject<void>();
  private daysSub?: Subscription;

  filterGrades = computed(() => {
    const stage = this.stageFilter();
    return this.grades().filter(g => !stage || g.stageId === stage);
  });

  filterSections = computed(() => {
    const grade = this.gradeFilter();
    return grade ? this.sections().filter(s => s.gradeId === grade) : [];
  });

  /** Rows of the chosen stage (the server filters year / grade / class / dates) */
  private scoped = computed(() => {
    const stage = this.stageFilter();
    if (!stage || this.gradeFilter()) return this.rows();
    const gradeIds = new Set(this.filterGrades().map(g => g.id));
    return this.rows().filter(r => gradeIds.has(r.gradeId));
  });

  summary = computed(() => ({ ...tally(this.scoped()), students: new Set(this.scoped().map(r => r.studentId)).size }));

  byDay = computed<DayRow[]>(() => {
    const groups = new Map<string, AttendanceReportRow[]>();
    for (const r of this.scoped()) groups.set(r.date, [...(groups.get(r.date) ?? []), r]);
    return [...groups.entries()].map(([date, rows]) => ({ date, ...tally(rows) })).sort((a, b) => b.date.localeCompare(a.date));
  });

  byClass = computed<ClassRow[]>(() => {
    const lang = this.currentLanguage();
    const grades = new Map(this.grades().map(g => [g.id, g]));
    const sections = new Map(this.sections().map(s => [s.id, s]));
    const groups = new Map<string, AttendanceReportRow[]>();
    for (const r of this.scoped()) groups.set(r.sectionId, [...(groups.get(r.sectionId) ?? []), r]);
    return [...groups.entries()]
      .map(([sectionId, rows]) => {
        const section = sections.get(sectionId);
        return {
          sectionId,
          grade: this.nameOf(grades.get(section?.gradeId ?? rows[0].gradeId), lang),
          section: this.nameOf(section, lang) || '-',
          students: new Set(rows.map(r => r.studentId)).size,
          ...tally(rows)
        };
      })
      .sort((a, b) => a.grade.localeCompare(b.grade) || a.section.localeCompare(b.section));
  });

  /** Per student, lowest rate first */
  byStudent = computed<StudentRow[]>(() => {
    const lang = this.currentLanguage();
    const students = this.students();
    const grades = new Map(this.grades().map(g => [g.id, g]));
    const sections = new Map(this.sections().map(s => [s.id, s]));
    const groups = new Map<string, AttendanceReportRow[]>();
    for (const r of this.scoped()) groups.set(r.studentId, [...(groups.get(r.studentId) ?? []), r]);
    return [...groups.entries()]
      .map(([studentId, rows]) => {
        const s = students.get(studentId);
        const last = rows[rows.length - 1];
        return {
          studentId,
          name: s ? ((lang === 'ar' ? s.fullNameAr || s.fullNameEn : s.fullNameEn || s.fullNameAr) || '') : this.translate.instant('reports.attendance.unknownStudent'),
          nationalId: s?.nationalId ?? '',
          grade: this.nameOf(grades.get(last.gradeId), lang),
          section: this.nameOf(sections.get(last.sectionId), lang),
          ...tally(rows)
        };
      })
      .sort((a, b) => (a.rate ?? 101) - (b.rate ?? 101) || a.name.localeCompare(b.name));
  });

  visibleStudents = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const limit = this.threshold();
    const onlyAttention = this.attentionOnly();
    return this.byStudent().filter(s => {
      if (onlyAttention && !(s.rate !== null && s.rate < limit)) return false;
      return !term || `${s.name} ${s.nationalId} ${s.grade} ${s.section}`.toLowerCase().includes(term);
    });
  });

  attentionCount = computed(() => this.byStudent().filter(s => s.rate !== null && s.rate < this.threshold()).length);

  constructor(
    private attendanceService: DailyAttendanceService,
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
    this.setQuickRange('thisMonth', false);
    this.loadLookups();
  }

  ngOnDestroy(): void {
    this.daysSub?.unsubscribe();
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadLookups(): void {
    this.isLoading.set(true);
    const list = <T>(obs: Observable<{ status: number; data: T[] }>) =>
      obs.pipe(
        map(r => (r.status === 200 && r.data ? r.data : [])),
        catchError(() => of([] as T[]))
      );
    forkJoin({
      students: list(this.studentService.getAll()),
      stages: list(this.stageService.getAllStages()),
      grades: list(this.gradeService.getAllGrades()),
      sections: list(this.sectionService.getAll()),
      years: list(this.academicYearService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe(r => {
        this.students.set(new Map(r.students.map(s => [s.id, s])));
        this.stages.set(r.stages);
        this.grades.set(r.grades);
        this.sections.set(r.sections);
        this.academicYears.set(r.years);
        this.isLoading.set(false);
        this.loadDays();
      });
  }

  /** The school days of the period / year / grade / class (asked again when one of them changes) */
  loadDays(): void {
    if (!this.dateFrom() || !this.dateTo()) return;
    this.daysSub?.unsubscribe();
    this.isLoadingDays.set(true);
    this.errorMessage.set(null);
    this.daysSub = this.attendanceService
      .getReport({
        from: this.dateFrom(),
        to: this.dateTo(),
        academicYearId: this.yearFilter() || null,
        gradeId: this.gradeFilter() || null,
        sectionId: this.sectionFilter() || null
      })
      .subscribe({
        next: r => {
          this.rows.set(r.status === 200 && r.data ? r.data : []);
          this.isLoadingDays.set(false);
        },
        error: () => {
          this.rows.set([]);
          this.isLoadingDays.set(false);
          this.errorMessage.set(this.translate.instant('reports.attendance.loadFailed'));
        }
      });
  }

  setQuickRange(range: QuickRange, reload = true): void {
    this.quickRange.set(range);
    const now = new Date();
    if (range === 'thisWeek') {
      // School week from Saturday
      const start = new Date(now.getFullYear(), now.getMonth(), now.getDate() - ((now.getDay() + 1) % 7));
      this.dateFrom.set(iso(start));
      this.dateTo.set(iso(new Date(start.getFullYear(), start.getMonth(), start.getDate() + 6)));
    } else if (range === 'thisMonth') {
      this.dateFrom.set(iso(new Date(now.getFullYear(), now.getMonth(), 1)));
      this.dateTo.set(iso(new Date(now.getFullYear(), now.getMonth() + 1, 0)));
    } else if (range === 'lastMonth') {
      this.dateFrom.set(iso(new Date(now.getFullYear(), now.getMonth() - 1, 1)));
      this.dateTo.set(iso(new Date(now.getFullYear(), now.getMonth(), 0)));
    } else {
      this.dateFrom.set(iso(new Date(now.getFullYear(), 0, 1)));
      this.dateTo.set(iso(new Date(now.getFullYear(), 11, 31)));
    }
    if (reload) this.loadDays();
  }

  setDate(which: 'from' | 'to', value: string): void {
    this.quickRange.set('');
    (which === 'from' ? this.dateFrom : this.dateTo).set(value);
    this.loadDays();
  }

  setYear(id: string): void {
    this.yearFilter.set(id);
    this.loadDays();
  }

  setStage(id: string): void {
    this.stageFilter.set(id);
    if (this.gradeFilter() && !this.filterGrades().some(g => g.id === this.gradeFilter())) {
      this.gradeFilter.set('');
      this.sectionFilter.set('');
      this.loadDays();
    }
  }

  setGrade(id: string): void {
    this.gradeFilter.set(id);
    this.sectionFilter.set('');
    this.loadDays();
  }

  setSection(id: string): void {
    this.sectionFilter.set(id);
    this.loadDays();
  }

  print(): void {
    window.print();
  }

  /** The students shown, as a CSV file for Excel (UTF-8 with BOM) */
  exportCsv(): void {
    const t = (key: string) => this.translate.instant(key);
    const header = [
      t('reports.attendance.student'), t('parents.nationalId'), t('parents.grade'), t('parents.section'),
      t('reports.attendance.days'), t('reports.attendance.present'), t('reports.attendance.partial'), t('reports.attendance.absent'),
      t('reports.attendance.excused'), t('reports.attendance.lateLessons'), t('reports.attendance.absentLessons'), t('reports.attendance.rate')
    ];
    const lines = this.visibleStudents().map(s => [
      s.name, s.nationalId, s.grade, s.section, s.days, s.present, s.partial, s.absent, s.excused, s.lateLessons, s.absentLessons,
      s.rate === null ? '' : `${s.rate}%`
    ]);
    const cell = (v: unknown) => {
      const s = String(v ?? '');
      return /[",\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
    };
    const csv = '﻿' + [header, ...lines].map(row => row.map(cell).join(',')).join('\r\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = `attendance-${this.dateFrom()}_${this.dateTo()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  /** Green from 90%, amber from the threshold, red below */
  rateClass(rate: number | null): string {
    if (rate === null) return 'none';
    if (rate >= 90) return 'good';
    return rate >= this.threshold() ? 'warn' : 'bad';
  }

  /** Share of the day count, for the stacked bars */
  pct(part: number, total: number): number {
    return total > 0 ? (part * 100) / total : 0;
  }

  dayLabel(date: string): string {
    const d = new Date(date + 'T00:00:00');
    return isNaN(d.getTime())
      ? date
      : d.toLocaleDateString(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-GB', { weekday: 'short', day: 'numeric', month: 'short' });
  }

  nameOf(item: { nameAr: string; nameEn: string } | undefined, lang = this.currentLanguage()): string {
    return item ? ((lang === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr) || '') : '';
  }
}
