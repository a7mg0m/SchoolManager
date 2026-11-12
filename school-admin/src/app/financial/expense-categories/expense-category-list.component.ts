import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ExpenseCategoryService } from '../../core/services/expense-category.service';
import { ExpenseCategory } from '../../core/models/expense-category.model';

@Component({
  selector: 'app-expense-category-list',
  standalone: false,
  styleUrl: './expense-category-list.component.scss',
  templateUrl: './expense-category-list.component.html',
})
export class ExpenseCategoryListComponent implements OnInit, OnDestroy {
  categories = signal<ExpenseCategory[]>([]);
  searchTerm = signal('');
  isLoading = signal(false);
  isDeleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  /** Categories filtered by the search box (name or description) */
  filteredCategories = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) return this.categories();
    return this.categories().filter(c =>
      c.name.toLowerCase().includes(term) ||
      (c.description || '').toLowerCase().includes(term)
    );
  });

  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  categoryToDelete = signal<ExpenseCategory | null>(null);

  constructor(
    private expenseCategoryService: ExpenseCategoryService,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCategories(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.expenseCategoryService
      .getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.categories.set(response.data);
          } else {
            this.errorMessage.set(response.message || 'Failed to load expense categories');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading expense categories:', error);
          this.errorMessage.set('Failed to load expense categories. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  onSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  navigateToCreate(): void {
    this.router.navigate(['/financial/expense-categories/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/financial/expense-categories/save', id]);
  }

  openDeleteConfirmation(category: ExpenseCategory, content: any): void {
    this.categoryToDelete.set(category);
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  confirmDelete(): void {
    const category = this.categoryToDelete();
    if (!category) return;

    this.isDeleting.set(true);

    this.expenseCategoryService
      .delete(category.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(response.message || 'Expense category deleted successfully');
            this.deleteModalRef?.close();
            this.categoryToDelete.set(null);
            this.loadCategories();

            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.errorMessage.set(response.message || 'Failed to delete expense category');
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting expense category:', error);
          // Most likely the category is still used by expenses (FK restriction)
          this.errorMessage.set('Failed to delete expense category. It may still be used by existing expenses.');
          this.deleteModalRef?.close();
          this.categoryToDelete.set(null);
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.categoryToDelete.set(null);
  }
}
