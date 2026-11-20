import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { VaccinationCampaignService } from '../../core/services/vaccination-campaign.service';
import { VaccineService } from '../../core/services/vaccine.service';
import { VaccinationCampaign } from '../../core/models/vaccination-campaign.model';
import { Vaccine } from '../../core/models/vaccine.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { formatDate } from '../../notification/shared/audience';

@Component({
  selector: 'app-vaccination-campaign-list',
  standalone: false,
  styleUrl: './vaccination-campaign-list.component.scss',
  templateUrl: './vaccination-campaign-list.component.html',
})
export class VaccinationCampaignListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  campaigns = signal<VaccinationCampaign[]>([]);
  vaccines = signal<Vaccine[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);
  currentLanguage = signal<string>('en');

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private campaignToDelete: VaccinationCampaign | null = null;

  constructor(
    private campaignService: VaccinationCampaignService,
    private vaccineService: VaccineService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));
    this.loadCampaigns();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all campaigns (and the vaccines for their names) from API
   */
  loadCampaigns(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({ campaigns: this.campaignService.getAll(), vaccines: this.vaccineService.getAll() })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ campaigns, vaccines }) => {
          this.vaccines.set(vaccines.data || []);
          if (campaigns.status === 200 && campaigns.data) {
            this.campaigns.set([...campaigns.data].sort((a, b) => b.startDate.localeCompare(a.startDate)));
          } else {
            this.errorMessage.set(campaigns.message || this.translate.instant('clinic.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading vaccination campaigns:', error);
          this.errorMessage.set(this.translate.instant('clinic.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  navigateToCreate(): void {
    this.router.navigate(['/clinic/vaccination-campaigns/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/clinic/vaccination-campaigns/save', id]);
  }

  toggleActive(campaign: VaccinationCampaign): void {
    const request$ = campaign.isActive
      ? this.campaignService.deactivate(campaign.id)
      : this.campaignService.activate(campaign.id);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.campaigns.update(list => list.map(c => c.id === campaign.id ? { ...c, isActive: !c.isActive } : c));
        } else {
          this.errorMessage.set(response.message || this.translate.instant('clinic.saveFailed'));
        }
      },
      error: () => this.errorMessage.set(this.translate.instant('clinic.saveFailed'))
    });
  }

  openDeleteConfirmation(campaign: VaccinationCampaign, content: any): void {
    this.campaignToDelete = campaign;
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  confirmDelete(): void {
    if (!this.campaignToDelete) {
      return;
    }

    this.isDeleting.set(true);
    const id = this.campaignToDelete.id;

    this.campaignService
      .delete(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(this.translate.instant('clinic.deleted'));
            this.deleteModalRef?.close();
            this.campaignToDelete = null;
            this.loadCampaigns();

            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.successMessage.set(null);
            this.errorMessage.set(response.message || this.translate.instant('clinic.deleteFailed'));
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          // A campaign used by recorded doses cannot be deleted (deactivate it instead)
          console.error('Error deleting vaccination campaign:', error);
          this.successMessage.set(null);
          this.errorMessage.set(this.translate.instant('clinic.campaignInUse'));
          this.deleteModalRef?.close();
          this.campaignToDelete = null;
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.campaignToDelete = null;
  }

  vaccineName(id: string): string {
    const vaccine = this.vaccines().find(v => v.id === id);
    if (!vaccine) return '-';
    return this.currentLanguage() === 'ar' ? vaccine.nameAr : vaccine.nameEn;
  }

  formatDate(value: string): string {
    return formatDate(value);
  }
}
