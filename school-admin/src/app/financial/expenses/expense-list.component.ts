import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { Subject, timer, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ExpenseService } from '../../core/services/expense.service';
import { ExpenseCategoryService } from '../../core/services/expense-category.service';
import { Expense, ExpenseType, EXPENSE_TYPES } from '../../core/models/expense.model';
import { ExpenseCategory } from '../../core/models/expense-category.model';

@Component({
  selector: 'app-expense-list',
  standalone: false,
  styleUrl: './expense-list.component.scss',
  templateUrl: './expense-list.component.html',
})
export class ExpenseListComponent implements OnInit, OnDestroy {
  expenses = signal<Expense[]>([]);
  categories = signal<ExpenseCategory[]>([]);
  isLoading = signal(false);
  isDeleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  expenseToDelete = signal<Expense | null>(null);

  // Filters
  searchTerm = signal('');
  categoryFilter = signal('');
  typeFilter = signal<string>('');
  dateFrom = signal('');
  dateTo = signal('');

  readonly expenseTypes = EXPENSE_TYPES;

  /** Expenses after applying all filters, newest first */
  filteredExpenses = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const category = this.categoryFilter();
    const type = this.typeFilter();
    const from = this.dateFrom();
    const to = this.dateTo();

    return this.expenses()
      .filter(e => {
        const date = this.dateOnly(e.expenseDate);
        if (category && e.categoryId !== category) return false;
        if (type !== '' && e.type !== Number(type)) return false;
        if (from && date < from) return false;
        if (to && date > to) return false;
        if (term) {
          const haystack = `${e.title} ${e.description || ''} ${e.reference || ''}`.toLowerCase();
          if (!haystack.includes(term)) return false;
        }
        return true;
      })
      .sort((a, b) => b.expenseDate.localeCompare(a.expenseDate));
  });

  /** Total of the filtered expenses, grouped by currency */
  totals = computed(() => {
    const map = new Map<string, number>();
    for (const e of this.filteredExpenses()) {
      map.set(e.currency, (map.get(e.currency) || 0) + e.amount);
    }
    return [...map.entries()].map(([currency, amount]) => ({ currency, amount }));
  });

  hasActiveFilters = computed(() =>
    !!(this.searchTerm() || this.categoryFilter() || this.typeFilter() !== '' || this.dateFrom() || this.dateTo())
  );

  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;

  constructor(
    private expenseService: ExpenseService,
    private expenseCategoryService: ExpenseCategoryService,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      expenses: this.expenseService.getAll(),
      categories: this.expenseCategoryService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ expenses, categories }) => {
          this.categories.set(categories.data || []);
          if (expenses.status === 200) {
            this.expenses.set(expenses.data || []);
          } else {
            this.errorMessage.set(expenses.message || 'Failed to load expenses');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading expenses:', error);
          this.errorMessage.set('Failed to load expenses. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Filters
  // ---------------------------------------------------------------------------

  setFilter(filter: 'search' | 'category' | 'type' | 'from' | 'to', event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLSelectElement).value;
    switch (filter) {
      case 'search': this.searchTerm.set(value); break;
      case 'category': this.categoryFilter.set(value); break;
      case 'type': this.typeFilter.set(value); break;
      case 'from': this.dateFrom.set(value); break;
      case 'to': this.dateTo.set(value); break;
    }
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.categoryFilter.set('');
    this.typeFilter.set('');
    this.dateFrom.set('');
    this.dateTo.set('');
  }

  // ---------------------------------------------------------------------------
  // Navigation
  // ---------------------------------------------------------------------------

  navigateToCreate(): void {
    this.router.navigate(['/financial/expenses/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/financial/expenses/save', id]);
  }

  navigateToCategories(): void {
    this.router.navigate(['/financial/expense-categories']);
  }

  // ---------------------------------------------------------------------------
  // Delete
  // ---------------------------------------------------------------------------

  openDeleteConfirmation(expense: Expense, content: any): void {
    this.expenseToDelete.set(expense);
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  confirmDelete(): void {
    const expense = this.expenseToDelete();
    if (!expense) return;

    this.isDeleting.set(true);

    this.expenseService
      .delete(expense.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(response.message || 'Expense deleted successfully');
            this.deleteModalRef?.close();
            this.expenseToDelete.set(null);
            this.loadData();

            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.errorMessage.set(response.message || 'Failed to delete expense');
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting expense:', error);
          this.errorMessage.set('Failed to delete expense. Please try again.');
          this.deleteModalRef?.close();
          this.expenseToDelete.set(null);
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.expenseToDelete.set(null);
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  categoryName(expense: Expense): string {
    return expense.categoryName
      || this.categories().find(c => c.id === expense.categoryId)?.name
      || '—';
  }

  typeKey(type: ExpenseType): string {
    return this.expenseTypes.find(t => t.value === type)?.key ?? 'expenses.types.other';
  }

  /** "2026-11-12T00:00:00" -> "2026-11-12" */
  dateOnly(value: string): string {
    return value ? value.substring(0, 10) : '';
  }

  formatDate(value: string): string {
    const date = this.dateOnly(value);
    if (!date) return '';
    try {
      return new Date(date + 'T00:00:00').toLocaleDateString(undefined, {
        year: 'numeric', month: 'short', day: 'numeric'
      });
    } catch {
      return date;
    }
  }

  formatAmount(amount: number, currency: string): string {
    const formatted = (amount ?? 0).toLocaleString(undefined, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
    return `${formatted} ${currency}`;
  }
}
