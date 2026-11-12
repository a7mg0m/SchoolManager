import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ExpenseService } from '../../core/services/expense.service';
import { ExpenseCategoryService } from '../../core/services/expense-category.service';
import { Expense, ExpenseType, EXPENSE_TYPES, EMPTY_GUID, SaveExpenseRequest } from '../../core/models/expense.model';
import { ExpenseCategory } from '../../core/models/expense-category.model';

/** Column sizes from ExpenseConfiguration */
const TITLE_MAX = 200;
const DESCRIPTION_MAX = 1000;
const REFERENCE_MAX = 200;
const CURRENCY_MAX = 10;

/** Suggestions for the currency field (free text is still allowed) */
const CURRENCY_SUGGESTIONS = ['EGP', 'SAR', 'AED', 'KWD', 'QAR', 'BHD', 'OMR', 'JOD', 'USD', 'EUR', 'GBP'];

/** Browser-local convenience: remember the last currency used */
const LAST_CURRENCY_KEY = 'expenses.lastCurrency';

@Component({
  selector: 'app-expense-save',
  standalone: false,
  styleUrl: './expense-save.component.scss',
  templateUrl: './expense-save.component.html',
})
export class ExpenseSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  categories = signal<ExpenseCategory[]>([]);
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  expenseId: string | null = null;

  /** Kept as-is on update: the API has no endpoint to list school events yet */
  private eventId: string = EMPTY_GUID;

  readonly expenseTypes = EXPENSE_TYPES;
  readonly currencies = CURRENCY_SUGGESTIONS;
  readonly titleMax = TITLE_MAX;
  readonly descriptionMax = DESCRIPTION_MAX;
  readonly referenceMax = REFERENCE_MAX;
  readonly currencyMax = CURRENCY_MAX;

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private expenseService: ExpenseService,
    private expenseCategoryService: ExpenseCategoryService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.formBuilder.group({
      title: ['', [Validators.required, Validators.maxLength(TITLE_MAX)]],
      categoryId: ['', Validators.required],
      type: [ExpenseType.Other, Validators.required],
      amount: [null, [Validators.required, Validators.min(0.01)]],
      currency: [this.readLastCurrency(), [Validators.required, Validators.maxLength(CURRENCY_MAX)]],
      expenseDate: [this.today(), Validators.required],
      reference: ['', [Validators.maxLength(REFERENCE_MAX)]],
      description: ['', [Validators.maxLength(DESCRIPTION_MAX)]]
    });

    this.expenseId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.expenseId);
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
      categories: this.expenseCategoryService.getAll(),
      expense: this.expenseId ? this.expenseService.getById(this.expenseId) : of(null)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ categories, expense }) => {
          this.categories.set(categories.data || []);

          if (expense) {
            if (expense.status === 200 && expense.data) {
              this.populateForm(expense.data);
            } else {
              this.errorMessage.set(expense.message || 'Failed to load expense');
            }
          } else if (this.categories().length === 1) {
            // Only one category: pre-select it
            this.form.patchValue({ categoryId: this.categories()[0].id });
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading expense data:', error);
          this.errorMessage.set('Failed to load data. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(expense: Expense): void {
    this.eventId = expense.eventId || EMPTY_GUID;
    this.form.patchValue({
      title: expense.title,
      categoryId: expense.categoryId,
      type: expense.type,
      amount: expense.amount,
      currency: expense.currency,
      expenseDate: expense.expenseDate ? expense.expenseDate.substring(0, 10) : '',
      reference: expense.reference ?? '',
      description: expense.description ?? ''
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
    const request: SaveExpenseRequest = {
      title: (v.title || '').trim(),
      description: (v.description || '').trim() || null,
      eventId: this.eventId,
      categoryId: v.categoryId,
      type: Number(v.type),
      amount: Number(v.amount),
      currency,
      // Date-only value sent as local midnight (no timezone shift)
      expenseDate: `${v.expenseDate}T00:00:00`,
      reference: (v.reference || '').trim() || null
    };

    const request$ = this.isEditMode()
      ? this.expenseService.update(this.expenseId!, request)
      : this.expenseService.create(request);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.saveLastCurrency(currency);
          this.successMessage.set(
            response.message && response.message !== 'Success'
              ? response.message
              : (this.isEditMode() ? 'Expense updated successfully' : 'Expense created successfully')
          );
          this.isSaving.set(false);
          setTimeout(() => this.router.navigate(['/financial/expenses']), 1000);
        } else {
          this.errorMessage.set(response.message || 'Failed to save expense');
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving expense:', error);
        this.errorMessage.set('Failed to save expense. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/financial/expenses']);
  }

  navigateToCategories(): void {
    this.router.navigate(['/financial/expense-categories/save']);
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
