import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SchoolInfoService } from '../../core/services/school-info.service';
import { SchoolInfo, CreateSchoolInfoRequest, UpdateSchoolInfoRequest } from '../../core/models/school-info.model';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-school-info',
  standalone: false,
  styleUrl: './school-info.component.scss',
  templateUrl: './school-info.component.html',
})
export class SchoolInfoComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  existingSchoolInfo: SchoolInfo | null = null;
  logoPreview = signal<string | null>(null);
  logoError = signal<string | null>(null);

  private readonly maxLogoSizeBytes = 1024 * 1024; // 1 MB

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private schoolInfoService: SchoolInfoService
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.loadSchoolInfo();
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
      nameAr: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      nameEn: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      email: ['', Validators.email],
      phone: ['', Validators.pattern(/^[+]?[(]?[0-9]{3}[)]?[-\s.]?[0-9]{3}[-\s.]?[0-9]{4,6}$/)],
      address: ['', Validators.maxLength(300)],
      logo: [null]
    });
  }

  /**
   * Load existing school information
   */
  private loadSchoolInfo(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.schoolInfoService.get()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.existingSchoolInfo = response.data;
            this.isEditMode.set(true);
            this.populateForm(response.data);
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading school info:', error);
          this.isEditMode.set(false);
          this.isLoading.set(false);
          // Don't show error if it's a 404 (no school info exists yet)
          if (error.status !== 404) {
            this.errorMessage.set(error.error?.message || 'Failed to load school information');
          }
        }
      });
  }

  /**
   * Populate the form with existing school information
   */
  private populateForm(schoolInfo: SchoolInfo): void {
    this.form.patchValue({
      nameAr: schoolInfo.nameAr,
      nameEn: schoolInfo.nameEn,
      address: schoolInfo.address || '',
      phone: schoolInfo.phone || '',
      email: schoolInfo.email || '',
      logo: schoolInfo.logo || null
    });
    this.logoPreview.set(this.toImageSrc(schoolInfo.logo));
    this.logoError.set(null);
  }

  /**
   * Read the selected image file and store it as base64 (backend expects byte[])
   */
  onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    this.logoError.set(null);
    if (!file) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.logoError.set('Please select an image file');
      input.value = '';
      return;
    }

    if (file.size > this.maxLogoSizeBytes) {
      this.logoError.set('Logo must not exceed 1 MB');
      input.value = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = reader.result as string;
      // Strip the "data:image/...;base64," prefix so only the base64 payload is sent
      const base64 = dataUrl.substring(dataUrl.indexOf(',') + 1);
      this.form.patchValue({ logo: base64 });
      this.form.get('logo')?.markAsDirty();
      this.logoPreview.set(dataUrl);
    };
    reader.onerror = () => this.logoError.set('Failed to read the selected file');
    reader.readAsDataURL(file);
  }

  /**
   * Remove the current logo
   */
  removeLogo(fileInput: HTMLInputElement): void {
    fileInput.value = '';
    this.form.patchValue({ logo: null });
    this.form.get('logo')?.markAsDirty();
    this.logoPreview.set(null);
    this.logoError.set(null);
  }

  /**
   * Convert a base64 logo from the API into an <img> src
   */
  private toImageSrc(logo: string | null | undefined): string | null {
    return SchoolInfoService.toImageSrc(logo);
  }

  /**
   * Submit the form to create or update school information
   */
  onSubmit(): void {
    if (!this.form.valid) {
      this.errorMessage.set('Please fill in all required fields correctly');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const formValue = { ...this.form.value, logo: this.form.value.logo || null };

    if (this.isEditMode()) {
      // Update existing school info
      const updateRequest: UpdateSchoolInfoRequest = {
        id: this.existingSchoolInfo?.id || '',
        ...formValue
      };

      this.schoolInfoService.update(updateRequest)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            this.isSaving.set(false);
            if (response.status === 200) {
              this.successMessage.set(response.message || 'School information updated successfully');
              if (response.data) {
                this.existingSchoolInfo = response.data;
                this.populateForm(response.data);
              } else {
                this.existingSchoolInfo = { ...formValue, id: this.existingSchoolInfo?.id };
              }
              // Sidebar shows the school name
              this.schoolInfoService.setCurrent(this.existingSchoolInfo);
              setTimeout(() => this.successMessage.set(null), 3000);
            } else {
              this.errorMessage.set(response.message || 'Failed to update school information');
            }
          },
          error: (error) => {
            this.isSaving.set(false);
            console.error('Error updating school info:', error);
            this.errorMessage.set(error.error?.message || 'An error occurred while updating school information');
          }
        });
    } else {
      // Create new school info
      const createRequest: CreateSchoolInfoRequest = formValue;

      this.schoolInfoService.create(createRequest)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            this.isSaving.set(false);
            if (response.status === 200) {
              this.successMessage.set(response.message || 'School information created successfully');
              if (response.data) {
                this.existingSchoolInfo = response.data;
                this.populateForm(response.data);
              } else {
                // Create endpoint returns only the inserted id
                this.existingSchoolInfo = { ...formValue, id: response.insertedId || undefined };
              }
              // Sidebar shows the school name
              this.schoolInfoService.setCurrent(this.existingSchoolInfo);
              this.isEditMode.set(true);
              setTimeout(() => this.successMessage.set(null), 3000);
            } else {
              this.errorMessage.set(response.message || 'Failed to create school information');
            }
          },
          error: (error) => {
            this.isSaving.set(false);
            console.error('Error creating school info:', error);
            this.errorMessage.set(error.error?.message || 'An error occurred while creating school information');
          }
        });
    }
  }

  /**
   * Reset form to initial state
   */
  onReset(): void {
    if (this.isEditMode() && this.existingSchoolInfo) {
      this.populateForm(this.existingSchoolInfo);
    } else {
      this.form.reset();
      this.logoPreview.set(null);
    }
    this.logoError.set(null);
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  /**
   * Check if a specific field has an error
   */
  hasError(fieldName: string, errorType: string): boolean {
    const field = this.form.get(fieldName);
    return field ? field.hasError(errorType) && (field.dirty || field.touched) : false;
  }
}
