import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { EmployeeService } from '../../core/services/employee.service';
import { EmployeeGroupService } from '../../core/services/employee-group.service';
import {
  Employee,
  CreateEmployeeRequest,
  UpdateEmployeeRequest,
  EMPLOYEE_ERROR_KEYS
} from '../../core/models/employee.model';
import { EmployeeGroup } from '../../core/models/employee-group.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

/** confirmPassword must equal password (only checked when a password is typed) */
const passwordsMatch: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const password = group.get('password')?.value || '';
  const confirm = group.get('confirmPassword')?.value || '';
  return password && password !== confirm ? { passwordMismatch: true } : null;
};

@Component({
  selector: 'app-employee-save',
  standalone: false,
  styleUrl: './employee-save.component.scss',
  templateUrl: './employee-save.component.html',
})
export class EmployeeSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  showPassword = signal(false);
  currentLanguage = signal<string>('en');
  groups = signal<EmployeeGroup[]>([]);
  /** Group of the employee being edited (kept selectable even if it was deactivated) */
  private currentGroupId = signal<string | null>(null);
  employeeId: string | null = null;

  readonly passwordMinLength = 6;

  /**
   * Groups shown in the dropdown: active ones, plus the employee's current group
   */
  availableGroups = computed(() => {
    const currentId = this.currentGroupId();
    return this.groups().filter(g => g.isActive || g.id === currentId);
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private employeeService: EmployeeService,
    private employeeGroupService: EmployeeGroupService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.employeeId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.employeeId);
    this.initializeForm();

    // Pre-select the group when opened from a filtered list (?groupId=...)
    const groupId = this.activatedRoute.snapshot.queryParamMap.get('groupId');
    if (!this.employeeId && groupId) {
      this.form.patchValue({ employeeGroupId: groupId });
    }

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
   * Initialize the form. Password is required on create, optional on edit (empty = keep current).
   */
  private initializeForm(): void {
    const passwordValidators = this.isEditMode()
      ? [Validators.minLength(this.passwordMinLength)]
      : [Validators.required, Validators.minLength(this.passwordMinLength)];

    this.form = this.formBuilder.group({
      fullNameAr: ['', [Validators.required, Validators.maxLength(200)]],
      fullNameEn: ['', [Validators.required, Validators.maxLength(200)]],
      nationalId: ['', [Validators.required, Validators.maxLength(50)]],
      phoneNumber: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      address: ['', [Validators.maxLength(500)]],
      jobTitle: ['', [Validators.required, Validators.maxLength(150)]],
      hireDate: [this.today(), Validators.required],
      employeeGroupId: ['', Validators.required],
      loginName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
      password: ['', passwordValidators],
      confirmPassword: [''],
      // Only used on create; in edit mode status is changed from the list (activate / deactivate)
      isActive: [true]
    }, { validators: passwordsMatch });
  }

  /**
   * Load groups and (in edit mode) the employee
   */
  private loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      groups: this.employeeGroupService.getAll(),
      employee: this.employeeId ? this.employeeService.getById(this.employeeId) : of(null)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ groups, employee }) => {
          this.groups.set(groups.status === 200 && groups.data ? groups.data : []);

          if (employee) {
            if (employee.status === 200 && employee.data) {
              this.populateForm(employee.data);
            } else {
              this.errorMessage.set(employee.message || this.translate.instant('employees.loadEmployeeFailed'));
            }
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading employee form data:', error);
          this.errorMessage.set(this.apiError(error, this.employeeId ? 'employees.loadEmployeeFailed' : 'employees.loadGroupsFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Populate form with employee data
   */
  private populateForm(employee: Employee): void {
    this.currentGroupId.set(employee.employeeGroupId);
    this.form.patchValue({
      fullNameAr: employee.fullNameAr,
      fullNameEn: employee.fullNameEn,
      nationalId: employee.nationalId,
      phoneNumber: employee.phoneNumber,
      email: employee.email ?? '',
      address: employee.address ?? '',
      jobTitle: employee.jobTitle,
      hireDate: (employee.hireDate || '').substring(0, 10),
      employeeGroupId: employee.employeeGroupId,
      loginName: employee.loginName ?? '',
      isActive: employee.isActive
    });

    // An old employee without a login needs a password to create one
    if (!employee.userId) {
      const password = this.form.get('password')!;
      password.addValidators(Validators.required);
      password.updateValueAndValidity();
    }
  }

  /**
   * Submit the form
   */
  onSubmit(): void {
    if (this.isSaving()) {
      return;
    }

    if (!this.form.valid) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.translate.instant('employees.invalidForm'));
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    if (this.isEditMode()) {
      this.updateEmployee();
    } else {
      this.createEmployee();
    }
  }

  /**
   * Values shared by create and update, trimmed
   */
  private formData() {
    const v = this.form.getRawValue();
    return {
      fullNameAr: v.fullNameAr.trim(),
      fullNameEn: v.fullNameEn.trim(),
      nationalId: v.nationalId.trim(),
      phoneNumber: v.phoneNumber.trim(),
      email: v.email.trim(),
      address: v.address?.trim() || null,
      jobTitle: v.jobTitle.trim(),
      hireDate: v.hireDate,
      employeeGroupId: v.employeeGroupId,
      loginName: v.loginName.trim()
    };
  }

  /**
   * Create a new employee (the API also creates its User and UserProfile)
   */
  private createEmployee(): void {
    const value = this.form.getRawValue();
    const request: CreateEmployeeRequest = {
      ...this.formData(),
      password: value.password,
      isActive: !!value.isActive
    };

    this.employeeService
      .create(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => this.handleSaveResponse(response.status, response.message, 'employees.created', 'employees.createFailed'),
        error: (error) => {
          console.error('Error creating employee:', error);
          this.errorMessage.set(this.apiError(error, 'employees.createFailed'));
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Update an existing employee (the API also updates its User and UserProfile)
   */
  private updateEmployee(): void {
    const password = this.form.get('password')!.value;
    const request: UpdateEmployeeRequest = {
      id: this.employeeId!,
      ...this.formData(),
      password: password ? password : null
    };

    this.employeeService
      .update(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => this.handleSaveResponse(response.status, response.message, 'employees.updated', 'employees.updateFailed'),
        error: (error) => {
          console.error('Error updating employee:', error);
          this.errorMessage.set(this.apiError(error, 'employees.updateFailed'));
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Show the result of create / update and go back to the list on success
   */
  private handleSaveResponse(status: number, message: string | undefined, successKey: string, failedKey: string): void {
    if (status === 200 || status === 201) {
      this.successMessage.set(this.translate.instant(successKey));
      // Navigate back after 1 second (keep isSaving to block duplicate submits)
      setTimeout(() => this.router.navigate(['/users/employees']), 1000);
    } else {
      this.errorMessage.set(message || this.translate.instant(failedKey));
      this.isSaving.set(false);
    }
  }

  /**
   * Cancel and navigate back
   */
  onCancel(): void {
    this.router.navigate(['/users/employees']);
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }

  /**
   * Localized group name for the dropdown
   */
  getGroupName(group: EmployeeGroup): string {
    const name = this.currentLanguage() === 'ar' ? group.nameAr || group.nameEn : group.nameEn || group.nameAr;
    return group.isActive ? name : `${name} (${this.translate.instant('common.inactive')})`;
  }

  /**
   * Check if a field has error
   * @param fieldName - Name of the field
   * @param errorType - Type of error to check
   */
  hasError(fieldName: string, errorType: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  get passwordMismatch(): boolean {
    const confirm = this.form.get('confirmPassword');
    return this.form.hasError('passwordMismatch') && !!confirm && (confirm.dirty || confirm.touched);
  }

  /** Today's date as "yyyy-MM-dd" (local time) */
  private today(): string {
    const d = new Date();
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }

  /**
   * Translate a known API error key (e.g. LoginNameExists), otherwise use the fallback
   */
  private apiError(error: any, fallbackKey: string): string {
    const key = error?.error?.message;
    return (EMPLOYEE_ERROR_KEYS as readonly string[]).includes(key)
      ? this.translate.instant(`employees.errors.${key}`)
      : this.translate.instant(fallbackKey);
  }
}
