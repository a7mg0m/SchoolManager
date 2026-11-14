import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
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

@Component({
  selector: 'app-teacher-save',
  standalone: false,
  styleUrl: './teacher-save.component.scss',
  templateUrl: './teacher-save.component.html',
})
export class TeacherSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  teacherId: string | null = null;

  readonly genders: TeacherGender[] = ['Male', 'Female'];
  /** Latest allowed date of birth (today), ISO yyyy-MM-dd */
  readonly maxDateOfBirth = new Date().toISOString().substring(0, 10);

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
    this.initializeForm();
    this.teacherId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.teacherId);

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
      isActive: [true]
    });
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
      isActive: teacher.isActive ?? true
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

    if (this.isEditMode()) {
      this.updateTeacher();
    } else {
      this.createTeacher();
    }
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
      isActive: !!v.isActive
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
      isActive: !!v.isActive
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
