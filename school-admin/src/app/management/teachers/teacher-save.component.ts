import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { TeacherService } from '../../core/services/teacher.service';
import {
  Teacher,
  TeacherGender,
  CreateTeacherRequest,
  UpdateTeacherRequest
} from '../../core/models/teacher.model';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

/** confirmPassword must equal password (only checked when a password is typed) */
const passwordsMatch: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const password = group.get('password')?.value || '';
  const confirm = group.get('confirmPassword')?.value || '';
  return password && password !== confirm ? { passwordMismatch: true } : null;
};

/** Error keys returned by the API for the teacher's login, with their translation */
const ACCOUNT_ERROR_KEYS: Record<string, string> = {
  LoginNameRequired: 'teachers.loginNameRequired',
  LoginNameTooShort: 'teachers.loginNameMinLength',
  LoginNameTooLong: 'teachers.loginNameMaxLength',
  LoginNameExists: 'teachers.loginNameExists',
  PasswordRequired: 'teachers.passwordRequired',
  PasswordTooShort: 'teachers.passwordMinLength'
};

@Component({
  selector: 'app-teacher-save',
  standalone: false,
  styleUrl: './teacher-save.component.scss',
  templateUrl: './teacher-save.component.html',
})
export class TeacherSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  /** yyyy-MM-dd: the end of service date can't be later */
  readonly today = new Date().toISOString().slice(0, 10);
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  teacherId: string | null = null;

  readonly genders: TeacherGender[] = ['Male', 'Female'];
  /** Latest allowed date of birth (today), ISO yyyy-MM-dd */
  readonly maxDateOfBirth = new Date().toISOString().substring(0, 10);

  /** Login account (same rules as the API: TeacherAccount) */
  showPassword = signal(false);
  readonly loginNameMinLength = 3;
  readonly loginNameMaxLength = 100;
  readonly passwordMinLength = 6;

  /** Fields the backend does not allow changing after creation */
  private readonly createOnlyFields = ['dateOfBirth', 'nationalId', 'gender'];

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private teacherService: TeacherService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Edit mode first: the password is required only when adding a teacher
    this.teacherId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.teacherId);
    this.initializeForm();

    if (this.teacherId) {
      this.createOnlyFields.forEach(f => this.form.get(f)!.disable());
      this.loadTeacher(this.teacherId);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Initialize the form (lengths mirror the backend EF configuration)
   */
  private initializeForm(): void {
    this.form = this.formBuilder.group({
      fullNameAr: ['', [Validators.required, Validators.maxLength(200)]],
      fullNameEn: ['', [Validators.required, Validators.maxLength(200)]],
      dateOfBirth: ['', Validators.required],
      nationalId: ['', [Validators.required, Validators.maxLength(50)]],
      gender: ['', Validators.required],
      phoneNumber: ['', [Validators.required, Validators.maxLength(50)]],
      // Required: the teacher's login account (Identity service) needs an email
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      address: ['', [Validators.required, Validators.maxLength(500)]],
      // Job level (المستوى الوظيفي): optional free text
      jobLevel: ['', Validators.maxLength(100)],
      isActive: [true],
      isServiceEnded: [false],
      serviceEndDate: [''],
      // Login account of the teacher (created / updated in the Identity service)
      loginName: ['', [
        Validators.required,
        Validators.minLength(this.loginNameMinLength),
        Validators.maxLength(this.loginNameMaxLength)
      ]],
      // Required on create, optional on edit (empty = keep the current one)
      password: ['', this.isEditMode()
        ? [Validators.minLength(this.passwordMinLength)]
        : [Validators.required, Validators.minLength(this.passwordMinLength)]],
      confirmPassword: ['']
    }, { validators: passwordsMatch });
  }

  /**
   * Load teacher data for edit mode
   * @param id - Teacher ID
   */
  private loadTeacher(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.teacherService
      .getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.populateForm(response.data);
          } else {
            this.errorMessage.set(
              response.message || this.translate.instant('teachers.loadTeacherFailed')
            );
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading teacher:', error);
          this.errorMessage.set(this.translate.instant('teachers.loadTeacherFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Populate form with teacher data
   * @param teacher - The teacher to load
   */
  private populateForm(teacher: Teacher): void {
    this.form.patchValue({
      fullNameAr: teacher.fullNameAr ?? '',
      fullNameEn: teacher.fullNameEn ?? '',
      dateOfBirth: teacher.dateOfBirth ? teacher.dateOfBirth.substring(0, 10) : '',
      nationalId: teacher.nationalId ?? '',
      gender: teacher.gender ?? '',
      phoneNumber: teacher.phoneNumber ?? '',
      email: teacher.email ?? '',
      address: teacher.address ?? '',
      jobLevel: teacher.jobLevel ?? '',
      isActive: teacher.isActive ?? true,
      isServiceEnded: !!teacher.isServiceEnded,
      serviceEndDate: teacher.serviceEndDate ?? '',
      // Teachers saved before login names existed sign in with their national ID
      loginName: teacher.loginName || teacher.nationalId || ''
    });
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
      this.errorMessage.set(this.translate.instant('teachers.invalidForm'));
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    // The teacher's login is created later (through Kafka), so check now that no employee,
    // parent or other teacher already uses the login name
    const loginName = this.form.get('loginName')!.value.trim();
    this.teacherService.isLoginNameAvailable(loginName, this.teacherId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data === false) {
            this.loginNameTaken();
            return;
          }
          if (this.isEditMode()) {
            this.updateTeacher();
          } else {
            this.createTeacher();
          }
        },
        error: (error) => {
          console.error('Error checking the login name:', error);
          this.errorMessage.set(this.translate.instant('teachers.loginNameCheckFailed'));
          this.isSaving.set(false);
        }
      });
  }

  /** Show "login name already used" on the field and in the page message */
  private loginNameTaken(): void {
    const control = this.form.get('loginName')!;
    control.setErrors({ ...(control.errors ?? {}), taken: true });
    control.markAsTouched();
    this.errorMessage.set(this.translate.instant('teachers.loginNameExists'));
    this.isSaving.set(false);
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }

  get passwordMismatch(): boolean {
    const confirm = this.form.get('confirmPassword');
    return this.form.hasError('passwordMismatch') && !!confirm && (confirm.dirty || confirm.touched);
  }

  /**
   * Trimmed email, or null when left empty (email is optional)
   */
  private normalizeEmail(value: string | null): string | null {
    const email = (value ?? '').trim();
    return email ? email : null;
  }

  /**
   * Create a new teacher
   */
  private createTeacher(): void {
    const v = this.form.getRawValue();
    const request: CreateTeacherRequest = {
      fullNameAr: v.fullNameAr.trim(),
      fullNameEn: v.fullNameEn.trim(),
      dateOfBirth: v.dateOfBirth,
      nationalId: v.nationalId.trim(),
      gender: v.gender,
      phoneNumber: v.phoneNumber.trim(),
      email: this.normalizeEmail(v.email),
      address: v.address.trim(),
      jobLevel: (v.jobLevel ?? '').trim() || null,
      isActive: !!v.isActive && !v.isServiceEnded,
      isServiceEnded: !!v.isServiceEnded,
      serviceEndDate: v.isServiceEnded ? (v.serviceEndDate || null) : null,
      loginName: v.loginName.trim(),
      password: v.password
    };

    this.teacherService
      .create(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => this.handleSaveResponse(response.status, response.message, 'teachers.created', 'teachers.createFailed'),
        error: (error) => {
          console.error('Error creating teacher:', error);
          this.errorMessage.set(this.translate.instant('teachers.createFailed'));
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Update an existing teacher
   */
  private updateTeacher(): void {
    const v = this.form.getRawValue();
    const request: UpdateTeacherRequest = {
      id: this.teacherId!,
      fullNameAr: v.fullNameAr.trim(),
      fullNameEn: v.fullNameEn.trim(),
      phoneNumber: v.phoneNumber.trim(),
      email: this.normalizeEmail(v.email),
      address: v.address.trim(),
      jobLevel: (v.jobLevel ?? '').trim() || null,
      isActive: !!v.isActive && !v.isServiceEnded,
      isServiceEnded: !!v.isServiceEnded,
      serviceEndDate: v.isServiceEnded ? (v.serviceEndDate || null) : null,
      loginName: v.loginName.trim(),
      // Empty: keep the current password
      password: v.password || null
    };

    this.teacherService
      .update(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => this.handleSaveResponse(response.status, response.message, 'teachers.updated', 'teachers.updateFailed'),
        error: (error) => {
          console.error('Error updating teacher:', error);
          this.errorMessage.set(this.translate.instant('teachers.updateFailed'));
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Shared success/failure handling for create and update
   */
  private handleSaveResponse(status: number, message: string, successKey: string, failKey: string): void {
    if (status === 200) {
      this.successMessage.set(this.translate.instant(successKey));
      // Navigate back after 1 second (keep isSaving to block duplicate submits)
      setTimeout(() => this.router.navigate(['/management/teachers']), 1000);
    } else if (message === 'LoginNameExists') {
      this.loginNameTaken();
    } else if (message && ACCOUNT_ERROR_KEYS[message]) {
      this.errorMessage.set(this.translate.instant(ACCOUNT_ERROR_KEYS[message], {
        min: message.startsWith('Password') ? this.passwordMinLength : this.loginNameMinLength,
        max: this.loginNameMaxLength
      }));
      this.isSaving.set(false);
    } else {
      this.errorMessage.set(message || this.translate.instant(failKey));
      this.isSaving.set(false);
    }
  }

  /**
   * Cancel and navigate back
   */
  onCancel(): void {
    this.router.navigate(['/management/teachers']);
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

  /**
   * Check if a field is invalid and has been interacted with
   */
  isInvalid(fieldName: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }
}
