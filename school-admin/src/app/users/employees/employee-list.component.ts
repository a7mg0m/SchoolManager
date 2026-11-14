import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeGroupService } from '../../core/services/employee-group.service';
import { Employee, EMPLOYEE_ERROR_KEYS } from '../../core/models/employee.model';
import { EmployeeGroup } from '../../core/models/employee-group.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, timer, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

type StatusFilter = '' | 'active' | 'inactive';

@Component({
  selector: 'app-employee-list',
  standalone: false,
  styleUrl: './employee-list.component.scss',
  templateUrl: './employee-list.component.html',
})
export class EmployeeListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  employees = signal<Employee[]>([]);
  groups = signal<EmployeeGroup[]>([]);
  search = signal('');
  groupFilter = signal('');
  statusFilter = signal<StatusFilter>('');
  currentLanguage = signal<string>('en');
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);
  /** Id of the employee whose active status is being changed */
  togglingId = signal<string | null>(null);
  employeeToDelete = signal<Employee | null>(null);

  /**
   * Employees matching the search box, group and status filters
   */
  filteredEmployees = computed(() => {
    const term = this.search().trim().toLowerCase();
    const groupId = this.groupFilter();
    const status = this.statusFilter();
    return this.employees().filter(e =>
      (!groupId || e.employeeGroupId === groupId) &&
      (!status || (status === 'active') === e.isActive) &&
      (!term || [e.fullNameAr, e.fullNameEn, e.nationalId, e.phoneNumber, e.email, e.jobTitle, e.loginName]
        .some(v => (v || '').toLowerCase().includes(term)))
    );
  });

  hasFilters = computed(() => !!this.search().trim() || !!this.groupFilter() || !!this.statusFilter());

  stats = computed(() => {
    const list = this.employees();
    const active = list.filter(e => e.isActive).length;
    return { total: list.length, active, inactive: list.length - active };
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;

  constructor(
    private employeeService: EmployeeService,
    private employeeGroupService: EmployeeGroupService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    // Opened from the employee groups list: /users/employees?groupId=...
    this.groupFilter.set(this.activatedRoute.snapshot.queryParamMap.get('groupId') || '');

    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe((language) => this.currentLanguage.set(language));

    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load employees and groups (all groups, so employees of inactive groups still show their group)
   */
  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      employees: this.employeeService.getAll(),
      groups: this.employeeGroupService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ employees, groups }) => {
          this.groups.set(groups.status === 200 && groups.data ? groups.data : []);
          if (employees.status === 200 && employees.data) {
            this.employees.set(employees.data);
          } else {
            this.errorMessage.set(employees.message || this.translate.instant('employees.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading employees:', error);
          this.errorMessage.set(this.translate.instant('employees.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  onGroupFilterChange(event: Event): void {
    this.groupFilter.set((event.target as HTMLSelectElement).value);
  }

  onStatusFilterChange(event: Event): void {
    this.statusFilter.set((event.target as HTMLSelectElement).value as StatusFilter);
  }

  clearFilters(): void {
    this.search.set('');
    this.groupFilter.set('');
    this.statusFilter.set('');
  }

  /**
   * Localized employee name
   */
  getName(employee: Employee): string {
    return this.currentLanguage() === 'ar'
      ? employee.fullNameAr || employee.fullNameEn
      : employee.fullNameEn || employee.fullNameAr;
  }

  /**
   * Localized name of the employee's group
   */
  getGroupName(employee: Employee): string {
    const ar = employee.employeeGroupNameAr;
    const en = employee.employeeGroupNameEn;
    return (this.currentLanguage() === 'ar' ? ar || en : en || ar) || '—';
  }

  /**
   * Localized group name for the filter dropdown
   */
  getGroupOptionName(group: EmployeeGroup): string {
    return this.currentLanguage() === 'ar' ? group.nameAr || group.nameEn : group.nameEn || group.nameAr;
  }

  navigateToCreate(): void {
    const groupId = this.groupFilter();
    this.router.navigate(['/users/employees/save'], groupId ? { queryParams: { groupId } } : {});
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/users/employees/save', id]);
  }

  /**
   * Activate or deactivate an employee (the API does the same for its login)
   */
  toggleActive(employee: Employee): void {
    if (this.togglingId()) {
      return;
    }

    this.togglingId.set(employee.id);
    this.errorMessage.set(null);

    const request$ = employee.isActive
      ? this.employeeService.deactivate(employee.id)
      : this.employeeService.activate(employee.id);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.employees.update(list => list.map(e => e.id === employee.id ? { ...e, isActive: !employee.isActive } : e));
          this.showSuccess(this.translate.instant(employee.isActive ? 'employees.deactivated' : 'employees.activated'));
        } else {
          this.errorMessage.set(response.message || this.translate.instant('employees.statusFailed'));
        }
        this.togglingId.set(null);
      },
      error: (error) => {
        console.error('Error changing employee status:', error);
        this.errorMessage.set(this.apiError(error, 'employees.statusFailed'));
        this.togglingId.set(null);
      }
    });
  }

  /**
   * Open delete confirmation modal
   */
  openDeleteConfirmation(employee: Employee, content: any): void {
    this.employeeToDelete.set(employee);
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  /**
   * Confirm and delete the employee (its User and UserProfile are deleted too)
   */
  confirmDelete(): void {
    const employee = this.employeeToDelete();
    if (!employee || this.isDeleting()) {
      return;
    }

    this.isDeleting.set(true);

    this.employeeService
      .delete(employee.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.closeDeleteModal();
            this.employees.update(list => list.filter(e => e.id !== employee.id));
            this.showSuccess(this.translate.instant('employees.deleted'));
          } else {
            this.successMessage.set(null);
            this.errorMessage.set(response.message || this.translate.instant('employees.deleteFailed'));
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting employee:', error);
          this.successMessage.set(null);
          this.errorMessage.set(this.apiError(error, 'employees.deleteFailed'));
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
    this.employeeToDelete.set(null);
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    // Auto-dismiss success message after 4 seconds
    timer(4000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.successMessage.set(null));
  }

  /**
   * Translate a known API error key, otherwise use the fallback
   */
  private apiError(error: any, fallbackKey: string): string {
    const key = error?.error?.message;
    return (EMPLOYEE_ERROR_KEYS as readonly string[]).includes(key)
      ? this.translate.instant(`employees.errors.${key}`)
      : this.translate.instant(fallbackKey);
  }
}
