import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { EmployeeGroupService } from '../../core/services/employee-group.service';
import { EmployeeGroup, EMPLOYEE_GROUP_ERROR_KEYS } from '../../core/models/employee-group.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

type StatusFilter = '' | 'active' | 'inactive';

@Component({
  selector: 'app-employee-group-list',
  standalone: false,
  styleUrl: './employee-group-list.component.scss',
  templateUrl: './employee-group-list.component.html',
})
export class EmployeeGroupListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  groups = signal<EmployeeGroup[]>([]);
  search = signal('');
  statusFilter = signal<StatusFilter>('');
  currentLanguage = signal<string>('en');
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);
  /** Id of the group whose active status is being changed */
  togglingId = signal<string | null>(null);

  /**
   * Groups matching the search box and status filter
   */
  filteredGroups = computed(() => {
    const term = this.search().trim().toLowerCase();
    const status = this.statusFilter();
    return this.groups().filter(g =>
      (!status || (status === 'active') === g.isActive) &&
      (!term ||
        g.nameAr.toLowerCase().includes(term) ||
        g.nameEn.toLowerCase().includes(term) ||
        (g.description || '').toLowerCase().includes(term))
    );
  });

  hasFilters = computed(() => !!this.search().trim() || !!this.statusFilter());

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  groupToDelete = signal<EmployeeGroup | null>(null);

  constructor(
    private employeeGroupService: EmployeeGroupService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe((language) => this.currentLanguage.set(language));

    this.loadGroups();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all employee groups from API
   */
  loadGroups(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.employeeGroupService
      .getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.groups.set(response.data);
          } else {
            this.errorMessage.set(response.message || this.translate.instant('employeeGroups.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading employee groups:', error);
          this.errorMessage.set(this.translate.instant('employeeGroups.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  onStatusFilterChange(event: Event): void {
    this.statusFilter.set((event.target as HTMLSelectElement).value as StatusFilter);
  }

  clearFilters(): void {
    this.search.set('');
    this.statusFilter.set('');
  }

  /**
   * Localized group name for the current language
   */
  getName(group: EmployeeGroup): string {
    return this.currentLanguage() === 'ar' ? group.nameAr || group.nameEn : group.nameEn || group.nameAr;
  }

  navigateToCreate(): void {
    this.router.navigate(['/users/employee-groups/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/users/employee-groups/save', id]);
  }

  /**
   * Activate or deactivate a group
   */
  toggleActive(group: EmployeeGroup): void {
    if (this.togglingId()) {
      return;
    }

    this.togglingId.set(group.id);
    this.errorMessage.set(null);

    const request$ = group.isActive
      ? this.employeeGroupService.deactivate(group.id)
      : this.employeeGroupService.activate(group.id);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.groups.update(list => list.map(g => g.id === group.id ? { ...g, isActive: !group.isActive } : g));
          this.showSuccess(this.translate.instant(group.isActive ? 'employeeGroups.deactivated' : 'employeeGroups.activated'));
        } else {
          this.errorMessage.set(response.message || this.translate.instant('employeeGroups.statusFailed'));
        }
        this.togglingId.set(null);
      },
      error: (error) => {
        console.error('Error changing employee group status:', error);
        this.errorMessage.set(this.apiError(error, 'employeeGroups.statusFailed'));
        this.togglingId.set(null);
      }
    });
  }

  /**
   * Open delete confirmation modal
   * @param group - The group to delete
   * @param content - Modal template reference
   */
  openDeleteConfirmation(group: EmployeeGroup, content: any): void {
    this.groupToDelete.set(group);
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  /**
   * Confirm and delete group
   */
  confirmDelete(): void {
    const group = this.groupToDelete();
    if (!group || this.isDeleting()) {
      return;
    }

    this.isDeleting.set(true);

    this.employeeGroupService
      .delete(group.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.closeDeleteModal();
            this.showSuccess(this.translate.instant('employeeGroups.deleted'));
            this.loadGroups();
          } else {
            this.successMessage.set(null);
            this.errorMessage.set(response.message || this.translate.instant('employeeGroups.deleteFailed'));
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting employee group:', error);
          this.successMessage.set(null);
          this.errorMessage.set(this.apiError(error, 'employeeGroups.deleteFailed'));
          this.closeDeleteModal();
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.closeDeleteModal();
  }

  private closeDeleteModal(): void {
    this.deleteModalRef?.close();
    this.deleteModalRef = null;
    this.groupToDelete.set(null);
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    // Auto-dismiss success message after 4 seconds
    timer(4000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.successMessage.set(null));
  }

  /**
   * Translate a known API error key (e.g. EmployeeGroupHasEmployees), otherwise use the fallback
   */
  private apiError(error: any, fallbackKey: string): string {
    const key = error?.error?.message;
    return (EMPLOYEE_GROUP_ERROR_KEYS as readonly string[]).includes(key)
      ? this.translate.instant(`employeeGroups.errors.${key}`)
      : this.translate.instant(fallbackKey);
  }
}
