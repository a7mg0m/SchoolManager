import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { VaccinationCampaignService } from '../../core/services/vaccination-campaign.service';
import { VaccineService } from '../../core/services/vaccine.service';
import { CreateVaccinationCampaignRequest, VaccinationCampaign } from '../../core/models/vaccination-campaign.model';
import { Vaccine } from '../../core/models/vaccine.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { clinicApiError } from '../clinic-errors';

/** The end date can't be before the start date */
function endNotBeforeStart(group: FormGroup): ValidationErrors | null {
  const start = group.get('startDate')?.value;
  const end = group.get('endDate')?.value;
  return start && end && end < start ? { endBeforeStart: true } : null;
}

@Component({
  selector: 'app-vaccination-campaign-save',
  standalone: false,
  styleUrl: './vaccination-campaign-save.component.scss',
  templateUrl: './vaccination-campaign-save.component.html',
})
export class VaccinationCampaignSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  vaccines = signal<Vaccine[]>([]);
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  currentLanguage = signal<string>('en');
  campaignId: string | null = null;

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private campaignService: VaccinationCampaignService,
    private vaccineService: VaccineService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.campaignId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.campaignId);
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.form = this.formBuilder.group(
      {
        vaccineId: ['', Validators.required],
        title: ['', [Validators.required, Validators.maxLength(200)]],
        startDate: ['', Validators.required],
        endDate: ['', Validators.required]
      },
      { validators: endNotBeforeStart }
    );
  }

  /** Vaccines for the dropdown (all of them when editing, so an inactive one still shows) and the campaign */
  private loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      vaccines: this.isEditMode() ? this.vaccineService.getAll() : this.vaccineService.getActive(),
      campaign: this.campaignId ? this.campaignService.getById(this.campaignId) : of(null)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ vaccines, campaign }) => {
          this.vaccines.set(vaccines.data || []);
          if (campaign) {
            if (campaign.status === 200 && campaign.data) {
              this.populateForm(campaign.data);
            } else {
              this.errorMessage.set(campaign.message || this.translate.instant('clinic.loadFailed'));
            }
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading vaccination campaign:', error);
          this.errorMessage.set(this.translate.instant('clinic.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(campaign: VaccinationCampaign): void {
    this.form.patchValue({
      vaccineId: campaign.vaccineId,
      title: campaign.title,
      startDate: (campaign.startDate || '').substring(0, 10),
      endDate: (campaign.endDate || '').substring(0, 10)
    });
  }

  hasError(field: string, error: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.hasError(error) && (control.dirty || control.touched));
  }

  vaccineName(vaccine: Vaccine): string {
    return this.currentLanguage() === 'ar' ? vaccine.nameAr : vaccine.nameEn;
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
    const request: CreateVaccinationCampaignRequest = {
      vaccineId: v.vaccineId,
      title: String(v.title).trim(),
      startDate: v.startDate,
      endDate: v.endDate
    };

    const request$ = this.isEditMode()
      ? this.campaignService.update({ ...request, id: this.campaignId! })
      : this.campaignService.create(request);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.successMessage.set(this.translate.instant('clinic.saved'));
          this.isSaving.set(false);
          // Navigate back after 1 second
          setTimeout(() => this.router.navigate(['/clinic/vaccination-campaigns']), 1000);
        } else {
          this.errorMessage.set(clinicApiError(this.translate, response.message, 'clinic.saveFailed'));
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving vaccination campaign:', error);
        this.errorMessage.set(clinicApiError(this.translate, error?.error?.message, 'clinic.saveFailed'));
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/clinic/vaccination-campaigns']);
  }
}
