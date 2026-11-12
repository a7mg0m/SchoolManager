import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ExpenseCategoryService } from '../../core/services/expense-category.service';
import { ExpenseCategory } from '../../core/models/expense-category.model';

/** Matches the DB column sizes in ExpenseCategoryConfiguration */
export const EXPENSE_CATEGORY_NAME_MAX = 200;
export const EXPENSE_CATEGORY_DESCRIPTION_MAX = 1000;

@Component({
  selector: 'app-expense-category-save',
  standalone: false,
  styleUrl: './expense-category-save.component.scss',
  templateUrl: './expense-category-save.component.html',
})
export class ExpenseCategorySaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  categoryId: string | null = null;

  readonly nameMax = EXPENSE_CATEGORY_NAME_MAX;
  readonly descriptionMax = EXPENSE_CATEGORY_DESCRIPTION_MAX;

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private expenseCategoryService: ExpenseCategoryService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.formBuilder.group({
      name: ['', [Validators.required, Validators.maxLength(this.nameMax)]],
      description: ['', [Validators.maxLength(this.descriptionMax)]]
    });

    this.categoryId = this.activatedRoute.snapshot.paramMap.get('id');
    if (this.categoryId) {
      this.isEditMode.set(true);
      this.loadCategory(this.categoryId);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadCategory(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.expenseCategoryService
      .getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.populateForm(response.data);
          } else {
            this.errorMessage.set(response.message || 'Failed to load expense category');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading expense category:', error);
          this.errorMessage.set('Failed to load expense category. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(category: ExpenseCategory): void {
    this.form.patchValue({
      name: category.name,
      description: category.description ?? ''
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

    const value = this.form.value;
    const request = {
      name: (value.name || '').trim(),
      description: (value.description || '').trim() || null
    };

    const request$ = this.isEditMode()
      ? this.expenseCategoryService.update(this.categoryId!, request)
      : this.expenseCategoryService.create(request);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.successMessage.set(
            response.message && response.message !== 'Success'
              ? response.message
              : (this.isEditMode() ? 'Expense category updated successfully' : 'Expense category created successfully')
          );
          this.isSaving.set(false);
          setTimeout(() => this.router.navigate(['/financial/expense-categories']), 1000);
        } else {
          this.errorMessage.set(response.message || 'Failed to save expense category');
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving expense category:', error);
        this.errorMessage.set('Failed to save expense category. Please try again.');
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/financial/expense-categories']);
  }

  hasError(fieldName: string, errorType: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  length(fieldName: string): number {
    return (this.form.get(fieldName)?.value || '').length;
  }
}
