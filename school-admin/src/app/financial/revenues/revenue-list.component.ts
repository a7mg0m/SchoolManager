import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { Subject, timer, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { RevenueService } from '../../core/services/revenue.service';
import { RevenueCategoryService } from '../../core/services/revenue-category.service';
import { Revenue, RevenueType, REVENUE_TYPES } from '../../core/models/revenue.model';
import { RevenueCategory } from '../../core/models/revenue-category.model';

@Component({
  selector: 'app-revenue-list',
  standalone: false,
  styleUrl: './revenue-list.component.scss',
  templateUrl: './revenue-list.component.html',
})
export class RevenueListComponent implements OnInit, OnDestroy {
  revenues = signal<Revenue[]>([]);
  categories = signal<RevenueCategory[]>([]);
  isLoading = signal(false);
  isDeleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  revenueToDelete = signal<Revenue | null>(null);

  // Filters
  searchTerm = signal('');
  categoryFilter = signal('');
  typeFilter = signal<string>('');
  dateFrom = signal('');
  dateTo = signal('');

  readonly revenueTypes = REVENUE_TYPES;

  /** Revenues after applying all filters, newest first */
  filteredRevenues = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const category = this.categoryFilter();
    const type = this.typeFilter();
    const from = this.dateFrom();
    const to = this.dateTo();

    return this.revenues()
      .filter(e => {
        const date = this.dateOnly(e.revenueDate);
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
      .sort((a, b) => b.revenueDate.localeCompare(a.revenueDate));
  });

  /** Total of the filtered revenues, grouped by currency */
  totals = computed(() => {
    const map = new Map<string, number>();
    for (const e of this.filteredRevenues()) {
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
    private revenueService: RevenueService,
    private revenueCategoryService: RevenueCategoryService,
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
      revenues: this.revenueService.getAll(),
      categories: this.revenueCategoryService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ revenues, categories }) => {
          this.categories.set(categories.data || []);
          if (revenues.status === 200) {
            this.revenues.set(revenues.data || []);
          } else {
            this.errorMessage.set(revenues.message || 'Failed to load revenues');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading revenues:', error);
          this.errorMessage.set('Failed to load revenues. Please try again.');
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
    this.router.navigate(['/financial/revenues/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/financial/revenues/save', id]);
  }

  navigateToCategories(): void {
    this.router.navigate(['/financial/revenue-categories']);
  }

  // ---------------------------------------------------------------------------
  // Delete
  // ---------------------------------------------------------------------------

  openDeleteConfirmation(revenue: Revenue, content: any): void {
    this.revenueToDelete.set(revenue);
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  confirmDelete(): void {
    const revenue = this.revenueToDelete();
    if (!revenue) return;

    this.isDeleting.set(true);

    this.revenueService
      .delete(revenue.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(response.message || 'Revenue deleted successfully');
            this.deleteModalRef?.close();
            this.revenueToDelete.set(null);
            this.loadData();

            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.errorMessage.set(response.message || 'Failed to delete revenue');
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting revenue:', error);
          this.errorMessage.set('Failed to delete revenue. Please try again.');
          this.deleteModalRef?.close();
          this.revenueToDelete.set(null);
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.revenueToDelete.set(null);
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  categoryName(revenue: Revenue): string {
    return revenue.categoryName
      || this.categories().find(c => c.id === revenue.categoryId)?.name
      || '—';
  }

  typeKey(type: RevenueType): string {
    return this.revenueTypes.find(t => t.value === type)?.key ?? 'revenues.types.other';
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
