import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { DailyAttendanceService } from '../../core/services/daily-attendance.service';
import { StudentService } from '../../core/services/student.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { AcademicContextService } from '../../core/services/academic-context.service';
import {
  DAILY_ATTENDANCE_STATUSES,
  DailyAttendance,
  DailyAttendanceStatus,
  SetDailyAttendanceRequest
} from '../../core/models/attendance.model';
import { Student } from '../../core/models/student.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Observable, Subject, forkJoin, of, timer } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { attendanceApiError } from '../attendance-errors';
import { formatTime, todayIso, toHourMinute } from '../../notification/shared/audience';

/** A student of the section with their day (null: nothing recorded yet) */
interface StudentDayRow {
  student: Student;
  record: DailyAttendance | null;
}

@Component({
  selector: 'app-daily-attendance-list',
  standalone: false,
  styleUrl: './daily-attendance-list.component.scss',
  templateUrl: './daily-attendance-list.component.html',
})
export class DailyAttendanceListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  sections = signal<Section[]>([]);
  students = signal<Student[]>([]);
  records = signal<DailyAttendance[]>([]);
  isLoading = signal(false);
  isLoadingDay = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  modalError = signal<string | null>(null);
  currentLanguage = signal<string>('en');

  // Filters
  gradeId = signal('');
  sectionId = signal('');
  date = signal(todayIso());
  searchTerm = signal('');

  readonly statuses = DAILY_ATTENDANCE_STATUSES;

  /** Active grades, by stage then name */
  activeGrades = computed(() =>
    this.grades()
      .filter(g => g.isActive !== false)
      .sort((a, b) => this.stageName(a.stageId).localeCompare(this.stageName(b.stageId)) || this.localName(a).localeCompare(this.localName(b))));

  /** Sections of the chosen grade */
  gradeSections = computed(() => this.sections().filter(s => s.gradeId === this.gradeId()));

  /** Every active student of the section with their day on the chosen date */
  rows = computed<StudentDayRow[]>(() => {
    const sectionId = this.sectionId();
    if (!sectionId) return [];
    const byStudent = new Map(this.records().map(r => [r.studentId, r]));
    const term = this.searchTerm().trim().toLowerCase();
    return this.students()
      .filter(s => s.sectionId === sectionId && s.isActive !== false)
      .filter(s => !term || `${s.fullNameAr} ${s.fullNameEn} ${s.nationalId}`.toLowerCase().includes(term))
      .sort((a, b) => this.studentName(a).localeCompare(this.studentName(b)))
      .map(student => ({ student, record: byStudent.get(student.id) ?? null }));
  });

  /** Students of the section without a day yet (for "mark all present") */
  unrecorded = computed(() => {
    const recorded = new Set(this.records().map(r => r.studentId));
    return this.students().filter(s => s.sectionId === this.sectionId() && s.isActive !== false && !recorded.has(s.id));
  });

  /** Count per status (and not recorded) for the summary badges */
  summary = computed(() => {
    const counts = new Map<DailyAttendanceStatus, number>();
    for (const r of this.records()) counts.set(r.status, (counts.get(r.status) || 0) + 1);
    return {
      byStatus: this.statuses.map(s => ({ ...s, count: counts.get(s.value) || 0 })),
      notRecorded: this.unrecorded().length
    };
  });

  /** Set status modal */
  dayForm!: FormGroup;
  checkOutTime = signal('');
  editingRow = signal<StudentDayRow | null>(null);

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private modalRef: NgbModalRef | null = null;

  constructor(
    private formBuilder: FormBuilder,
    private attendanceService: DailyAttendanceService,
    private studentService: StudentService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private router: Router,
    private modalService: NgbModal,
    public academic: AcademicContextService
  ) {
    this.academic.load();
  }

  ngOnInit(): void {
    this.dayForm = this.formBuilder.group({
      status: [DailyAttendanceStatus.Present, Validators.required],
      checkInTime: [''],
      checkOutTime: [''],
      notes: ['', Validators.maxLength(500)]
    });
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));
    this.loadLookups();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Stages, grades, sections and students (once) */
  private loadLookups(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getAllGrades(),
      sections: this.sectionService.getAll(),
      students: this.studentService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ stages, grades, sections, students }) => {
          this.stages.set(stages.data || []);
          this.grades.set(grades.data || []);
          this.sections.set(sections.data || []);
          this.students.set(students.data || []);
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading attendance lookups:', error);
          this.errorMessage.set(this.translate.instant('attendance.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /** Days of the section's students on the chosen date */
  loadDay(): void {
    const sectionId = this.sectionId();
    if (!sectionId || !this.date()) {
      this.records.set([]);
      return;
    }

    this.isLoadingDay.set(true);
    this.errorMessage.set(null);

    this.attendanceService
      .getBySection(sectionId, this.date())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.records.set(response.data || []);
          } else {
            this.records.set([]);
            this.errorMessage.set(attendanceApiError(this.translate, response.message, 'attendance.loadFailed'));
          }
          this.isLoadingDay.set(false);
        },
        error: (error) => {
          console.error('Error loading section attendance:', error);
          this.records.set([]);
          this.errorMessage.set(this.translate.instant('attendance.loadFailed'));
          this.isLoadingDay.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Filters
  // ---------------------------------------------------------------------------

  onGradeChange(event: Event): void {
    this.gradeId.set((event.target as HTMLSelectElement).value);
    this.sectionId.set('');
    this.records.set([]);
  }

  onSectionChange(event: Event): void {
    this.sectionId.set((event.target as HTMLSelectElement).value);
    this.loadDay();
  }

  onDateChange(event: Event): void {
    this.date.set((event.target as HTMLInputElement).value || todayIso());
    this.loadDay();
  }

  setSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  // ---------------------------------------------------------------------------
  // Set status
  // ---------------------------------------------------------------------------

  openSetStatus(row: StudentDayRow, content: any): void {
    this.editingRow.set(row);
    this.modalError.set(null);
    this.dayForm.reset({
      status: row.record?.status ?? DailyAttendanceStatus.Present,
      checkInTime: toHourMinute(row.record?.checkInTime),
      checkOutTime: toHourMinute(row.record?.checkOutTime),
      notes: row.record?.notes ?? ''
    });
    this.openModal(content);
  }

  saveStatus(): void {
    const row = this.editingRow();
    if (!row || this.dayForm.invalid) {
      this.dayForm.markAllAsTouched();
      return;
    }

    const v = this.dayForm.getRawValue();
    const checkIn = v.checkInTime ? `${v.checkInTime}:00` : null;
    const checkOut = v.checkOutTime ? `${v.checkOutTime}:00` : null;
    if (checkIn && checkOut && checkOut < checkIn) {
      this.modalError.set(this.translate.instant('attendance.errors.CheckOutBeforeCheckIn'));
      return;
    }

    this.isSaving.set(true);
    this.modalError.set(null);

    this.attendanceService
      .setDay(this.dayRequest(row.student, Number(v.status), checkIn, checkOut, String(v.notes ?? '').trim() || null))
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.closeModal();
            this.showSuccess(this.translate.instant('attendance.saved'));
            this.loadDay();
          } else {
            this.modalError.set(attendanceApiError(this.translate, response.message, 'attendance.saveFailed'));
          }
          this.isSaving.set(false);
        },
        error: (error) => {
          console.error('Error saving the day:', error);
          this.modalError.set(attendanceApiError(this.translate, error?.error?.message, 'attendance.saveFailed'));
          this.isSaving.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Check out
  // ---------------------------------------------------------------------------

  openCheckOut(row: StudentDayRow, content: any): void {
    if (!row.record) return;
    this.editingRow.set(row);
    this.modalError.set(null);
    this.checkOutTime.set(toHourMinute(row.record.checkOutTime) || this.nowTime());
    this.openModal(content);
  }

  onCheckOutTime(event: Event): void {
    this.checkOutTime.set((event.target as HTMLInputElement).value);
  }

  confirmCheckOut(): void {
    const record = this.editingRow()?.record;
    if (!record || !this.checkOutTime()) return;

    this.isSaving.set(true);
    this.modalError.set(null);

    this.attendanceService
      .checkOut(record.id, this.checkOutTime())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.closeModal();
            this.showSuccess(this.translate.instant('attendance.checkedOut'));
            this.loadDay();
          } else {
            this.modalError.set(attendanceApiError(this.translate, response.message, 'attendance.saveFailed'));
          }
          this.isSaving.set(false);
        },
        error: (error) => {
          console.error('Error checking out:', error);
          this.modalError.set(attendanceApiError(this.translate, error?.error?.message, 'attendance.saveFailed'));
          this.isSaving.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Mark all present
  // ---------------------------------------------------------------------------

  openMarkAllPresent(content: any): void {
    if (this.unrecorded().length === 0) return;
    this.modalError.set(null);
    this.openModal(content);
  }

  /** set-day (Present) for every student of the section without a day; the others are left as they are */
  confirmMarkAllPresent(): void {
    const students = this.unrecorded();
    if (students.length === 0) return;

    this.isSaving.set(true);
    this.modalError.set(null);

    const requests: Observable<boolean>[] = students.map(s =>
      this.attendanceService.setDay(this.dayRequest(s, DailyAttendanceStatus.Present, null, null, null)).pipe(
        map(r => r.status === 200),
        catchError(() => of(false))
      ));

    forkJoin(requests)
      .pipe(takeUntil(this.destroy$))
      .subscribe(results => {
        const failed = results.filter(ok => !ok).length;
        const saved = results.length - failed;
        this.isSaving.set(false);
        this.closeModal();
        if (failed > 0) {
          this.errorMessage.set(this.translate.instant('attendance.markAllPartial', { saved, failed }));
        } else {
          this.showSuccess(this.translate.instant('attendance.markAllDone', { count: saved }));
        }
        this.loadDay();
      });
  }

  cancelModal(): void {
    this.closeModal();
  }

  navigateToStudent(studentId: string): void {
    this.router.navigate(['/attendance/daily/student', studentId]);
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  statusInfo(status: DailyAttendanceStatus) {
    return this.statuses.find(s => s.value === status) ?? this.statuses[0];
  }

  time(value: string | null | undefined): string {
    return formatTime(value) || '-';
  }

  studentName(student: Student): string {
    return this.currentLanguage() === 'ar' ? (student.fullNameAr || student.fullNameEn) : (student.fullNameEn || student.fullNameAr);
  }

  localName(item: { nameAr: string; nameEn: string } | undefined): string {
    if (!item) return '';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  stageName(stageId: string): string {
    return this.localName(this.stages().find(s => s.id === stageId));
  }

  /** set-day body for a student of the section on the chosen date (header year, else the student's year) */
  private dayRequest(student: Student, status: DailyAttendanceStatus, checkIn: string | null,
                     checkOut: string | null, notes: string | null): SetDailyAttendanceRequest {
    return {
      studentId: student.id,
      sectionId: student.sectionId,
      gradeId: student.gradeId,
      academicYearId: this.academic.yearId() || student.academicYearId,
      date: this.date(),
      status,
      checkInTime: checkIn,
      checkOutTime: checkOut,
      notes
    };
  }

  private openModal(content: any): void {
    this.modalRef = this.modalService.open(content, { centered: true, backdrop: 'static', keyboard: false });
  }

  private closeModal(): void {
    this.modalRef?.close();
    this.modalRef = null;
    this.editingRow.set(null);
  }

  private showSuccess(message: string): void {
    this.errorMessage.set(null);
    this.successMessage.set(message);
    timer(4000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.successMessage.set(null));
  }

  private nowTime(): string {
    const d = new Date();
    return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
  }
}
