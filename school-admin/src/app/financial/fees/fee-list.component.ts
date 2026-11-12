import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { Subject, timer, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { FeeService } from '../../core/services/fee.service';
import { GradeService } from '../../core/services/grade.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Fee, FeeType, FEE_TYPES } from '../../core/models/fee.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';

interface CurrencyTotal {
  currency: string;
  amount: number;
}

interface GradeGroup {
  gradeId: string;
  gradeName: string;
  stageName: string;
  fees: Fee[];
  mandatoryTotals: CurrencyTotal[];
  allTotals: CurrencyTotal[];
}

@Component({
  selector: 'app-fee-list',
  standalone: false,
  styleUrl: './fee-list.component.scss',
  templateUrl: './fee-list.component.html',
})
export class FeeListComponent implements OnInit, OnDestroy {
  fees = signal<Fee[]>([]);
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  currentLanguage = signal<string>('en');
  isLoading = signal(false);
  isDeleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  feeToDelete = signal<Fee | null>(null);

  // Filters
  searchTerm = signal('');
  stageFilter = signal('');
  gradeFilter = signal('');
  typeFilter = signal('');
  mandatoryFilter = signal<'' | 'mandatory' | 'optional'>('');

  readonly feeTypes = FEE_TYPES;

  /** Grades shown in the grade filter (limited to the selected stage) */
  filterGrades = computed(() => {
    const stageId = this.stageFilter();
    return stageId ? this.grades().filter(g => g.stageId === stageId) : this.grades();
  });

  filteredFees = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const stageId = this.stageFilter();
    const gradeId = this.gradeFilter();
    const type = this.typeFilter();
    const mandatory = this.mandatoryFilter();
    const gradeById = new Map(this.grades().map(g => [g.id, g]));

    return this.fees().filter(f => {
      if (gradeId && f.gradeId !== gradeId) return false;
      if (stageId && gradeById.get(f.gradeId)?.stageId !== stageId) return false;
      if (type !== '' && f.type !== Number(type)) return false;
      if (mandatory === 'mandatory' && !f.isMandatory) return false;
      if (mandatory === 'optional' && f.isMandatory) return false;
      if (term && !f.name.toLowerCase().includes(term)) return false;
      return true;
    });
  });

  /** Filtered fees grouped by grade, ordered by stage then grade name */
  groups = computed<GradeGroup[]>(() => {
    const gradeById = new Map(this.grades().map(g => [g.id, g]));
    const stageById = new Map(this.stages().map(s => [s.id, s]));
    const map = new Map<string, Fee[]>();

    for (const fee of this.filteredFees()) {
      const list = map.get(fee.gradeId) ?? [];
      list.push(fee);
      map.set(fee.gradeId, list);
    }

    return [...map.entries()]
      .map(([gradeId, fees]) => {
        const grade = gradeById.get(gradeId);
        const stage = grade ? stageById.get(grade.stageId) : undefined;
        return {
          gradeId,
          gradeName: this.localized(grade) || '—',
          stageName: this.localized(stage) || '',
          fees: [...fees].sort((a, b) => Number(b.isMandatory) - Number(a.isMandatory) || a.name.localeCompare(b.name)),
          mandatoryTotals: this.sumByCurrency(fees.filter(f => f.isMandatory)),
          allTotals: this.sumByCurrency(fees)
        };
      })
      .sort((a, b) => a.stageName.localeCompare(b.stageName) || a.gradeName.localeCompare(b.gradeName));
  });

  hasActiveFilters = computed(() =>
    !!(this.searchTerm() || this.stageFilter() || this.gradeFilter() || this.typeFilter() !== '' || this.mandatoryFilter())
  );

  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;

  constructor(
    private feeService: FeeService,
    private gradeService: GradeService,
    private languageService: LanguageService,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

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
      fees: this.feeService.getAll(),
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getAllGrades()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ fees, stages, grades }) => {
          this.stages.set(stages.data || []);
          this.grades.set(grades.data || []);
          if (fees.status === 200) {
            this.fees.set(fees.data || []);
          } else {
            this.errorMessage.set(fees.message || 'Failed to load fees');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading fees:', error);
          this.errorMessage.set('Failed to load fees. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Filters
  // ---------------------------------------------------------------------------

  setFilter(filter: 'search' | 'stage' | 'grade' | 'type' | 'mandatory', event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLSelectElement).value;
    switch (filter) {
      case 'search': this.searchTerm.set(value); break;
      case 'stage':
        this.stageFilter.set(value);
        // Drop a grade filter that isn't in the new stage
        if (this.gradeFilter() && !this.filterGrades().some(g => g.id === this.gradeFilter())) {
          this.gradeFilter.set('');
        }
        break;
      case 'grade': this.gradeFilter.set(value); break;
      case 'type': this.typeFilter.set(value); break;
      case 'mandatory': this.mandatoryFilter.set(value as '' | 'mandatory' | 'optional'); break;
    }
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.stageFilter.set('');
    this.gradeFilter.set('');
    this.typeFilter.set('');
    this.mandatoryFilter.set('');
  }

  // ---------------------------------------------------------------------------
  // Navigation
  // ---------------------------------------------------------------------------

  navigateToCreate(gradeId?: string): void {
    this.router.navigate(['/financial/fees/save'], gradeId ? { queryParams: { gradeId } } : {});
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/financial/fees/save', id]);
  }

  // ---------------------------------------------------------------------------
  // Delete
  // ---------------------------------------------------------------------------

  openDeleteConfirmation(fee: Fee, content: any): void {
    this.feeToDelete.set(fee);
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  confirmDelete(): void {
    const fee = this.feeToDelete();
    if (!fee) return;

    this.isDeleting.set(true);

    this.feeService
      .delete(fee.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(response.message || 'Fee deleted successfully');
            this.deleteModalRef?.close();
            this.feeToDelete.set(null);
            this.loadData();

            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.errorMessage.set(response.message || 'Failed to delete fee');
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting fee:', error);
          this.errorMessage.set('Failed to delete fee. Please try again.');
          this.deleteModalRef?.close();
          this.feeToDelete.set(null);
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.feeToDelete.set(null);
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  private localized(item: { nameAr?: string; nameEn?: string } | undefined): string {
    if (!item) return '';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  getName(item: { nameAr?: string; nameEn?: string }): string {
    return this.localized(item);
  }

  gradeNameOf(fee: Fee): string {
    return this.localized(this.grades().find(g => g.id === fee.gradeId)) || '—';
  }

  typeKey(type: FeeType): string {
    return this.feeTypes.find(t => t.value === type)?.key ?? 'fees.types.other';
  }

  formatAmount(amount: number, currency: string): string {
    const formatted = (amount ?? 0).toLocaleString(undefined, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
    return `${formatted} ${currency}`;
  }

  private sumByCurrency(fees: Fee[]): CurrencyTotal[] {
    const map = new Map<string, number>();
    for (const f of fees) {
      map.set(f.currency, (map.get(f.currency) || 0) + f.amount);
    }
    return [...map.entries()].map(([currency, amount]) => ({ currency, amount }));
  }
}
