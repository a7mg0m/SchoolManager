import { Component, OnDestroy, OnInit, computed, input, signal } from '@angular/core';
import { Observable, Subject, forkJoin, of } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { TimetableService } from '../../core/services/timetable.service';
import { SubjectService } from '../../core/services/subject.service';
import { TeacherService } from '../../core/services/teacher.service';
import { DailyAttendanceService } from '../../core/services/daily-attendance.service';
import { StudentEvaluationService } from '../../core/services/student-evaluation.service';
import { Timetable, TimetableSlot } from '../../core/models/timetable.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';
import { Teacher } from '../../core/models/teacher.model';
import { AcademicYear, AcademicYearSemesters } from '../../core/models/academic-year.model';
import {
  DAILY_ATTENDANCE_STATUSES,
  DailyAttendance,
  EVALUATION_RATINGS,
  HEALTH_STATUSES,
  HealthStatus,
  PERIOD_ATTENDANCE_STATUSES,
  PeriodAttendance,
  PeriodAttendanceStatus,
  StudentEvaluation
} from '../../core/models/attendance.model';

/** The student fields the week needs */
export interface WeekStudent {
  id: string;
  gradeId: string;
  sectionId: string;
  academicYearId: string;
}

/** One lesson of a day: the timetable slot, what was recorded in it and the teacher's review */
interface WeekCell {
  slotId: string;
  startTime: string;
  endTime: string;
  subjectId: string;
  teacherId: string;
  attendance: PeriodAttendance | null;
  evaluation: StudentEvaluation | null;
}

interface WeekDay {
  /** "yyyy-MM-dd" */
  date: string;
  /** 0 = Sunday */
  weekday: number;
  daily: DailyAttendance | null;
  cells: WeekCell[];
}

const DAY_KEYS = [
  'timetables.days.sunday', 'timetables.days.monday', 'timetables.days.tuesday', 'timetables.days.wednesday',
  'timetables.days.thursday', 'timetables.days.friday', 'timetables.days.saturday'
];

/** "yyyy-MM-dd" in local time */
function iso(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function addDays(d: Date, days: number): Date {
  const r = new Date(d);
  r.setDate(r.getDate() + days);
  return r;
}

/** Sunday of the week of a date */
function weekStartOf(d: Date): Date {
  const r = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  r.setDate(r.getDate() - r.getDay());
  return r;
}

/**
 * Student profile: one week of the student's school days — for every lesson of the class timetable, the
 * attendance recorded by the teacher and the teacher's review (focus / interaction / behavior, health, comment).
 * <app-student-week [student]="s" [academicYears]="years" [lang]="lang" />
 */
@Component({
  selector: 'app-student-week',
  standalone: false,
  styleUrl: './student-week.component.scss',
  templateUrl: './student-week.component.html',
})
export class StudentWeekComponent implements OnInit, OnDestroy {
  student = input.required<WeekStudent>();
  academicYears = input<AcademicYear[]>([]);
  lang = input<string>('en');

  weekStart = signal<Date>(weekStartOf(new Date()));
  isLoading = signal(false);
  failed = signal(false);

  private timetables = signal<Timetable[]>([]);
  private subjects = signal<SchoolSubject[]>([]);
  private teachers = signal<Teacher[]>([]);
  private attendance = signal<DailyAttendance[]>([]);
  private evaluations = signal<StudentEvaluation[]>([]);

  readonly dayKeys = DAY_KEYS;
  private destroy$ = new Subject<void>();
  private weekLoad$ = new Subject<void>();

  weekEnd = computed(() => addDays(this.weekStart(), 6));
  isThisWeek = computed(() => iso(this.weekStart()) === iso(weekStartOf(new Date())));

  /** The class timetable in force that week: the semester covering it, else one without a semester */
  private timetable = computed<Timetable | null>(() => {
    const s = this.student();
    const list = this.timetables().filter(t => t.gradeId === s.gradeId && t.academicYearId === s.academicYearId);
    if (!list.length) return null;
    const year = this.academicYears().find(y => y.id === s.academicYearId);
    const mid = iso(addDays(this.weekStart(), 3));
    const semester = AcademicYearSemesters.ordered(year)
      .find(x => x.startDate.substring(0, 10) <= mid && mid <= x.endDate.substring(0, 10));
    return list.find(t => semester && t.semesterId === semester.id)
      ?? list.find(t => !t.semesterId)
      ?? list[0];
  });

  /** The week's days: those with lessons in the timetable or something recorded (else Sunday → Thursday) */
  days = computed<WeekDay[]>(() => {
    const s = this.student();
    const slots = (this.timetable()?.slots ?? []).filter(x => x.sectionId === s.sectionId);
    const start = this.weekStart();

    const all = Array.from({ length: 7 }, (_, i) => {
      const date = iso(addDays(start, i));
      const weekday = addDays(start, i).getDay();
      const daily = this.attendance().find(a => a.date.substring(0, 10) === date) ?? null;
      const evaluations = this.evaluations().filter(e => e.date.substring(0, 10) === date);
      const periods = daily?.periods ?? [];

      // The timetable's lessons of that weekday, plus lessons recorded that are no longer in it
      const cells = new Map<string, WeekCell>();
      for (const slot of slots.filter(x => x.day === weekday)) {
        cells.set(slot.id ?? '', this.cell(slot.id ?? '', slot.startTime, slot.endTime, slot.subjectId, slot.teacherId));
      }
      for (const p of periods) {
        if (!cells.has(p.timetableSlotId)) cells.set(p.timetableSlotId, this.cell(p.timetableSlotId, p.startTime, '', p.subjectId, p.teacherId));
      }
      for (const e of evaluations) {
        if (!cells.has(e.timetableSlotId)) cells.set(e.timetableSlotId, this.cell(e.timetableSlotId, '', '', e.subjectId, e.teacherId));
      }
      for (const c of cells.values()) {
        c.attendance = periods.find(p => p.timetableSlotId === c.slotId) ?? null;
        c.evaluation = evaluations.find(e => e.timetableSlotId === c.slotId) ?? null;
      }

      return {
        date,
        weekday,
        daily,
        cells: [...cells.values()].sort((a, b) => (a.startTime || '99').localeCompare(b.startTime || '99'))
      };
    });

    const used = all.filter(d => d.cells.length || d.daily);
    return used.length ? used : all.filter(d => d.weekday <= 4);
  });

  /** Number of period columns (the busiest day) */
  periodColumns = computed(() => Array.from({ length: Math.max(1, ...this.days().map(d => d.cells.length)) }, (_, i) => i));

  /** Week summary: lessons recorded / attended / absent, and the average review */
  summary = computed(() => {
    const cells = this.days().flatMap(d => d.cells);
    const recorded = cells.filter(c => c.attendance);
    const attended = recorded.filter(c => c.attendance!.status === PeriodAttendanceStatus.Present || c.attendance!.status === PeriodAttendanceStatus.Late);
    const absent = recorded.filter(c => c.attendance!.status === PeriodAttendanceStatus.Absent);
    const reviews = cells.map(c => c.evaluation).filter((e): e is StudentEvaluation => !!e);
    const average = reviews.length
      ? reviews.reduce((sum, e) => sum + (e.focus + e.interaction + e.behavior) / 3, 0) / reviews.length
      : null;
    return {
      lessons: cells.length,
      recorded: recorded.length,
      rate: recorded.length ? Math.round((attended.length / recorded.length) * 100) : null,
      absent: absent.length,
      reviews: reviews.length,
      average: average === null ? null : Math.round(average * 10) / 10
    };
  });

  constructor(
    private timetableService: TimetableService,
    private subjectService: SubjectService,
    private teacherService: TeacherService,
    private attendanceService: DailyAttendanceService,
    private evaluationService: StudentEvaluationService
  ) {}

  ngOnInit(): void {
    const list = <T>(obs: Observable<{ status: number; data: T[] }>) =>
      obs.pipe(map(r => (r.status === 200 && r.data ? r.data : [])), catchError(() => of([] as T[])));

    forkJoin({
      timetables: list(this.timetableService.getAll()),
      subjects: list(this.subjectService.getAll()),
      teachers: list(this.teacherService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe(r => {
        this.timetables.set(r.timetables);
        this.subjects.set(r.subjects);
        this.teachers.set(r.teachers);
      });

    this.loadWeek();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.weekLoad$.complete();
  }

  previousWeek(): void {
    this.weekStart.set(addDays(this.weekStart(), -7));
    this.loadWeek();
  }

  nextWeek(): void {
    this.weekStart.set(addDays(this.weekStart(), 7));
    this.loadWeek();
  }

  thisWeek(): void {
    this.weekStart.set(weekStartOf(new Date()));
    this.loadWeek();
  }

  /** Attendance and reviews of the shown week (an older request still running is dropped) */
  loadWeek(): void {
    this.weekLoad$.next();
    this.isLoading.set(true);
    this.failed.set(false);
    const id = this.student().id;
    const from = iso(this.weekStart());
    const to = iso(this.weekEnd());

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
      attendance: list(this.attendanceService.getByStudent(id, from, to)),
      evaluations: list(this.evaluationService.getByStudent(id, from, to))
    })
      .pipe(takeUntil(this.weekLoad$), takeUntil(this.destroy$))
      .subscribe(r => {
        this.attendance.set(r.attendance);
        this.evaluations.set(r.evaluations);
        this.failed.set(failed);
        this.isLoading.set(false);
      });
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  private cell(slotId: string, startTime: string, endTime: string, subjectId: string, teacherId: string): WeekCell {
    return { slotId, startTime, endTime, subjectId, teacherId, attendance: null, evaluation: null };
  }

  subjectName(id: string): string {
    const s = this.subjects().find(x => x.id === id);
    if (!s) return '-';
    return this.lang() === 'ar' ? s.nameAr || s.nameEn : s.nameEn || s.nameAr;
  }

  teacherName(id: string): string {
    const t = this.teachers().find(x => x.id === id);
    if (!t) return '';
    return this.lang() === 'ar' ? t.fullNameAr || t.fullNameEn : t.fullNameEn || t.fullNameAr;
  }

  periodStatus(c: WeekCell) {
    return c.attendance ? PERIOD_ATTENDANCE_STATUSES.find(s => s.value === c.attendance!.status) ?? null : null;
  }

  dailyStatus(d: WeekDay) {
    return d.daily ? DAILY_ATTENDANCE_STATUSES.find(s => s.value === d.daily!.status) ?? null : null;
  }

  ratingInfo(value: number) {
    return EVALUATION_RATINGS.find(r => r.value === value) ?? EVALUATION_RATINGS[0];
  }

  healthInfo(e: StudentEvaluation) {
    return HEALTH_STATUSES.find(h => h.value === e.healthStatus) ?? HEALTH_STATUSES[0];
  }

  isUnwell(e: StudentEvaluation): boolean {
    return e.healthStatus !== HealthStatus.Good;
  }

  isToday(date: string): boolean {
    return date === iso(new Date());
  }

  isFuture(date: string): boolean {
    return date > iso(new Date());
  }

  time(value: string | null | undefined): string {
    return (value || '').substring(0, 5);
  }

  dayDate(date: string): string {
    const d = new Date(date + 'T00:00:00');
    return d.toLocaleDateString(this.lang() === 'ar' ? 'ar-EG' : undefined, { day: 'numeric', month: 'short' });
  }

  rangeLabel(): string {
    const opts: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short', year: 'numeric' };
    const locale = this.lang() === 'ar' ? 'ar-EG' : undefined;
    return `${this.weekStart().toLocaleDateString(locale, opts)} – ${this.weekEnd().toLocaleDateString(locale, opts)}`;
  }
}
