import { Component, OnInit, OnDestroy, effect, signal } from '@angular/core';
import { AcademicContextService } from '../../core/services/academic-context.service';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Subject, forkJoin, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ExamService } from '../../core/services/exam.service';
import { GradeService } from '../../core/services/grade.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { SectionService } from '../../core/services/section.service';
import { SubjectService } from '../../core/services/subject.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Exam, EXAM_TYPES, EXAM_MONTHS, ExamType, CreateExamRequest } from '../../core/models/exam.model';
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

  /** Answer model upload (PDF or picture, same limits as the server) */
  answerUploading = signal(false);
  answerProgress = signal(0);
  answerError = signal<string | null>(null);
  private static readonly ANSWER_MAX_BYTES = 10 * 1024 * 1024;
  private static readonly ANSWER_TYPES = ['application/pdf', 'image/jpeg', 'image/png', 'image/webp', 'image/gif'];

  /** Final exams a second final can repeat (with whether they already have one) */
  finals = signal<{ exam: Exam; label: string }[]>([]);
  private allFinals: { exam: Exam; label: string; taken: boolean; stageId: string }[] = [];
  private allExams: Exam[] = [];

  readonly examTypes = EXAM_TYPES;
  readonly months = EXAM_MONTHS;

  private destroy$ = new Subject<void>();

  constructor(
    private http: HttpClient,
    private formBuilder: FormBuilder,
    private examService: ExamService,
    private gradeService: GradeService,
    private academicYearService: AcademicYearService,
    private sectionService: SectionService,
    private subjectService: SubjectService,
    private languageService: LanguageService,
    private activatedRoute: ActivatedRoute,
    private router: Router,
    public academic: AcademicContextService
  ) {
    this.followHeader();
  }

  ngOnInit(): void {
    this.initializeForm();
    this.examId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.examId);
    this.lockAcademicFields();
    this.academic.load();

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
        // Month of a Monthly exam (1..12); required only when the type is Monthly
        month: [null as number | null, [Validators.min(1), Validators.max(12)]],
        examDate: ['', Validators.required],
        startTime: ['', Validators.required],
        endTime: ['', Validators.required],
        duration: [null, [Validators.required, Validators.min(1), Validators.max(1440)]],
        weight: [100, [Validators.required, Validators.min(0.01), Validators.max(10000)]],
        successScore: [50, [Validators.required, Validators.min(0)]],
        // Uploaded file (/exam-files/answer-models/…) or, for older exams, a link
        answerModelUrl: ['', [Validators.maxLength(500)]],
        // Second final only: the Final exam it repeats
        originalExamId: ['']
      },
      { validators: [endTimeAfterStartTime, successScoreWithinWeight] }
    );

    // Second final: choosing the Final fills its grade / class / subject
    this.form.get('originalExamId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(id => this.applyOriginal(id));

    // The Finals offered follow the selected stage / year / grade / class / subject
    ['stageId', 'gradeId', 'sectionId', 'subjectId', 'academicYearId'].forEach(name =>
      this.form.get(name)!.valueChanges
        .pipe(takeUntil(this.destroy$))
        .subscribe(() => this.filterFinals()));

    // Monthly exams need a month: make it required and suggest the exam date's (or this) month
    this.form.get('examType')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((type) => {
        this.updateSecondFinal(type);
        this.updateMonthValidators(type);
        const month = this.form.get('month')!;
        if (this.isMonthly(type) && !month.value) {
          const date = this.form.get('examDate')!.value;
          month.setValue(date ? Number(String(date).substring(5, 7)) : new Date().getMonth() + 1);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Answer model file
  // ---------------------------------------------------------------------------

  get answerModel(): string {
    return this.form?.get('answerModelUrl')?.value || '';
  }

  /** Full address of the answer model (a stored path goes through the gateway) */
  answerModelHref(): string {
    const url = this.answerModel;
    if (!url) return '';
    return /^https?:\/\//i.test(url) ? url : `${environment.gatewayUrl}${url.startsWith('/') ? '' : '/'}${url}`;
  }

  answerModelIsPdf(): boolean {
    return /\.pdf($|\?)/i.test(this.answerModel);
  }

  answerModelIsImage(): boolean {
    return /\.(jpe?g|png|webp|gif)($|\?)/i.test(this.answerModel);
  }

  onAnswerFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // the same file can be picked again
    if (!file) return;

    this.answerError.set(null);
    if (!ExamSaveComponent.ANSWER_TYPES.includes(file.type)) {
      this.answerError.set('exams.answerModelErrors.FileTypeNotAllowed');
      return;
    }
    if (file.size > ExamSaveComponent.ANSWER_MAX_BYTES) {
      this.answerError.set('exams.answerModelErrors.FileTooLarge');
      return;
    }

    const body = new FormData();
    body.append('file', file, file.name);
    this.answerUploading.set(true);
    this.answerProgress.set(0);
    this.http
      .post<{ data?: string }>(`${environment.gatewayUrl}/api/exam/upload-answer-model`, body, { reportProgress: true, observe: 'events' })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: e => {
          if (e.type === HttpEventType.UploadProgress && e.total) {
            this.answerProgress.set(Math.round((e.loaded / e.total) * 100));
          } else if (e.type === HttpEventType.Response) {
            const url = e.body?.data;
            if (url) {
              this.form.get('answerModelUrl')!.setValue(url);
              this.form.get('answerModelUrl')!.markAsDirty();
            } else {
              this.answerError.set('exams.answerModelErrors.UploadFailed');
            }
            this.answerUploading.set(false);
          }
        },
        error: err => {
          const key = err?.error?.message;
          this.answerError.set(['FileRequired', 'FileTooLarge', 'FileTypeNotAllowed'].includes(key)
            ? `exams.answerModelErrors.${key}`
            : err?.status === 413 ? 'exams.answerModelErrors.FileTooLarge' : 'exams.answerModelErrors.UploadFailed');
          this.answerUploading.set(false);
        }
      });
  }

  removeAnswerModel(): void {
    this.form.get('answerModelUrl')!.setValue('');
    this.form.get('answerModelUrl')!.markAsDirty();
    this.answerError.set(null);
  }

  /** The selected type is Second final */
  isSecondFinal(type: unknown = this.form?.get('examType')?.value): boolean {
    return Number(type) === ExamType.SecondFinal;
  }

  /** Second final: the Final it repeats is required (the class / subject fields stay editable) */
  private updateSecondFinal(type: unknown): void {
    const original = this.form.get('originalExamId')!;
    if (this.isSecondFinal(type)) {
      original.setValidators([Validators.required]);
      this.filterFinals();
    } else {
      original.clearValidators();
      original.setValue('', { emitEvent: false });
    }
    original.updateValueAndValidity({ emitEvent: false });
  }

  /**
   * The Finals of the selected stage and academic year (and grade / class / subject once chosen) that have no
   * second final yet. A chosen Final no longer offered is cleared.
   */
  private filterFinals(): void {
    if (!this.form) return;
    const v = this.form.getRawValue();
    const list = v.stageId && v.academicYearId
      ? this.allFinals.filter(f =>
          !f.taken &&
          f.stageId === v.stageId &&
          f.exam.academicYearId === v.academicYearId &&
          (!v.gradeId || f.exam.gradeId === v.gradeId) &&
          (!v.sectionId || !f.exam.sectionId || f.exam.sectionId === v.sectionId) &&
          (!v.subjectId || f.exam.subjectId === v.subjectId))
      : [];
    this.finals.set(list.map(f => ({ exam: f.exam, label: f.label })));

    const original = this.form.get('originalExamId')!;
    if (original.value && !list.some(f => f.exam.id === original.value)) {
      original.setValue('', { emitEvent: false });
    }
  }

  /** Fills the chosen Final's grade / class / subject and semester (its stage and year are the selected ones) */
  private applyOriginal(id: string): void {
    if (!this.isSecondFinal()) return;
    const original = this.allExams.find(e => e.id === id);
    if (!original) return;
    this.applyGradeFilter(original.gradeId);
    this.form.patchValue({
      gradeId: original.gradeId,
      sectionId: original.sectionId || '',
      subjectId: original.subjectId,
      semesterId: original.semesterId
    }, { emitEvent: false });
    this.filterFinals();
  }

  /** Finals of the exams list, newest first, labelled "subject · grade / class · date" */
  private buildFinals(exams: Exam[]): void {
    this.allExams = exams;
    const taken = new Set(exams.filter(e => e.examType === ExamType.SecondFinal && e.id !== this.examId).map(e => e.originalExamId));
    this.allFinals = exams
      .filter(e => e.examType === ExamType.Final)
      .sort((a, b) => (b.examDate || '').localeCompare(a.examDate || ''))
      .map(e => {
        const grade = this.grades().find(g => g.id === e.gradeId);
        const section = this.sections.find(s => s.id === e.sectionId);
        const subject = this.subjects.find(s => s.id === e.subjectId);
        const year = this.academicYears().find(y => y.id === e.academicYearId);
        return {
          exam: e,
          taken: taken.has(e.id),
          stageId: (e.stageId && e.stageId !== EMPTY_GUID) ? e.stageId : grade?.stageId || '',
          label: [this.getName(subject), [this.getName(grade), this.getName(section)].filter(Boolean).join(' / '),
            (e.examDate || '').substring(0, 10), year?.name].filter(Boolean).join(' · ')
        };
      });
    this.filterFinals();
  }

  /** The selected type is Monthly (the select gives a string, the API a number) */
  isMonthly(type: unknown = this.form?.get('examType')?.value): boolean {
    return Number(type) === ExamType.Monthly;
  }

  private updateMonthValidators(type: unknown): void {
    const month = this.form.get('month')!;
    month.setValidators(this.isMonthly(type)
      ? [Validators.required, Validators.min(1), Validators.max(12)]
      : [Validators.min(1), Validators.max(12)]);
    month.updateValueAndValidity({ emitEvent: false });
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

// ---------------------------------------------------------------------------
  // Academic year / semester from the header
  // ---------------------------------------------------------------------------

  /** New record: the header's year and semester (kept in step when the header changes) */
  private followHeader(): void {
    effect(() => {
      const academicYearId = this.academic.yearId();
      const semesterId = this.academic.semesterId();
      if (this.form && !this.isEditMode()) {
        this.form.patchValue({ academicYearId, semesterId });
      }
    });
  }

  /** Year / semester are not edited here: new records take the header's, saved ones keep theirs */
  private lockAcademicFields(): void {
    this.form.get('academicYearId')!.disable({ emitEvent: false });
    this.form.get('semesterId')!.disable({ emitEvent: false });
  }

  /** The header has no year / semester to save with */
  private missingAcademicContext(value: { academicYearId?: string; semesterId?: string }): boolean {
    return !value.academicYearId || !value.semesterId;
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
      subjects: this.subjectService.getAll(),
      exams: this.examService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.stages.set(res.stages.data || []);
          this.grades.set(res.grades.data || []);
          this.academicYears.set(res.academicYears.data || []);
          this.sections = res.sections.data || [];
          this.subjects = res.subjects.data || [];
          this.buildFinals(res.exams.data || []);

          if (this.examId) {
            this.loadExam(this.examId);
          } else {
            // The header's academic year / semester
            this.form.patchValue({ academicYearId: this.academic.yearId(), semesterId: this.academic.semesterId() });
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
      // A Monthly exam saved without a month: suggest the exam date's month
      month: exam.month ?? (this.isMonthly(exam.examType) && exam.examDate
        ? Number(exam.examDate.substring(5, 7))
        : null),
      examDate: exam.examDate ? exam.examDate.substring(0, 10) : '',
      startTime: this.toInputTime(exam.startTime),
      endTime: this.toInputTime(exam.endTime),
      duration: exam.duration,
      weight: exam.weight,
      successScore: exam.successScore,
      answerModelUrl: exam.answerModelUrl || '',
      originalExamId: exam.originalExamId || ''
    }, { emitEvent: false });
    this.updateSecondFinal(exam.examType);
    // patchValue above does not emit: apply the Monthly rule for the loaded type
    this.updateMonthValidators(exam.examType);
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

    if (this.missingAcademicContext(this.form.getRawValue())) {
      this.errorMessage.set('Choose the academic year and semester in the header first.');
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
      // Only Monthly exams have a month
      month: this.isMonthly(value.examType) && value.month ? Number(value.month) : null,
      examDate: value.examDate,
      startTime: this.fromInputTime(value.startTime),
      endTime: this.fromInputTime(value.endTime),
      duration: Number(value.duration),
      weight: Number(value.weight),
      successScore: Number(value.successScore),
      answerModelUrl: value.answerModelUrl?.trim() || null,
      originalExamId: this.isSecondFinal(value.examType) ? value.originalExamId || null : null
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
    const code = error?.error?.message;
    const secondFinal: Record<string, string> = {
      OriginalFinalRequired: 'Choose the final exam this second final repeats.',
      OriginalMustBeFinal: 'A second final can only repeat a Final exam.',
      SecondFinalExists: 'This final exam already has a second final.',
      HasSecondFinal: 'This final has a second final: it must stay a Final exam.'
    };
    if (code && secondFinal[code]) return secondFinal[code];

    switch (error?.error?.message) {
      case 'WeightBelowExistingScores':
        return 'The full mark is lower than scores already entered for this exam. Fix those results first or use a higher full mark.';
      case 'WeightInvalid':
        return 'Full mark must be greater than 0.';
      case 'SuccessScoreInvalid':
        return 'Pass score must be between 0 and the full mark.';
      case 'MonthRequired':
        return 'Please choose the month of the monthly exam.';
      case 'MonthInvalid':
        return 'Month must be between 1 and 12.';
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
