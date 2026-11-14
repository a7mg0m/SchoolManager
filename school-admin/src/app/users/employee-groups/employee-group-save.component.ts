import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { EmployeeGroupService } from '../../core/services/employee-group.service';
import {
  EmployeeGroup,
  CreateEmployeeGroupRequest,
  UpdateEmployeeGroupRequest,
  EMPLOYEE_GROUP_ERROR_KEYS
} from '../../core/models/employee-group.model';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-employee-group-save',
  standalone: false,
  styleUrl: './employee-group-save.component.scss',
  templateUrl: './employee-group-save.component.html',
})
export class EmployeeGroupSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  /** Employees in the group being edited (shown as info) */
  employeesCount = signal(0);
  groupId: string | null = null;

  readonly descriptionMaxLength = 500;

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private employeeGroupService: EmployeeGroupService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.groupId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.groupId);

    if (this.groupId) {
      this.loadGroup(this.groupId);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Initialize the form with default values
   */
  private initializeForm(): void {
    this.form = this.formBuilder.group({
      nameAr: ['', [Validators.required, Validators.maxLength(200)]],
      nameEn: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(this.descriptionMaxLength)]],
      // Only used on create; in edit mode status is changed from the list (activate / deactivate)
      isActive: [true]
    });
  }

  /**
   * Load employee group data for edit mode
   * @param id - Employee group ID
   */
  private loadGroup(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.employeeGroupService
      .getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.populateForm(response.data);
          } else {
            this.errorMessage.set(response.message || this.translate.instant('employeeGroups.loadGroupFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading employee group:', error);
          this.errorMessage.set(this.apiError(error, 'employeeGroups.loadGroupFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Populate form with employee group data
   */
  private populateForm(group: EmployeeGroup): void {
    this.form.patchValue({
      nameAr: group.nameAr,
      nameEn: group.nameEn,
      description: group.description ?? '',
      isActive: group.isActive
    });
    this.employeesCount.set(group.employeesCount);
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
      this.errorMessage.set(this.translate.instant('employeeGroups.invalidForm'));
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    if (this.isEditMode()) {
      this.updateGroup();
    } else {
      this.createGroup();
    }
  }

  /**
   * Create a new employee group
   */
  private createGroup(): void {
    const value = this.form.getRawValue();
    const request: CreateEmployeeGroupRequest = {
      nameAr: value.nameAr.trim(),
      nameEn: value.nameEn.trim(),
      description: value.description?.trim() || null,
      isActive: !!value.isActive
    };

    this.employeeGroupService
      .create(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => this.handleSaveResponse(response.status, response.message, 'employeeGroups.created', 'employeeGroups.createFailed'),
        error: (error) => {
          console.error('Error creating employee group:', error);
          this.errorMessage.set(this.apiError(error, 'employeeGroups.createFailed'));
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Update an existing employee group
   */
  private updateGroup(): void {
    const value = this.form.getRawValue();
    const request: UpdateEmployeeGroupRequest = {
      id: this.groupId!,
      nameAr: value.nameAr.trim(),
      nameEn: value.nameEn.trim(),
      description: value.description?.trim() || null
    };

    this.employeeGroupService
      .update(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => this.handleSaveResponse(response.status, response.message, 'employeeGroups.updated', 'employeeGroups.updateFailed'),
        error: (error) => {
          console.error('Error updating employee group:', error);
          this.errorMessage.set(this.apiError(error, 'employeeGroups.updateFailed'));
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Show the result of create / update and go back to the list on success
   */
  private handleSaveResponse(status: number, message: string | undefined, successKey: string, failedKey: string): void {
    // Create returns 201 (CreatedAtAction) with status 200 in the body
    if (status === 200 || status === 201) {
      this.successMessage.set(this.translate.instant(successKey));
      // Navigate back after 1 second (keep isSaving to block duplicate submits)
      setTimeout(() => this.router.navigate(['/users/employee-groups']), 1000);
    } else {
      this.errorMessage.set(message || this.translate.instant(failedKey));
      this.isSaving.set(false);
    }
  }

  /**
   * Cancel and navigate back
   */
  onCancel(): void {
    this.router.navigate(['/users/employee-groups']);
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

  get descriptionLength(): number {
    return (this.form.get('description')?.value || '').length;
  }

  /**
   * Translate a known API error key (e.g. EmployeeGroupNameExists), otherwise use the fallback
   */
  private apiError(error: any, fallbackKey: string): string {
    const key = error?.error?.message;
    return (EMPLOYEE_GROUP_ERROR_KEYS as readonly string[]).includes(key)
      ? this.translate.instant(`employeeGroups.errors.${key}`)
      : this.translate.instant(fallbackKey);
  }
}
