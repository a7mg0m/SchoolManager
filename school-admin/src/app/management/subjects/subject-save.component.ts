import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { SubjectService } from '../../core/services/subject.service';
import { GradeService } from '../../core/services/grade.service';
import { StageService } from '../../core/services/stage.service';
import { TeacherService } from '../../core/services/teacher.service';
import { Stage } from '../../core/models/stage.model';
import {
  Subject as SubjectModel,
  CreateSubjectRequest,
  UpdateSubjectRequest,
  SubjectSingleResponse
} from '../../core/models/subject.model';
import { Grade } from '../../core/models/grade.model';
import { Teacher } from '../../core/models/teacher.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

/**
 * Requires a non-empty array (backend validator: TeacherIds NotEmpty)
 */
function nonEmptyArray(control: AbstractControl): ValidationErrors | null {
  return Array.isArray(control.value) && control.value.length > 0 ? null : { required: true };
}

@Component({
  selector: 'app-subject-save',
  standalone: false,
  styleUrl: './subject-save.component.scss',
  templateUrl: './subject-save.component.html',
})
export class SubjectSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  currentLanguage = signal<string>('en');
  subjectId: string | null = null;

  private allStages = signal<Stage[]>([]);
  private allGrades = signal<Grade[]>([]);
  private allTeachers = signal<Teacher[]>([]);
  /** Stage/grade/teachers already assigned to the subject being edited (kept even if now inactive) */
  private originalStageId = signal<string>('');
  private originalGradeId = signal<string>('');
  private originalTeacherIds = signal<string[]>([]);

  /** Stage chosen in the form (UI only: the subject itself stores just its grade) */
  selectedStageId = signal<string>('');
  selectedTeacherIds = signal<string[]>([]);
  teacherSearch = signal<string>('');

  /**
   * Active stages, plus the subject's current stage if it has since been deactivated
   */
  stageOptions = computed(() => {
    const keep = this.originalStageId();
    return this.allStages().filter(s => s.isActive || s.id === keep);
  });

  /**
   * Grades of the selected stage: active ones, plus the subject's current grade
   * if it has since been deactivated
   */
  gradeOptions = computed(() => {
    const stageId = this.selectedStageId();
    if (!stageId) {
      return [];
    }
    const keep = this.originalGradeId();
    return this.allGrades().filter(g => g.stageId === stageId && (g.isActive || g.id === keep));
  });

  /**
   * Active teachers, plus any already assigned teacher that has since been deactivated
   */
  teacherOptions = computed(() => {
    const keep = new Set(this.originalTeacherIds());
    return this.allTeachers().filter(t => t.isActive || keep.has(t.id));
  });

  /**
   * Teacher options matching the search box inside the dropdown
   */
  visibleTeachers = computed(() => {
    const term = this.teacherSearch().trim().toLowerCase();
    if (!term) {
      return this.teacherOptions();
    }
    return this.teacherOptions().filter(t =>
      (t.fullNameAr ?? '').toLowerCase().includes(term) ||
      (t.fullNameEn ?? '').toLowerCase().includes(term)
    );
  });

  /**
   * Selected teachers in selection order (for the chips)
   */
  selectedTeachers = computed(() => {
    const map = new Map(this.allTeachers().map(t => [t.id, t]));
    return this.selectedTeacherIds().map(id => ({ id, teacher: map.get(id) ?? null }));
  });

  private readonly emptyGuid = '00000000-0000-0000-0000-000000000000';

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private subjectService: SubjectService,
    private gradeService: GradeService,
    private stageService: StageService,
    private teacherService: TeacherService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.subjectId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.subjectId);

    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe((language) => this.currentLanguage.set(language));

    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Initialize the form (name lengths mirror the backend EF configuration)
   */
  private initializeForm(): void {
    this.form = this.formBuilder.group({
      nameAr: ['', [Validators.required, Validators.maxLength(200)]],
      nameEn: ['', [Validators.required, Validators.maxLength(200)]],
      stageId: ['', Validators.required],
      gradeId: [{ value: '', disabled: true }, Validators.required],
      teacherIds: [[] as string[], nonEmptyArray]
    });

    // Keep the grade list in sync with the selected stage
    this.form.get('stageId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((stageId: string) => this.onStageSelected(stageId));
  }

  /**
   * React to stage selection: enable the grade dropdown and drop a grade from another stage
   */
  private onStageSelected(stageId: string): void {
    this.selectedStageId.set(stageId || '');
    const gradeControl = this.form.get('gradeId')!;

    if (stageId) {
      gradeControl.enable({ emitEvent: false });
    } else {
      gradeControl.disable({ emitEvent: false });
    }

    const gradeId = gradeControl.value;
    if (gradeId && !this.allGrades().some(g => g.id === gradeId && g.stageId === stageId)) {
      gradeControl.setValue('', { emitEvent: false });
    }
  }

  /**
   * Load stages, grades, teachers and (in edit mode) the subject in parallel,
   * then populate the form once all options are available
   */
  private loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      stages: this.stageService.getAllStages(),
      grades: this.gradeService.getAllGrades(),
      teachers: this.teacherService.getAll(),
      subject: this.subjectId
        ? this.subjectService.getById(this.subjectId)
        : of<SubjectSingleResponse | null>(null)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ stages, grades, teachers, subject }) => {
          this.allStages.set(stages.status === 200 && stages.data ? stages.data : []);
          this.allGrades.set(grades.status === 200 && grades.data ? grades.data : []);
          this.allTeachers.set(teachers.status === 200 && teachers.data ? teachers.data : []);

          if (subject) {
            if (subject.status === 200 && subject.data) {
              this.populateForm(subject.data);
            } else {
              this.errorMessage.set(
                subject.message || this.translate.instant('subjects.loadSubjectFailed')
              );
            }
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading subject form data:', error);
          this.errorMessage.set(
            this.translate.instant(this.subjectId ? 'subjects.loadSubjectFailed' : 'subjects.loadLookupsFailed')
          );
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Populate form with subject data
   */
  private populateForm(subject: SubjectModel): void {
    const teacherIds = [...new Set(subject.teacherIds ?? [])];
    // The subject only stores its grade; its stage is the grade's stage
    const stageId = this.allGrades().find(g => g.id === subject.gradeId)?.stageId ?? '';

    this.originalStageId.set(stageId);
    this.originalGradeId.set(subject.gradeId ?? '');
    this.originalTeacherIds.set(teacherIds);

    // Setting stageId first enables and filters the grade dropdown
    this.form.patchValue({
      nameAr: subject.nameAr ?? '',
      nameEn: subject.nameEn ?? '',
      stageId
    });
    this.form.patchValue({ gradeId: subject.gradeId ?? '' });
    this.setTeacherIds(teacherIds);
  }

  /**
   * Update teacher selection in both the form control and the signal
   */
  private setTeacherIds(ids: string[]): void {
    const control = this.form.get('teacherIds')!;
    control.setValue(ids);
    control.markAsDirty();
    this.selectedTeacherIds.set(ids);
  }

  /**
   * Toggle a teacher in the selection
   */
  toggleTeacher(id: string): void {
    const current = this.selectedTeacherIds();
    this.setTeacherIds(current.includes(id) ? current.filter(x => x !== id) : [...current, id]);
  }

  /**
   * Remove a selected teacher (chip close button)
   */
  removeTeacher(id: string): void {
    this.setTeacherIds(this.selectedTeacherIds().filter(x => x !== id));
    this.form.get('teacherIds')!.markAsTouched();
  }

  isTeacherSelected(id: string): boolean {
    return this.selectedTeacherIds().includes(id);
  }

  /**
   * Mark the teacher control as touched when the dropdown closes, and reset its search
   */
  onTeacherDropdownToggle(open: boolean): void {
    if (!open) {
      this.form.get('teacherIds')!.markAsTouched();
      this.teacherSearch.set('');
    }
  }

  onTeacherSearch(event: Event): void {
    this.teacherSearch.set((event.target as HTMLInputElement).value);
  }

  /**
   * Submit the form
   */
  onSubmit(): void {
    if (this.isSaving()) {
      return;
    }

    if (!this.form.valid) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.translate.instant('subjects.invalidForm'));
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const v = this.form.getRawValue();
    const body = {
      nameAr: v.nameAr.trim(),
      nameEn: v.nameEn.trim(),
      gradeId: v.gradeId,
      teacherIds: [...this.selectedTeacherIds()]
    };

    if (this.isEditMode()) {
      const request: UpdateSubjectRequest = { id: this.subjectId!, ...body };
      this.save(this.subjectService.update(request), 'subjects.updated', 'subjects.updateFailed');
    } else {
      const request: CreateSubjectRequest = { id: this.emptyGuid, ...body };
      this.save(this.subjectService.create(request), 'subjects.created', 'subjects.createFailed');
    }
  }

  /**
   * Shared create/update submission handling
   */
  private save(request$: ReturnType<SubjectService['create']>, successKey: string, failKey: string): void {
    request$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.successMessage.set(this.translate.instant(successKey));
            // Navigate back after 1 second (keep isSaving to block duplicate submits)
            setTimeout(() => this.router.navigate(['/management/subjects']), 1000);
          } else {
            this.errorMessage.set(response.message || this.translate.instant(failKey));
            this.isSaving.set(false);
          }
        },
        error: (error) => {
          console.error('Error saving subject:', error);
          this.errorMessage.set(this.translate.instant(failKey));
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Cancel and navigate back
   */
  onCancel(): void {
    this.router.navigate(['/management/subjects']);
  }

  /**
   * Localized stage or grade name
   */
  getOptionName(item: Stage | Grade): string {
    return this.currentLanguage() === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr;
  }

  /**
   * Localized teacher name
   */
  getTeacherName(teacher: Teacher | null): string {
    if (!teacher) {
      return this.translate.instant('subjects.unknownTeacher');
    }
    return this.currentLanguage() === 'ar'
      ? teacher.fullNameAr || teacher.fullNameEn
      : teacher.fullNameEn || teacher.fullNameAr;
  }

  /**
   * Check if a field has error
   */
  hasError(fieldName: string, errorType: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  /**
   * Check if a field is invalid and has been interacted with
   */
  isInvalid(fieldName: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.invalid && control.touched);
  }
}
