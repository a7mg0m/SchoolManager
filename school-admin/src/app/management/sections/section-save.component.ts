import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { SectionService } from '../../core/services/section.service';
import { GradeService } from '../../core/services/grade.service';
import { Section, CreateSectionRequest, UpdateSectionRequest } from '../../core/models/section.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-section-save',
  standalone: false,
  styleUrl: './section-save.component.scss',
  templateUrl: './section-save.component.html',
})
export class SectionSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  selectedStageId = signal<string>('');
  currentLanguage = signal<string>('en');
  sectionId: string | null = null;

  /**
   * Grades belonging to the selected stage
   */
  availableGrades = computed(() => {
    const stageId = this.selectedStageId();
    return stageId ? this.grades().filter(g => g.stageId === stageId) : [];
  });

  private readonly emptyGuid = '00000000-0000-0000-0000-000000000000';

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private sectionService: SectionService,
    private gradeService: GradeService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.sectionId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.sectionId);

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
   * Initialize the form with default values
   */
  private initializeForm(): void {
    this.form = this.formBuilder.group({
      nameAr: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(100)]],
      nameEn: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(100)]],
      stageId: ['', Validators.required],
      gradeId: [{ value: '', disabled: true }, Validators.required]
    });

    // Keep grade list in sync with the selected stage
    this.form.get('stageId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((stageId: string) => this.onStageSelected(stageId));
  }

  /**
   * Load stages, grades and (in edit mode) the section
   */
  private loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getActiveGrades()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ stages, grades }) => {
          this.stages.set(stages.status === 200 && stages.data ? stages.data : []);
          this.grades.set(grades.status === 200 && grades.data ? grades.data : []);

          if (this.sectionId) {
            this.loadSection(this.sectionId);
          } else {
            this.isLoading.set(false);
          }
        },
        error: (error) => {
          console.error('Error loading stages/grades:', error);
          this.errorMessage.set(this.translate.instant('sections.loadLookupsFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Load section data for edit mode
   * @param id - Section ID
   */
  private loadSection(id: string): void {
    this.sectionService
      .getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.populateForm(response.data);
            this.errorMessage.set(null);
          } else {
            this.errorMessage.set(
              response.message || this.translate.instant('sections.loadSectionFailed')
            );
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading section:', error);
          this.errorMessage.set(this.translate.instant('sections.loadSectionFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Populate form with section data
   * @param section - The section to load
   */
  private populateForm(section: Section): void {
    // Setting stageId first enables and filters the grade dropdown
    this.form.patchValue({
      nameAr: section.nameAr,
      nameEn: section.nameEn,
      stageId: section.stageId
    });
    this.form.patchValue({ gradeId: section.gradeId });
  }

  /**
   * React to stage selection: enable grade dropdown and drop a grade from another stage
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
    if (gradeId && !this.grades().some(g => g.id === gradeId && g.stageId === stageId)) {
      gradeControl.setValue('', { emitEvent: false });
    }
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
      this.errorMessage.set(this.translate.instant('sections.invalidForm'));
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    if (this.isEditMode()) {
      this.updateSection();
    } else {
      this.createSection();
    }
  }

  /**
   * Create a new section
   */
  private createSection(): void {
    const formValue = this.form.getRawValue();
    const request: CreateSectionRequest = {
      id: this.emptyGuid,
      nameAr: formValue.nameAr.trim(),
      nameEn: formValue.nameEn.trim(),
      stageId: formValue.stageId,
      gradeId: formValue.gradeId
    };

    this.sectionService
      .create(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.successMessage.set(this.translate.instant('sections.created'));
            // Navigate back after 1 second (keep isSaving to block duplicate submits)
            setTimeout(() => {
              this.router.navigate(['/management/sections']);
            }, 1000);
          } else {
            this.errorMessage.set(
              response.message || this.translate.instant('sections.createFailed')
            );
            this.isSaving.set(false);
          }
        },
        error: (error) => {
          console.error('Error creating section:', error);
          this.errorMessage.set(this.translate.instant('sections.createFailed'));
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Update an existing section
   */
  private updateSection(): void {
    const formValue = this.form.getRawValue();
    const request: UpdateSectionRequest = {
      id: this.sectionId!,
      nameAr: formValue.nameAr.trim(),
      nameEn: formValue.nameEn.trim(),
      stageId: formValue.stageId,
      gradeId: formValue.gradeId
    };

    this.sectionService
      .update(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.successMessage.set(this.translate.instant('sections.updated'));
            // Navigate back after 1 second (keep isSaving to block duplicate submits)
            setTimeout(() => {
              this.router.navigate(['/management/sections']);
            }, 1000);
          } else {
            this.errorMessage.set(
              response.message || this.translate.instant('sections.updateFailed')
            );
            this.isSaving.set(false);
          }
        },
        error: (error) => {
          console.error('Error updating section:', error);
          this.errorMessage.set(this.translate.instant('sections.updateFailed'));
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Cancel and navigate back
   */
  onCancel(): void {
    this.router.navigate(['/management/sections']);
  }

  /**
   * Get localized name for a dropdown option
   */
  getOptionName(item: Stage | Grade): string {
    return this.currentLanguage() === 'ar' ? item.nameAr : item.nameEn || item.nameAr;
  }

  /**
   * Check if a field has error
   * @param fieldName - Name of the field
   * @param errorType - Type of error to check
   */
  hasError(fieldName: string, errorType: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }
}
