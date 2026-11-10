import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { AcademicYear, CreateAcademicYearRequest, UpdateAcademicYearRequest } from '../../core/models/academic-year.model';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

/**
 * Custom validator for end date
 * Ensures end date is greater than start date
 */
function endDateGreaterThanStartDate(formGroup: FormGroup): { [key: string]: boolean } | null {
  const startDate = formGroup.get('startDate');
  const endDate = formGroup.get('endDate');

  if (startDate && endDate && startDate.value && endDate.value) {
    const start = new Date(startDate.value);
    const end = new Date(endDate.value);

    if (start >= end) {
      endDate.setErrors({ 'endDateMustBeGreater': true });
      return { 'endDateMustBeGreater': true };
    }
  }

  return null;
}

/**
 * Custom validator for semester end date
 * Ensures semester end date is greater than start date
 */
function semesterEndDateGreaterThanStartDate(formGroup: FormGroup): { [key: string]: boolean } | null {
  const startDate = formGroup.get('startDate');
  const endDate = formGroup.get('endDate');

  if (startDate && endDate && startDate.value && endDate.value) {
    const start = new Date(startDate.value);
    const end = new Date(endDate.value);

    if (start >= end) {
      endDate.setErrors({ 'endDateMustBeGreater': true });
      return { 'endDateMustBeGreater': true };
    }
  }

  return null;
}

@Component({
  selector: 'app-academic-year-save',
  standalone: false,
  styleUrl: './academic-year-save.component.scss',
  templateUrl: './academic-year-save.component.html',
})
export class AcademicYearSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  academicYearId: string | null = null;
  existingAcademicYear: AcademicYear | null = null;

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private academicYearService: AcademicYearService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.academicYearId = this.activatedRoute.snapshot.paramMap.get('id');

    if (this.academicYearId) {
      this.isEditMode.set(true);
      this.loadAcademicYear(this.academicYearId);
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
    this.form = this.formBuilder.group(
      {
        name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
        startDate: ['', Validators.required],
        endDate: ['', Validators.required],
        isActive: [true],
        semesters: this.formBuilder.array([])
      },
      { validators: endDateGreaterThanStartDate }
    );
  }

  /**
   * Get semesters FormArray
   */
  get semesters(): FormArray {
    return this.form.get('semesters') as FormArray;
  }

  /**
   * Add a new semester to the form
   */
  addSemester(): void {
    const semesterForm = this.formBuilder.group(
      {
        id: [this.generateGUID()],
        name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
        startDate: ['', Validators.required],
        endDate: ['', Validators.required]
      },
      { validators: semesterEndDateGreaterThanStartDate }
    );
    this.semesters.push(semesterForm);
  }

  /**
   * Remove a semester from the form
   * @param index - Index of the semester to remove
   */
  removeSemester(index: number): void {
    if (this.semesters.length > 0) {
      this.semesters.removeAt(index);
    }
  }

  /**
   * Load academic year data for edit mode
   * @param id - Academic year ID
   */
  private loadAcademicYear(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    // Retrieve the academic year by ID
    this.academicYearService
      .getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.existingAcademicYear = response.data;
            this.populateForm(response.data);
            this.errorMessage.set(null);
          } else {
            this.errorMessage.set(
              response.message || 'Failed to load academic year'
            );
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading academic year:', error);
          this.errorMessage.set('Failed to load academic year. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Populate form with academic year data
   * @param academicYear - The academic year to load
   */
  private populateForm(academicYear: AcademicYear): void {
    this.form.patchValue({
      name: academicYear.name,
      startDate: academicYear.startDate,
      endDate: academicYear.endDate,
      isActive: academicYear.isActive
    });

    // Clear existing semesters
    while (this.semesters.length > 0) {
      this.semesters.removeAt(0);
    }

    // Add semesters from the loaded academic year
    if (academicYear.semesters && academicYear.semesters.length > 0) {
      academicYear.semesters.forEach(semester => {
        const semesterForm = this.formBuilder.group(
          {
            id: [semester.id || this.generateGUID()],
            name: [semester.name, [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
            startDate: [semester.startDate, Validators.required],
            endDate: [semester.endDate, Validators.required]
          },
          { validators: semesterEndDateGreaterThanStartDate }
        );
        this.semesters.push(semesterForm);
      });
    }
  }

  /**
   * Submit the form
   */
  onSubmit(): void {
    if (!this.form.valid) {
      this.errorMessage.set('Please fix form errors before submitting');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    if (this.isEditMode()) {
      this.updateAcademicYear();
    } else {
      this.createAcademicYear();
    }
  }

  /**
   * Create a new academic year
   */
  private createAcademicYear(): void {
    const formValue = this.form.value;
    const request: CreateAcademicYearRequest = {
      id: this.generateGUID(),
      name: formValue.name,
      startDate: formValue.startDate,
      endDate: formValue.endDate,
      isActive: formValue.isActive,
      semesters: formValue.semesters || []
    };

    this.academicYearService
      .create(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.successMessage.set(
              response.message || 'Academic year created successfully'
            );
            this.isSaving.set(false);
            // Navigate back after 1 second
            setTimeout(() => {
              this.router.navigate(['/management/academic-years']);
            }, 1000);
          } else {
            this.errorMessage.set(
              response.message || 'Failed to create academic year'
            );
            this.isSaving.set(false);
          }
        },
        error: (error) => {
          console.error('Error creating academic year:', error);
          this.errorMessage.set('Failed to create academic year. Please try again.');
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Update an existing academic year
   */
  private updateAcademicYear(): void {
    const formValue = this.form.value;
    const request: UpdateAcademicYearRequest = {
      id: this.academicYearId!,
      name: formValue.name,
      startDate: formValue.startDate,
      endDate: formValue.endDate,
      isActive: formValue.isActive,
      semesters: formValue.semesters || []
    };

    this.academicYearService
      .update(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.successMessage.set(
              response.message || 'Academic year updated successfully'
            );
            this.isSaving.set(false);
            // Navigate back after 1 second
            setTimeout(() => {
              this.router.navigate(['/management/academic-years']);
            }, 1000);
          } else {
            this.errorMessage.set(
              response.message || 'Failed to update academic year'
            );
            this.isSaving.set(false);
          }
        },
        error: (error) => {
          console.error('Error updating academic year:', error);
          this.errorMessage.set('Failed to update academic year. Please try again.');
          this.isSaving.set(false);
        }
      });
  }

  /**
   * Cancel and navigate back
   */
  onCancel(): void {
    this.router.navigate(['/management/academic-years']);
  }

  /**
   * Generate a GUID (UUID v4)
   * @returns Generated GUID string
   */
  private generateGUID(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }

  /**
   * Get form control for template
   * @param controlName - Name of the form control
   */
  getControl(controlName: string) {
    return this.form.get(controlName);
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
   * Check if a field has been touched
   * @param fieldName - Name of the field
   */
  isTouched(fieldName: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && (control.dirty || control.touched));
  }

  /**
   * Check if a semester field has error
   * @param index - Semester index
   * @param fieldName - Field name
   * @param errorType - Error type
   */
  hasSemesterError(index: number, fieldName: string, errorType: string): boolean {
    const control = this.semesters.at(index)?.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  /**
   * Get semester FormControl by index
   * @param index - Semester index
   * @returns FormControl for semester name
   */
  getSemesterControl(index: number) {
    return this.semesters.at(index)?.get('name');
  }
}
