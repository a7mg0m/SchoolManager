import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ExamService } from '../../core/services/exam.service';
import { GradeService } from '../../core/services/grade.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { SectionService } from '../../core/services/section.service';
import { SubjectService } from '../../core/services/subject.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Exam, EXAM_TYPES, CreateExamRequest } from '../../core/models/exam.model';
import { Grade } from '../../core/models/grade.model';
import { Stage } from '../../core/models/stage.model';
import { AcademicYear, AcademicYearSemester } from '../../core/models/academic-year.model';
import { Section } from '../../core/models/section.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

/** Form-level validator: end time must be after start time */
function endTimeAfterStartTime(group: AbstractControl): ValidationErrors | null {
  const start = group.get('startTime')?.value;
  const end = group.get('endTime')?.value;
  if (start && end && start >= end) {
    return { endTimeMustBeGreater: true };
  }
  return null;
}

/** Form-level validator: success score must not exceed the weight (full mark) */
function successScoreWithinWeight(group: AbstractControl): ValidationErrors | null {
  const weight = group.get('weight')?.value;
  const success = group.get('successScore')?.value;
  if (weight !== null && weight !== '' && success !== null && success !== '' && Number(success) > Number(weight)) {
    return { successScoreAboveWeight: true };
  }
  return null;
}

@Component({
  selector: 'app-exam-save',
  standalone: false,
  templateUrl: './exam-save.component.html',
})
export class ExamSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  currentLanguage = signal<string>('en');
  examId: string | null = null;

  // Lookups
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  academicYears = signal<AcademicYear[]>([]);
  private sections: Section[] = [];
  private subjects: SchoolSubject[] = [];

  // Dependent lookups
  stageGrades = signal<Grade[]>([]);
  gradeSections = signal<Section[]>([]);
  gradeSubjects = signal<SchoolSubject[]>([]);
  yearSemesters = signal<AcademicYearSemester[]>([]);

  readonly examTypes = EXAM_TYPES;

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private examService: ExamService,
    private gradeService: GradeService,
    private academicYearService: AcademicYearService,
    private sectionService: SectionService,
    private subjectService: SubjectService,
    private languageService: LanguageService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.examId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.examId);

    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    // Stage drives grades
    this.form.get('stageId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(stageId => {
        this.applyStageFilter(stageId);
        this.clearIfNotIn('gradeId', this.stageGrades());
      });

    // Grade drives sections & subjects
    this.form.get('gradeId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(gradeId => {
        this.applyGradeFilter(gradeId);
        this.clearIfNotIn('sectionId', this.gradeSections());
        this.clearIfNotIn('subjectId', this.gradeSubjects());
      });

    // Academic year drives semesters
    this.form.get('academicYearId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(yearId => {
        this.applyYearFilter(yearId);
        this.clearIfNotIn('semesterId', this.yearSemesters());
      });

    // Keep duration in sync with start/end time
    this.form.get('startTime')!.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => this.updateDuration());
    this.form.get('endTime')!.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => this.updateDuration());

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
    this.form = this.formBuilder.group(
      {
        stageId: ['', Validators.required],
        gradeId: ['', Validators.required],
        sectionId: [''], // optional: empty = all sections of the grade
        subjectId: ['', Validators.required],
        academicYearId: ['', Validators.required],
        semesterId: ['', Validators.required],
        examType: [null, Validators.required],
        examDate: ['', Validators.required],
        startTime: ['', Validators.required],
        endTime: ['', Validators.required],
        duration: [null, [Validators.required, Validators.min(1), Validators.max(1440)]],
        weight: [100, [Validators.required, Validators.min(0.01), Validators.max(10000)]],
        successScore: [50, [Validators.required, Validators.min(0)]],
        answerModelUrl: ['', [Validators.maxLength(500), Validators.pattern(/^https?:\/\/\S+$/i)]]
      },
      { validators: [endTimeAfterStartTime, successScoreWithinWeight] }
    );
  }

  private clearIfNotIn(controlName: string, items: { id?: string }[]): void {
    const control = this.form.get(controlName)!;
    if (control.value && !items.some(i => i.id === control.value)) {
      control.setValue('');
    }
  }

  private applyStageFilter(stageId: string): void {
    this.stageGrades.set(stageId ? this.grades().filter(g => g.stageId === stageId) : []);
  }

  private applyGradeFilter(gradeId: string): void {
    this.gradeSections.set(this.sections.filter(s => s.gradeId === gradeId));
    this.gradeSubjects.set(this.subjects.filter(s => s.gradeId === gradeId && s.isActive !== false));
  }

  private applyYearFilter(yearId: string): void {
    const year = this.academicYears().find(y => y.id === yearId);
    this.yearSemesters.set((year?.semesters || []).filter(s => !!s.id));
  }

  /** Duration (minutes) = end - start, when both are set and valid */
  private updateDuration(): void {
    const start = this.toMinutes(this.form.get('startTime')!.value);
    const end = this.toMinutes(this.form.get('endTime')!.value);
    if (start !== null && end !== null && end > start) {
      this.form.get('duration')!.setValue(end - start);
    }
  }

  private toMinutes(value: string | null): number | null {
    if (!value) return null;
    const [h, m] = value.split(':').map(Number);
    return Number.isFinite(h) && Number.isFinite(m) ? h * 60 + m : null;
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
      subjects: this.subjectService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.stages.set(res.stages.data || []);
          this.grades.set(res.grades.data || []);
          this.academicYears.set(res.academicYears.data || []);
          this.sections = res.sections.data || [];
          this.subjects = res.subjects.data || [];

          if (this.examId) {
            this.loadExam(this.examId);
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

  private loadExam(id: string): void {
    this.examService
      .getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.populateForm(response.data);
          } else {
            this.errorMessage.set(response.message || 'Failed to load exam');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading exam:', error);
          this.errorMessage.set('Failed to load exam. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(exam: Exam): void {
    // Exams saved before stage existed have an empty stageId: derive it from the grade
    const stageId = (exam.stageId && exam.stageId !== EMPTY_GUID)
      ? exam.stageId
      : this.grades().find(g => g.id === exam.gradeId)?.stageId || '';

    // Fill dependent lists first so the selects can show the saved values
    this.applyStageFilter(stageId);
    this.applyGradeFilter(exam.gradeId);
    this.applyYearFilter(exam.academicYearId);

    this.form.patchValue({
      stageId,
      gradeId: exam.gradeId,
      sectionId: exam.sectionId || '',
      subjectId: exam.subjectId,
      academicYearId: exam.academicYearId,
      semesterId: exam.semesterId,
      examType: exam.examType,
      examDate: exam.examDate ? exam.examDate.substring(0, 10) : '',
      startTime: this.toInputTime(exam.startTime),
      endTime: this.toInputTime(exam.endTime),
      duration: exam.duration,
      weight: exam.weight,
      successScore: exam.successScore,
      answerModelUrl: exam.answerModelUrl || ''
    }, { emitEvent: false });
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
    const request: CreateExamRequest = {
      subjectId: value.subjectId,
      stageId: value.stageId,
      gradeId: value.gradeId,
      sectionId: value.sectionId || null,
      academicYearId: value.academicYearId,
      semesterId: value.semesterId,
      examType: Number(value.examType),
      examDate: value.examDate,
      startTime: this.fromInputTime(value.startTime),
      endTime: this.fromInputTime(value.endTime),
      duration: Number(value.duration),
      weight: Number(value.weight),
      successScore: Number(value.successScore),
      answerModelUrl: value.answerModelUrl?.trim() || null
    };

    const request$ = this.isEditMode()
      ? this.examService.update({ id: this.examId!, ...request })
      : this.examService.create(request);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.successMessage.set(
            this.isEditMode() ? 'Exam updated successfully' : 'Exam created successfully'
          );
          this.isSaving.set(false);
          timer(1000)
            .pipe(takeUntil(this.destroy$))
            .subscribe(() => this.router.navigate(['/exam/exams']));
        } else {
          this.errorMessage.set(response.message || 'Failed to save exam');
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving exam:', error);
        this.errorMessage.set(this.saveErrorMessage(error));
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/exam/exams']);
  }

  // ---------------------------------------------------------------------------
  // Template helpers
  // ---------------------------------------------------------------------------

  getName(item: { nameAr?: string; nameEn?: string } | undefined | null): string {
    if (!item) return '';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  hasError(fieldName: string, errorType: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  get successScoreAboveWeight(): boolean {
    const s = this.form.get('successScore');
    return this.form.hasError('successScoreAboveWeight') && !!s && (s.dirty || s.touched);
  }

  /** Map backend error codes to friendly messages */
  private saveErrorMessage(error: any): string {
    switch (error?.error?.message) {
      case 'WeightBelowExistingScores':
        return 'The full mark is lower than scores already entered for this exam. Fix those results first or use a higher full mark.';
      case 'WeightInvalid':
        return 'Full mark must be greater than 0.';
      case 'SuccessScoreInvalid':
        return 'Pass score must be between 0 and the full mark.';
      default:
        return error?.error?.message || 'Failed to save exam. Please try again.';
    }
  }

  get timeRangeInvalid(): boolean {
    const end = this.form.get('endTime');
    return this.form.hasError('endTimeMustBeGreater') && !!end && (end.dirty || end.touched);
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
