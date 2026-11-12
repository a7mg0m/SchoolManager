import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { RevenueCategoryService } from '../../core/services/revenue-category.service';
import { RevenueCategory } from '../../core/models/revenue-category.model';

@Component({
  selector: 'app-revenue-category-list',
  standalone: false,
  styleUrl: './revenue-category-list.component.scss',
  templateUrl: './revenue-category-list.component.html',
})
export class RevenueCategoryListComponent implements OnInit, OnDestroy {
  categories = signal<RevenueCategory[]>([]);
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
  categoryToDelete = signal<RevenueCategory | null>(null);

  constructor(
    private revenueCategoryService: RevenueCategoryService,
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

    this.revenueCategoryService
      .getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.categories.set(response.data);
          } else {
            this.errorMessage.set(response.message || 'Failed to load revenue categories');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading revenue categories:', error);
          this.errorMessage.set('Failed to load revenue categories. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  onSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  navigateToCreate(): void {
    this.router.navigate(['/financial/revenue-categories/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/financial/revenue-categories/save', id]);
  }

  openDeleteConfirmation(category: RevenueCategory, content: any): void {
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

    this.revenueCategoryService
      .delete(category.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(response.message || 'Revenue category deleted successfully');
            this.deleteModalRef?.close();
            this.categoryToDelete.set(null);
            this.loadCategories();

            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.errorMessage.set(response.message || 'Failed to delete revenue category');
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting revenue category:', error);
          // Most likely the category is still used by revenues (FK restriction)
          this.errorMessage.set('Failed to delete revenue category. It may still be used by existing revenues.');
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
