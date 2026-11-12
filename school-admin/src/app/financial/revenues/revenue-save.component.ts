import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { RevenueService } from '../../core/services/revenue.service';
import { RevenueCategoryService } from '../../core/services/revenue-category.service';
import { Revenue, RevenueType, REVENUE_TYPES, SaveRevenueRequest } from '../../core/models/revenue.model';
import { RevenueCategory } from '../../core/models/revenue-category.model';

/** Column sizes from RevenueConfiguration */
const TITLE_MAX = 200;
const DESCRIPTION_MAX = 1000;
const REFERENCE_MAX = 200;
const CURRENCY_MAX = 10;

/** Suggestions for the currency field (free text is still allowed) */
const CURRENCY_SUGGESTIONS = ['EGP', 'SAR', 'AED', 'KWD', 'QAR', 'BHD', 'OMR', 'JOD', 'USD', 'EUR', 'GBP'];

/** Browser-local convenience: remember the last currency used */
const LAST_CURRENCY_KEY = 'revenues.lastCurrency';

@Component({
  selector: 'app-revenue-save',
  standalone: false,
  styleUrl: './revenue-save.component.scss',
  templateUrl: './revenue-save.component.html',
})
export class RevenueSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  categories = signal<RevenueCategory[]>([]);
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  revenueId: string | null = null;


  readonly revenueTypes = REVENUE_TYPES;
  readonly currencies = CURRENCY_SUGGESTIONS;
  readonly titleMax = TITLE_MAX;
  readonly descriptionMax = DESCRIPTION_MAX;
  readonly referenceMax = REFERENCE_MAX;
  readonly currencyMax = CURRENCY_MAX;

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private revenueService: RevenueService,
    private revenueCategoryService: RevenueCategoryService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.formBuilder.group({
      title: ['', [Validators.required, Validators.maxLength(TITLE_MAX)]],
      categoryId: ['', Validators.required],
      type: [RevenueType.Other, Validators.required],
      amount: [null, [Validators.required, Validators.min(0.01)]],
      currency: [this.readLastCurrency(), [Validators.required, Validators.maxLength(CURRENCY_MAX)]],
      revenueDate: [this.today(), Validators.required],
      reference: ['', [Validators.maxLength(REFERENCE_MAX)]],
      description: ['', [Validators.maxLength(DESCRIPTION_MAX)]]
    });

    this.revenueId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.revenueId);
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
      categories: this.revenueCategoryService.getAll(),
      revenue: this.revenueId ? this.revenueService.getById(this.revenueId) : of(null)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ categories, revenue }) => {
          this.categories.set(categories.data || []);

          if (revenue) {
            if (revenue.status === 200 && revenue.data) {
              this.populateForm(revenue.data);
            } else {
              this.errorMessage.set(revenue.message || 'Failed to load revenue');
            }
          } else if (this.categories().length === 1) {
            // Only one category: pre-select it
            this.form.patchValue({ categoryId: this.categories()[0].id });
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading revenue data:', error);
          this.errorMessage.set('Failed to load data. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(revenue: Revenue): void {
    this.form.patchValue({
      title: revenue.title,
      categoryId: revenue.categoryId,
      type: revenue.type,
      amount: revenue.amount,
      currency: revenue.currency,
      revenueDate: revenue.revenueDate ? revenue.revenueDate.substring(0, 10) : '',
      reference: revenue.reference ?? '',
      description: revenue.description ?? ''
    });
  }

  onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Please fix form errors before submitting');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const v = this.form.value;
    const currency = (v.currency || '').trim().toUpperCase();
    const request: SaveRevenueRequest = {
      title: (v.title || '').trim(),
      description: (v.description || '').trim() || null,
      categoryId: v.categoryId,
      type: Number(v.type),
      amount: Number(v.amount),
      currency,
      // Date-only value sent as local midnight (no timezone shift)
      revenueDate: `${v.revenueDate}T00:00:00`,
      reference: (v.reference || '').trim() || null
    };

    const request$ = this.isEditMode()
      ? this.revenueService.update(this.revenueId!, request)
      : this.revenueService.create(request);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.saveLastCurrency(currency);
          this.successMessage.set(
            response.message && response.message !== 'Success'
              ? response.message
              : (this.isEditMode() ? 'Revenue updated successfully' : 'Revenue created successfully')
          );
          this.isSaving.set(false);
          setTimeout(() => this.router.navigate(['/financial/revenues']), 1000);
        } else {
          this.errorMessage.set(response.message || 'Failed to save revenue');
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving revenue:', error);
        this.errorMessage.set('Failed to save revenue. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/financial/revenues']);
  }

  navigateToCategories(): void {
    this.router.navigate(['/financial/revenue-categories/save']);
  }

  hasError(fieldName: string, errorType: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  isInvalid(fieldName: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  length(fieldName: string): number {
    return (this.form.get(fieldName)?.value || '').length;
  }

  // ---------------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------------

  private today(): string {
    const d = new Date();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${mm}-${dd}`;
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
      // storage unavailable (private mode etc.) – ignore
    }
  }
}
