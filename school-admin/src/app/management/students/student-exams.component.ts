import { Component, OnDestroy, OnInit, computed, input, signal } from '@angular/core';
import { Observable, Subject, forkJoin, of } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { ExamService } from '../../core/services/exam.service';
import { ExamResultService } from '../../core/services/exam-result.service';
import { SubjectService } from '../../core/services/subject.service';
import { Exam, EXAM_TYPES, ExamType } from '../../core/models/exam.model';
import { ExamResult } from '../../core/models/exam-result.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';
import { AcademicYear, AcademicYearSemesters } from '../../core/models/academic-year.model';

/** The student fields the exams need */
export interface ExamsStudent {
  id: string;
  gradeId: string;
  sectionId: string;
  academicYearId: string;
}

interface ExamRow {
  exam: Exam;
  result: ExamResult | null;
  upcoming: boolean;
  /** Score as % of the full mark (null without a result) */
  percent: number | null;
}

/** "yyyy-MM-dd" today (local) */
function todayIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/**
 * Student profile: the exams of the student's class (and the grade's exams for every class) with the student's score:
 * quizzes, monthly, mid-term and final. Filter by academic year / semester / type / upcoming / done.
 * The year defaults to the newest year that has exams of the class (the student's own year field can be stale).
 * <app-student-exams [student]="s" [academicYears]="years" [lang]="lang" />
 */
@Component({
  selector: 'app-student-exams',
  standalone: false,
  styleUrl: './student-exams.component.scss',
  templateUrl: './student-exams.component.html',
})
export class StudentExamsComponent implements OnInit, OnDestroy {
  student = input.required<ExamsStudent>();
  academicYears = input<AcademicYear[]>([]);
  lang = input<string>('en');

  isLoading = signal(true);
  failed = signal(false);
  /** Chosen academic year; null = default (student's year if it has exams, else the newest) */
  private chosenYearId = signal<string | null>(null);
  /** '' = every semester of the year */
  semesterId = signal<string>('');
  /** 0 = every exam type */
  examType = signal<number>(0);
  readonly examTypes = EXAM_TYPES;
  /** all | upcoming | done */
  view = signal<'all' | 'upcoming' | 'done'>('all');

  private exams = signal<Exam[]>([]);
  private results = signal<ExamResult[]>([]);
  private subjects = signal<SchoolSubject[]>([]);
  private destroy$ = new Subject<void>();

  /** Exams of the student's class (every year), plus any exam the student has a result in */
  private classExams = computed(() => {
    const s = this.student();
    const mine = new Set(this.results().filter(r => r.studentId === s.id).map(r => r.examId));
    return this.exams().filter(e =>
      mine.has(e.id) || (e.gradeId === s.gradeId && (!e.sectionId || e.sectionId === s.sectionId)));
  });

  /** Years that have exams of the class, newest first */
  years = computed(() => {
    const ids = new Set(this.classExams().map(e => e.academicYearId));
    const known = this.academicYears().filter(y => ids.has(y.id));
    const unknown = [...ids]
      .filter(id => !known.some(y => y.id === id))
      .map(id => ({ id, name: '-', startDate: '' }) as AcademicYear);
    return [...known, ...unknown].sort((a, b) => (b.startDate || '').localeCompare(a.startDate || ''));
  });

  yearId = computed(() => {
    const chosen = this.chosenYearId();
    const years = this.years();
    if (chosen && years.some(y => y.id === chosen)) return chosen;
    return years.find(y => y.id === this.student().academicYearId)?.id ?? years[0]?.id ?? '';
  });

  semesters = computed(() =>
    AcademicYearSemesters.ordered(this.academicYears().find(y => y.id === this.yearId())));

  /** The student's exams of the chosen year with their results, upcoming first (soonest), then done (newest) */
  private allRows = computed<ExamRow[]>(() => {
    const s = this.student();
    const year = this.yearId();
    const today = todayIso();
    const mine = new Map(this.results().filter(r => r.studentId === s.id).map(r => [r.examId, r]));
    return this.classExams()
      .filter(e => e.academicYearId === year)
      .map(e => {
        const result = mine.get(e.id) ?? null;
        return {
          exam: e,
          result,
          upcoming: (e.examDate || '').substring(0, 10) >= today && !result,
          percent: result && e.weight > 0 ? Math.round((result.score / e.weight) * 100) : null
        };
      })
      .sort((a, b) => {
        if (a.upcoming !== b.upcoming) return a.upcoming ? -1 : 1;
        const byDate = (a.exam.examDate + a.exam.startTime).localeCompare(b.exam.examDate + b.exam.startTime);
        return a.upcoming ? byDate : -byDate;
      });
  });

  /** Rows of the chosen semester (before the type / view filters) */
  private semesterRows = computed(() => {
    const semester = this.semesterId();
    return this.allRows().filter(r => !semester || r.exam.semesterId === semester);
  });

  /** Exam count per type (for the type chips) */
  typeCounts = computed(() => {
    const counts = new Map<number, number>();
    for (const r of this.semesterRows()) counts.set(r.exam.examType, (counts.get(r.exam.examType) ?? 0) + 1);
    return counts;
  });

  /** Table search: subject (Arabic / English), type, month, date, marks, score, passed / failed */
  searchTerm = signal('');

  rows = computed(() => {
    const type = this.examType();
    const view = this.view();
    const term = this.searchTerm().trim().toLowerCase();
    return this.semesterRows().filter(r =>
      (!type || r.exam.examType === type) &&
      (view === 'all' || (view === 'upcoming' ? r.upcoming : !r.upcoming)) &&
      (!term || this.searchText(r).includes(term)));
  });

  /** Everything a row shows, lower-cased, for the search box */
  private searchText(r: ExamRow): string {
    const e = r.exam;
    const subject = this.subjects().find(x => x.id === e.subjectId);
    const status = r.result
      ? (r.result.isPassed ? 'students.exams.passed' : 'students.exams.failed')
      : (r.upcoming ? 'students.exams.upcomingBadge' : 'students.exams.noResult');
    return [
      subject?.nameAr, subject?.nameEn,
      this.translate.instant(this.typeInfo(e).key),
      e.month ? this.translate.instant('exams.months.' + e.month) : '',
      e.examDate, this.date(e.examDate), this.time(e.startTime),
      this.mark(e.weight), r.result ? this.mark(r.result.score) : '',
      this.translate.instant(status),
      e.sectionId ? '' : this.translate.instant('students.exams.wholeGrade')
    ].filter(Boolean).join(' ').toLowerCase();
  }

  onSearch(value: string): void {
    this.searchTerm.set(value);
    this.pageNo.set(1);
  }

  /** Table paging: 7 exams per page by default, the user can choose another size */
  readonly pageSizes = [7, 10, 25, 50];
  pageSize = signal(7);
  private pageNo = signal(1);
  /** The current page, kept in range when a filter shrinks the list */
  page = computed(() => Math.min(this.pageNo(), Math.max(1, Math.ceil(this.rows().length / this.pageSize()))));
  pagedRows = computed(() => {
    const start = (this.page() - 1) * this.pageSize();
    return this.rows().slice(start, start + this.pageSize());
  });

  setPage(page: number): void {
    this.pageNo.set(page);
  }

  setPageSize(size: number | string): void {
    this.pageSize.set(Number(size) || 7);
    this.pageNo.set(1);
  }

  /** Change a filter and go back to the first page */
  setType(type: number): void {
    this.examType.set(type);
    this.pageNo.set(1);
  }

  setView(view: 'all' | 'upcoming' | 'done'): void {
    this.view.set(view);
    this.pageNo.set(1);
  }

  setSemester(id: string): void {
    this.semesterId.set(id);
    this.pageNo.set(1);
  }

  summary = computed(() => {
    const type = this.examType();
    const rows = this.semesterRows().filter(r => !type || r.exam.examType === type);
    const graded = rows.filter(r => r.result);
    const passed = graded.filter(r => r.result!.isPassed);
    const average = graded.length ? Math.round(graded.reduce((s, r) => s + (r.percent ?? 0), 0) / graded.length) : null;
    return {
      total: rows.length,
      upcoming: rows.filter(r => r.upcoming).length,
      graded: graded.length,
      passRate: graded.length ? Math.round((passed.length / graded.length) * 100) : null,
      average
    };
  });

  constructor(
    private examService: ExamService,
    private examResultService: ExamResultService,
    private subjectService: SubjectService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(): void {
    this.isLoading.set(true);
    this.failed.set(false);
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
      results: list(this.examResultService.getAll()),
      subjects: list(this.subjectService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe(r => {
        this.exams.set(r.exams);
        this.results.set(r.results);
        this.subjects.set(r.subjects);
        this.failed.set(failed);
        this.isLoading.set(false);
      });
  }

  selectYear(id: string): void {
    this.chosenYearId.set(id);
    this.semesterId.set('');
    this.pageNo.set(1);
  }

  isMonthly(e: Exam): boolean {
    return e.examType === ExamType.Monthly && !!e.month;
  }

  subjectName(id: string): string {
    const s = this.subjects().find(x => x.id === id);
    if (!s) return '-';
    return this.lang() === 'ar' ? s.nameAr || s.nameEn : s.nameEn || s.nameAr;
  }

  typeInfo(e: Exam) {
    return EXAM_TYPES.find(t => t.value === e.examType) ?? EXAM_TYPES[0];
  }

  date(value: string): string {
    const d = new Date((value || '').substring(0, 10) + 'T00:00:00');
    return isNaN(d.getTime())
      ? value
      : d.toLocaleDateString(this.lang() === 'ar' ? 'ar-EG' : undefined, { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' });
  }

  time(value: string): string {
    return (value || '').substring(0, 5);
  }

  mark(value: number | null | undefined): string {
    return value === null || value === undefined ? '-' : String(Math.round(value * 100) / 100);
  }

  /** Green from 75%, amber from 50%, red below */
  barClass(percent: number): string {
    return percent >= 75 ? 'bg-success' : percent >= 50 ? 'bg-warning' : 'bg-danger';
  }
}
