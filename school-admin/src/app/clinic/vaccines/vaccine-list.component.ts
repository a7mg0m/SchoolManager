import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { VaccineService } from '../../core/services/vaccine.service';
import { Vaccine } from '../../core/models/vaccine.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-vaccine-list',
  standalone: false,
  styleUrl: './vaccine-list.component.scss',
  templateUrl: './vaccine-list.component.html',
})
export class VaccineListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  vaccines = signal<Vaccine[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);
  currentLanguage = signal<string>('en');

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private vaccineToDelete: Vaccine | null = null;

  constructor(
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
    this.loadVaccines();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all vaccines from API
   */
  loadVaccines(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.vaccineService
      .getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.vaccines.set(response.data);
          } else {
            this.errorMessage.set(response.message || this.translate.instant('clinic.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading vaccines:', error);
          this.errorMessage.set(this.translate.instant('clinic.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  navigateToCreate(): void {
    this.router.navigate(['/clinic/vaccines/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/clinic/vaccines/save', id]);
  }

  /**
   * Activate / deactivate a vaccine (inactive ones are not offered for new doses)
   */
  toggleActive(vaccine: Vaccine): void {
    const request$ = vaccine.isActive
      ? this.vaccineService.deactivate(vaccine.id)
      : this.vaccineService.activate(vaccine.id);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.vaccines.update(list => list.map(v => v.id === vaccine.id ? { ...v, isActive: !v.isActive } : v));
        } else {
          this.errorMessage.set(response.message || this.translate.instant('clinic.saveFailed'));
        }
      },
      error: () => this.errorMessage.set(this.translate.instant('clinic.saveFailed'))
    });
  }

  openDeleteConfirmation(vaccine: Vaccine, content: any): void {
    this.vaccineToDelete = vaccine;
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  confirmDelete(): void {
    if (!this.vaccineToDelete) {
      return;
    }

    this.isDeleting.set(true);
    const id = this.vaccineToDelete.id;

    this.vaccineService
      .delete(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(this.translate.instant('clinic.deleted'));
            this.deleteModalRef?.close();
            this.vaccineToDelete = null;
            this.loadVaccines();

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
          // A vaccine already used by doses or campaigns cannot be deleted (deactivate it instead)
          console.error('Error deleting vaccine:', error);
          this.successMessage.set(null);
          this.errorMessage.set(this.translate.instant('clinic.vaccineInUse'));
          this.deleteModalRef?.close();
          this.vaccineToDelete = null;
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.vaccineToDelete = null;
  }

  name(vaccine: Vaccine): string {
    return this.currentLanguage() === 'ar' ? vaccine.nameAr : vaccine.nameEn;
  }
}
