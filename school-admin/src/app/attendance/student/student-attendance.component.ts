import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { DailyAttendanceService } from '../../core/services/daily-attendance.service';
import { StudentEvaluationService } from '../../core/services/student-evaluation.service';
import { StudentService } from '../../core/services/student.service';
import { SubjectService } from '../../core/services/subject.service';
import { TeacherService } from '../../core/services/teacher.service';
import {
  DAILY_ATTENDANCE_STATUSES,
  DailyAttendance,
  DailyAttendanceStatus,
  EVALUATION_RATINGS,
  EvaluationRating,
  HEALTH_STATUSES,
  HealthStatus,
  PERIOD_ATTENDANCE_STATUSES,
  PeriodAttendance,
  PeriodAttendanceStatus,
  StudentEvaluation
} from '../../core/models/attendance.model';
import { Student } from '../../core/models/student.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';
import { Teacher } from '../../core/models/teacher.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Observable, Subject, forkJoin, of } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { attendanceApiError } from '../attendance-errors';
import { formatDate, formatTime, todayIso } from '../../notification/shared/audience';

@Component({
  selector: 'app-student-attendance',
  standalone: false,
  styleUrl: './student-attendance.component.scss',
  templateUrl: './student-attendance.component.html',
})
export class StudentAttendanceComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  student = signal<Student | null>(null);
  subjects = signal<SchoolSubject[]>([]);
  teachers = signal<Teacher[]>([]);
  days = signal<DailyAttendance[]>([]);
  evaluations = signal<StudentEvaluation[]>([]);
  isLoading = signal(false);
  isLoadingRange = signal(false);
  errorMessage = signal<string | null>(null);
  currentLanguage = signal<string>('en');

  // Range (default: the current month)
  from = signal(this.monthStart());
  to = signal(todayIso());

  /** Tab: 1 = attendance, 2 = evaluations */
  activeTab = 1;
  /** Days whose lessons are shown */
  expanded = signal<Set<string>>(new Set());

  readonly statuses = DAILY_ATTENDANCE_STATUSES;
  readonly periodStatuses = PERIOD_ATTENDANCE_STATUSES;
  readonly ratings = EVALUATION_RATINGS;
  readonly healthStatuses = HEALTH_STATUSES;
  readonly stars = [1, 2, 3, 4, 5];

  /** Newest day first */
  sortedDays = computed(() => [...this.days()].sort((a, b) => b.date.localeCompare(a.date)));

  /** Newest evaluation first */
  sortedEvaluations = computed(() => [...this.evaluations()].sort((a, b) => b.date.localeCompare(a.date)));

  /** Days per status (summary cards) */
  summary = computed(() => {
    const counts = new Map<DailyAttendanceStatus, number>();
    for (const d of this.days()) counts.set(d.status, (counts.get(d.status) || 0) + 1);
    return this.statuses.map(s => ({ ...s, count: counts.get(s.value) || 0 }));
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private studentId = '';

  constructor(
    private attendanceService: DailyAttendanceService,
    private evaluationService: StudentEvaluationService,
    private studentService: StudentService,
    private subjectService: SubjectService,
    private teacherService: TeacherService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.studentId = this.activatedRoute.snapshot.paramMap.get('studentId') || '';
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Student, subjects and teachers (for names), then the range */
  private loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    // Names are optional: a failing lookup only shows "-"
    const list = <T>(obs: Observable<{ data: T[] }>): Observable<T[]> =>
      obs.pipe(map(r => r?.data || []), catchError(() => of([] as T[])));

    forkJoin({
      student: this.studentService.getById(this.studentId),
      subjects: list(this.subjectService.getAll()),
      teachers: list(this.teacherService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ student, subjects, teachers }) => {
          this.subjects.set(subjects);
          this.teachers.set(teachers);
          if (student.status === 200 && student.data) {
            this.student.set(student.data);
            this.loadRange();
          } else {
            this.errorMessage.set(this.translate.instant('attendance.studentNotFound'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading student attendance:', error);
          this.errorMessage.set(this.translate.instant('attendance.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /** Days and evaluations between from and to */
  loadRange(): void {
    if (!this.from() || !this.to()) return;
    if (this.to() < this.from()) {
      this.errorMessage.set(this.translate.instant('attendance.errors.ToBeforeFrom'));
      return;
    }

    this.isLoadingRange.set(true);
    this.errorMessage.set(null);

    forkJoin({
      days: this.attendanceService.getByStudent(this.studentId, this.from(), this.to()),
      evaluations: this.evaluationService.getByStudent(this.studentId, this.from(), this.to())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ days, evaluations }) => {
          this.days.set(days.status === 200 ? days.data || [] : []);
          this.evaluations.set(evaluations.status === 200 ? evaluations.data || [] : []);
          if (days.status !== 200) {
            this.errorMessage.set(attendanceApiError(this.translate, days.message, 'attendance.loadFailed'));
          } else if (evaluations.status !== 200) {
            this.errorMessage.set(attendanceApiError(this.translate, evaluations.message, 'attendance.loadFailed'));
          }
          this.expanded.set(new Set());
          this.isLoadingRange.set(false);
        },
        error: (error) => {
          console.error('Error loading attendance range:', error);
          this.errorMessage.set(this.translate.instant('attendance.loadFailed'));
          this.isLoadingRange.set(false);
        }
      });
  }

  onFromChange(event: Event): void {
    this.from.set((event.target as HTMLInputElement).value);
    this.loadRange();
  }

  onToChange(event: Event): void {
    this.to.set((event.target as HTMLInputElement).value);
    this.loadRange();
  }

  toggleDay(id: string): void {
    this.expanded.update(set => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  back(): void {
    this.router.navigate(['/attendance/daily']);
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  studentName(): string {
    const s = this.student();
    if (!s) return '';
    return this.currentLanguage() === 'ar' ? (s.fullNameAr || s.fullNameEn) : (s.fullNameEn || s.fullNameAr);
  }

  subjectName(id: string): string {
    const subject = this.subjects().find(s => s.id === id);
    if (!subject) return '-';
    return this.currentLanguage() === 'ar' ? (subject.nameAr || subject.nameEn) : (subject.nameEn || subject.nameAr);
  }

  teacherName(id: string): string {
    const teacher = this.teachers().find(t => t.id === id);
    if (!teacher) return '-';
    return this.currentLanguage() === 'ar' ? (teacher.fullNameAr || teacher.fullNameEn) : (teacher.fullNameEn || teacher.fullNameAr);
  }

  statusInfo(status: DailyAttendanceStatus) {
    return this.statuses.find(s => s.value === status) ?? this.statuses[0];
  }

  periodStatusInfo(status: PeriodAttendanceStatus) {
    return this.periodStatuses.find(s => s.value === status) ?? this.periodStatuses[0];
  }

  ratingInfo(rating: EvaluationRating) {
    return this.ratings.find(r => r.value === rating) ?? this.ratings[0];
  }

  healthInfo(status: HealthStatus) {
    return this.healthStatuses.find(h => h.value === status) ?? this.healthStatuses[0];
  }

  /** Lessons of a day by start time */
  sortedPeriods(day: DailyAttendance): PeriodAttendance[] {
    return [...(day.periods || [])].sort((a, b) => a.startTime.localeCompare(b.startTime));
  }

  day(value: string): string {
    return formatDate(value, { weekday: 'long', year: 'numeric', month: 'short', day: 'numeric' }) || value;
  }

  time(value: string | null | undefined): string {
    return formatTime(value) || '-';
  }

  private monthStart(): string {
    return `${todayIso().substring(0, 8)}01`;
  }
}
