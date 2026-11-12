import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { FeeService } from '../../core/services/fee.service';
import { GradeService } from '../../core/services/grade.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Fee, FeeType, FEE_TYPES, CreateFeeRequest, UpdateFeeRequest } from '../../core/models/fee.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';

/** Column sizes from FeeConfiguration */
const NAME_MAX = 200;
const CURRENCY_MAX = 10;

/** Suggestions for the currency field (free text is still allowed) */
const CURRENCY_SUGGESTIONS = ['EGP', 'SAR', 'AED', 'KWD', 'QAR', 'BHD', 'OMR', 'JOD', 'USD', 'EUR', 'GBP'];

/** Browser-local convenience: remember the last currency used */
const LAST_CURRENCY_KEY = 'fees.lastCurrency';

@Component({
  selector: 'app-fee-save',
  standalone: false,
  styleUrl: './fee-save.component.scss',
  templateUrl: './fee-save.component.html',
})
export class FeeSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  currentLanguage = signal<string>('en');
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  feeId: string | null = null;

  readonly feeTypes = FEE_TYPES;
  readonly currencies = CURRENCY_SUGGESTIONS;
  readonly nameMax = NAME_MAX;
  readonly currencyMax = CURRENCY_MAX;

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private feeService: FeeService,
    private gradeService: GradeService,
    private languageService: LanguageService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.formBuilder.group({
      stageId: ['', Validators.required],
      gradeId: ['', Validators.required],
      name: ['', [Validators.required, Validators.maxLength(NAME_MAX)]],
      type: [FeeType.Tuition, Validators.required],
      amount: [null, [Validators.required, Validators.min(0.01)]],
      currency: [this.readLastCurrency(), [Validators.required, Validators.maxLength(CURRENCY_MAX)]],
      isMandatory: [true]
    });

    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    // Changing the stage clears a grade from another stage
    this.form.get('stageId')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        const gradeId = this.form.get('gradeId')!.value;
        if (gradeId && !this.stageGrades.some(g => g.id === gradeId)) {
          this.form.get('gradeId')!.setValue('');
        }
      });

    this.feeId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.feeId);
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getAllGrades(),
      fee: this.feeId ? this.feeService.getById(this.feeId) : of(null)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ stages, grades, fee }) => {
          this.stages.set(stages.data || []);
          this.grades.set(grades.data || []);

          if (fee) {
            if (fee.status === 200 && fee.data) {
              this.populateForm(fee.data);
            } else {
              this.errorMessage.set(fee.message || 'Failed to load fee');
            }
          } else {
            // Pre-select grade when coming from a grade card ("+" button)
            const gradeId = this.activatedRoute.snapshot.queryParamMap.get('gradeId');
            const grade = gradeId ? this.grades().find(g => g.id === gradeId) : undefined;
            if (grade) {
              this.form.patchValue({ stageId: grade.stageId, gradeId: grade.id });
            }
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading fee data:', error);
          this.errorMessage.set('Failed to load data. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(fee: Fee): void {
    const grade = this.grades().find(g => g.id === fee.gradeId);
    this.form.patchValue({
      stageId: grade?.stageId ?? '',
      gradeId: fee.gradeId,
      name: fee.name,
      type: fee.type,
      amount: fee.amount,
      currency: fee.currency,
      isMandatory: fee.isMandatory
    });

    // The API can't move a fee to another grade
    this.form.get('stageId')!.disable({ emitEvent: false });
    this.form.get('gradeId')!.disable({ emitEvent: false });
  }

  /**
   * Grades of the selected stage. Inactive grades are hidden,
   * except the fee's current grade in edit mode.
   */
  get stageGrades(): Grade[] {
    const stageId = this.form?.get('stageId')?.value;
    const currentGradeId = this.form?.get('gradeId')?.value;
    if (!stageId) return [];
    return this.grades().filter(g =>
      g.stageId === stageId && (g.isActive || (this.isEditMode() && g.id === currentGradeId))
    );
  }

  onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Please fix form errors before submitting');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const v = this.form.getRawValue();
    const currency = (v.currency || '').trim().toUpperCase();
    const body: UpdateFeeRequest = {
      name: (v.name || '').trim(),
      type: Number(v.type),
      amount: Number(v.amount),
      currency,
      isMandatory: !!v.isMandatory
    };

    const request$ = this.isEditMode()
      ? this.feeService.update(this.feeId!, body)
      : this.feeService.create({ ...body, gradeId: v.gradeId } as CreateFeeRequest);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.saveLastCurrency(currency);
          this.successMessage.set(
            response.message && response.message !== 'Success'
              ? response.message
              : (this.isEditMode() ? 'Fee updated successfully' : 'Fee created successfully')
          );
          this.isSaving.set(false);
          setTimeout(() => this.router.navigate(['/financial/fees']), 1000);
        } else {
          this.errorMessage.set(response.message || 'Failed to save fee');
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving fee:', error);
        this.errorMessage.set('Failed to save fee. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/financial/fees']);
  }

  getName(item: { nameAr?: string; nameEn?: string }): string {
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  hasError(fieldName: string, errorType: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  isInvalid(fieldName: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  private readLastCurrency(): string {
    try {
      return localStorage.getItem(LAST_CURRENCY_KEY) || '';
    } catch {
      return '';
    }
  }

  private saveLastCurrency(currency: string): void {
    try {
      localStorage.setItem(LAST_CURRENCY_KEY, currency);
    } catch {
      // storage unavailable – ignore
    }
  }
}
