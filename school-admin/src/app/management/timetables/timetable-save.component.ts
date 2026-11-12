import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { TimetableService } from '../../core/services/timetable.service';
import { GradeService } from '../../core/services/grade.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { SectionService } from '../../core/services/section.service';
import { SubjectService } from '../../core/services/subject.service';
import { TeacherService } from '../../core/services/teacher.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Timetable, TimetableSlot, CreateTimetableRequest, UpdateTimetableRequest } from '../../core/models/timetable.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { AcademicYear } from '../../core/models/academic-year.model';
import { Section } from '../../core/models/section.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';
import { Teacher } from '../../core/models/teacher.model';

/** .NET DayOfWeek values (0 = Sunday) */
export const WEEK_DAYS: { value: number; key: string }[] = [
  { value: 0, key: 'timetables.days.sunday' },
  { value: 1, key: 'timetables.days.monday' },
  { value: 2, key: 'timetables.days.tuesday' },
  { value: 3, key: 'timetables.days.wednesday' },
  { value: 4, key: 'timetables.days.thursday' },
  { value: 5, key: 'timetables.days.friday' },
  { value: 6, key: 'timetables.days.saturday' }
];

/** Slot-level validator: end time must be after start time */
function endTimeAfterStartTime(group: AbstractControl): ValidationErrors | null {
  const start = group.get('startTime')?.value;
  const end = group.get('endTime')?.value;
  if (start && end && start >= end) {
    return { endTimeMustBeGreater: true };
  }
  return null;
}

/** Array-level validator: slots on the same day + section must not overlap */
function noOverlappingSlots(array: AbstractControl): ValidationErrors | null {
  const slots = (array as FormArray).controls.map(c => c.value);
  for (let i = 0; i < slots.length; i++) {
    for (let j = i + 1; j < slots.length; j++) {
      const a = slots[i];
      const b = slots[j];
      if (
        a.day !== null && a.day === b.day &&
        a.sectionId && a.sectionId === b.sectionId &&
        a.startTime && a.endTime && b.startTime && b.endTime &&
        a.startTime < b.endTime && b.startTime < a.endTime
      ) {
        return { overlappingSlots: { first: i + 1, second: j + 1 } };
      }
    }
  }
  return null;
}

@Component({
  selector: 'app-timetable-save',
  standalone: false,
  styleUrl: './timetable-save.component.scss',
  templateUrl: './timetable-save.component.html',
})
export class TimetableSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  currentLanguage = signal<string>('en');
  timetableId: string | null = null;

  // Lookups
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  academicYears = signal<AcademicYear[]>([]);
  sections = signal<Section[]>([]);
  subjects = signal<SchoolSubject[]>([]);
  teachers = signal<Teacher[]>([]);

  // Grade-filtered lookups (recomputed when grade changes)
  gradeSections = signal<Section[]>([]);
  gradeSubjects = signal<SchoolSubject[]>([]);

  readonly weekDays = WEEK_DAYS;

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private timetableService: TimetableService,
    private gradeService: GradeService,
    private academicYearService: AcademicYearService,
    private sectionService: SectionService,
    private subjectService: SubjectService,
    private teacherService: TeacherService,
    private languageService: LanguageService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.timetableId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.timetableId);

    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    // Changing the stage clears a grade that doesn't belong to it
    this.form.get('stageId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(stageId => {
        const gradeId = this.form.get('gradeId')!.value;
        if (gradeId && !this.stageGrades.some(g => g.id === gradeId)) {
          this.form.get('gradeId')!.setValue('');
        }
      });

    this.form.get('gradeId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(gradeId => this.onGradeChanged(gradeId));

    this.loadLookups();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ---------------------------------------------------------------------------
  // Form
  // ---------------------------------------------------------------------------

  private initializeForm(): void {
    this.form = this.formBuilder.group({
      stageId: ['', Validators.required],
      gradeId: ['', Validators.required],
      academicYearId: ['', Validators.required],
      slots: this.formBuilder.array([], { validators: noOverlappingSlots })
    });
  }

  get slots(): FormArray {
    return this.form.get('slots') as FormArray;
  }

  private createSlotGroup(slot?: Partial<TimetableSlot>): FormGroup {
    const group = this.formBuilder.group(
      {
        id: [slot?.id ?? null],
        day: [slot?.day ?? null, Validators.required],
        nameAr: [slot?.nameAr ?? '', [Validators.required, Validators.maxLength(100)]],
        nameEn: [slot?.nameEn ?? '', [Validators.required, Validators.maxLength(100)]],
        startTime: [this.toInputTime(slot?.startTime), Validators.required],
        endTime: [this.toInputTime(slot?.endTime), Validators.required],
        sectionId: [slot?.sectionId ?? '', Validators.required],
        subjectId: [slot?.subjectId ?? '', Validators.required],
        teacherId: [slot?.teacherId ?? '', Validators.required]
      },
      { validators: endTimeAfterStartTime }
    );

    // When the subject changes, clear the teacher if they don't teach it
    group.get('subjectId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(subjectId => {
        const teacherId = group.get('teacherId')!.value;
        if (teacherId && !this.getTeachersForSubject(subjectId).some(t => t.id === teacherId)) {
          group.get('teacherId')!.setValue('');
        }
      });

    return group;
  }

  addSlot(): void {
    // Pre-fill day/section from the last slot to make bulk entry faster
    const last = this.slots.length ? this.slots.at(this.slots.length - 1).value : null;
    this.slots.push(this.createSlotGroup({
      day: last?.day ?? null,
      sectionId: last?.sectionId ?? '',
      startTime: last?.endTime ? this.fromInputTime(last.endTime) : undefined
    }));
  }

  duplicateSlot(index: number): void {
    const value = this.slots.at(index).value;
    this.slots.insert(index + 1, this.createSlotGroup({
      ...value,
      id: null,
      startTime: this.fromInputTime(value.startTime),
      endTime: this.fromInputTime(value.endTime)
    }));
  }

  removeSlot(index: number): void {
    this.slots.removeAt(index);
  }

  /** Sort slots by day then start time (purely visual) */
  sortSlots(): void {
    const sorted = [...this.slots.controls].sort((a, b) => {
      const av = a.value, bv = b.value;
      if (av.day !== bv.day) return (av.day ?? 99) - (bv.day ?? 99);
      return (av.startTime || '').localeCompare(bv.startTime || '');
    });
    this.slots.clear();
    sorted.forEach(c => this.slots.push(c));
  }

  // ---------------------------------------------------------------------------
  // Loading
  // ---------------------------------------------------------------------------

  private loadLookups(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getActiveGrades(),
      academicYears: this.academicYearService.getAll(),
      sections: this.sectionService.getAll(),
      subjects: this.subjectService.getAll(),
      teachers: this.teacherService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.stages.set(res.stages.data || []);
          this.grades.set(res.grades.data || []);
          this.academicYears.set(res.academicYears.data || []);
          this.sections.set(res.sections.data || []);
          this.subjects.set(res.subjects.data || []);
          this.teachers.set((res.teachers.data || []).filter(t => t.isActive));

          if (this.timetableId) {
            this.loadTimetable(this.timetableId);
          } else {
            const activeYear = this.academicYears().find(y => y.isActive);
            if (activeYear) {
              this.form.patchValue({ academicYearId: activeYear.id });
            }
            this.isLoading.set(false);
          }
        },
        error: (error) => {
          console.error('Error loading lookups:', error);
          this.errorMessage.set('Failed to load required data. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private loadTimetable(id: string): void {
    this.timetableService
      .getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.populateForm(response.data);
          } else {
            this.errorMessage.set(response.message || 'Failed to load timetable');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading timetable:', error);
          this.errorMessage.set('Failed to load timetable. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(timetable: Timetable): void {
    const stageId = timetable.stageId
      || this.grades().find(g => g.id === timetable.gradeId)?.stageId
      || '';
    this.form.patchValue({
      stageId,
      gradeId: timetable.gradeId,
      academicYearId: timetable.academicYearId
    }, { emitEvent: false });
    this.applyGradeFilter(timetable.gradeId);

    this.slots.clear();
    [...(timetable.slots || [])]
      .sort((a, b) => a.day - b.day || a.startTime.localeCompare(b.startTime))
      .forEach(slot => this.slots.push(this.createSlotGroup(slot)));
  }

  // ---------------------------------------------------------------------------
  // Grade-dependent filtering
  // ---------------------------------------------------------------------------

  private onGradeChanged(gradeId: string): void {
    this.applyGradeFilter(gradeId);

    // Clear slot sections/subjects that don't belong to the new grade
    const sectionIds = new Set(this.gradeSections().map(s => s.id));
    const subjectIds = new Set(this.gradeSubjects().map(s => s.id));
    this.slots.controls.forEach(slot => {
      if (!sectionIds.has(slot.get('sectionId')!.value)) slot.get('sectionId')!.setValue('');
      if (!subjectIds.has(slot.get('subjectId')!.value)) slot.get('subjectId')!.setValue('');
    });
  }

  private applyGradeFilter(gradeId: string): void {
    this.gradeSections.set(this.sections().filter(s => s.gradeId === gradeId));
    this.gradeSubjects.set(this.subjects().filter(s => s.gradeId === gradeId));
  }

  /** Teachers assigned to the subject; falls back to all teachers if the subject has none */
  getTeachersForSubject(subjectId: string): Teacher[] {
    const subject = this.subjects().find(s => s.id === subjectId);
    if (!subject || !subject.teacherIds?.length) {
      return this.teachers();
    }
    return this.teachers().filter(t => subject.teacherIds.includes(t.id));
  }

  /** Grades of the selected stage for the grade <select> */
  get stageGrades(): Grade[] {
    const stageId = this.form?.get('stageId')?.value;
    return stageId ? this.grades().filter(g => g.stageId === stageId) : [];
  }

  // ---------------------------------------------------------------------------
  // Submit
  // ---------------------------------------------------------------------------

  onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Please fix form errors before submitting');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue();
    const slots: TimetableSlot[] = value.slots.map((s: any) => ({
      id: s.id || null,
      day: Number(s.day),
      nameAr: s.nameAr,
      nameEn: s.nameEn,
      startTime: this.fromInputTime(s.startTime),
      endTime: this.fromInputTime(s.endTime),
      sectionId: s.sectionId,
      subjectId: s.subjectId,
      teacherId: s.teacherId,
      gradeId: value.gradeId
    }));

    const request$ = this.isEditMode()
      ? this.timetableService.update({
          id: this.timetableId!,
          stageId: value.stageId,
          gradeId: value.gradeId,
          academicYearId: value.academicYearId,
          slots
        } as UpdateTimetableRequest)
      : this.timetableService.create({
          stageId: value.stageId,
          gradeId: value.gradeId,
          academicYearId: value.academicYearId,
          slots: slots.map(({ id, ...rest }) => rest)
        } as CreateTimetableRequest);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.successMessage.set(
            response.message || (this.isEditMode() ? 'Timetable updated successfully' : 'Timetable created successfully')
          );
          this.isSaving.set(false);
          setTimeout(() => this.router.navigate(['/management/timetables']), 1000);
        } else {
          this.errorMessage.set(response.message || 'Failed to save timetable');
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving timetable:', error);
        this.errorMessage.set('Failed to save timetable. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/management/timetables']);
  }

  // ---------------------------------------------------------------------------
  // Template helpers
  // ---------------------------------------------------------------------------

  getName(item: { nameAr?: string; nameEn?: string } | undefined | null): string {
    if (!item) return '';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  getTeacherName(teacher: Teacher): string {
    return this.currentLanguage() === 'ar'
      ? (teacher.fullNameAr || teacher.fullNameEn)
      : (teacher.fullNameEn || teacher.fullNameAr);
  }

  hasError(fieldName: string, errorType: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  hasSlotError(index: number, fieldName: string, errorType: string): boolean {
    const control = this.slots.at(index)?.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  slotControl(index: number, name: string): any {
    return this.slots.at(index)?.get(name);
  }

  /** "08:00:00" -> "08:00" for <input type="time"> */
  private toInputTime(value?: string | null): string {
    return value ? value.substring(0, 5) : '';
  }

  /** "08:00" -> "08:00:00" for .NET TimeOnly */
  private fromInputTime(value?: string | null): string {
    if (!value) return '';
    return value.length === 5 ? `${value}:00` : value;
  }
}
