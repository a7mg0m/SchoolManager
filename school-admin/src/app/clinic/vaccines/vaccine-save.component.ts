import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { VaccineService } from '../../core/services/vaccine.service';
import { CreateVaccineRequest, Vaccine } from '../../core/models/vaccine.model';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { clinicApiError } from '../clinic-errors';

@Component({
  selector: 'app-vaccine-save',
  standalone: false,
  styleUrl: './vaccine-save.component.scss',
  templateUrl: './vaccine-save.component.html',
})
export class VaccineSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  vaccineId: string | null = null;

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private vaccineService: VaccineService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.vaccineId = this.activatedRoute.snapshot.paramMap.get('id');

    if (this.vaccineId) {
      this.isEditMode.set(true);
      this.loadVaccine(this.vaccineId);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.form = this.formBuilder.group({
      nameAr: ['', [Validators.required, Validators.maxLength(200)]],
      nameEn: ['', [Validators.required, Validators.maxLength(200)]],
      totalDoses: [1, [Validators.required, Validators.min(1), Validators.max(20)]],
      isMandatory: [false]
    });
  }

  private loadVaccine(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.vaccineService
      .getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.populateForm(response.data);
          } else {
            this.errorMessage.set(response.message || this.translate.instant('clinic.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading vaccine:', error);
          this.errorMessage.set(this.translate.instant('clinic.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(vaccine: Vaccine): void {
    this.form.patchValue({
      nameAr: vaccine.nameAr,
      nameEn: vaccine.nameEn,
      totalDoses: vaccine.totalDoses,
      isMandatory: vaccine.isMandatory
    });
  }

  hasError(field: string, error: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.hasError(error) && (control.dirty || control.touched));
  }

  onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.translate.instant('clinic.fixErrors'));
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const v = this.form.value;
    const request: CreateVaccineRequest = {
      nameAr: String(v.nameAr).trim(),
      nameEn: String(v.nameEn).trim(),
      totalDoses: Number(v.totalDoses),
      isMandatory: !!v.isMandatory
    };

    const request$ = this.isEditMode()
      ? this.vaccineService.update({ ...request, id: this.vaccineId! })
      : this.vaccineService.create(request);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.successMessage.set(this.translate.instant('clinic.saved'));
          this.isSaving.set(false);
          // Navigate back after 1 second
          setTimeout(() => this.router.navigate(['/clinic/vaccines']), 1000);
        } else {
          this.errorMessage.set(clinicApiError(this.translate, response.message, 'clinic.saveFailed'));
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving vaccine:', error);
        this.errorMessage.set(clinicApiError(this.translate, error?.error?.message, 'clinic.saveFailed'));
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/clinic/vaccines']);
  }
}
