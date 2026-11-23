import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { BusService } from '../../core/services/bus.service';
import { BusRouteService } from '../../core/services/bus-route.service';
import { Bus, BusRoute, CreateBusRequest } from '../../core/models/transport.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { transportApiError } from '../transport-errors';

@Component({
  selector: 'app-bus-save',
  standalone: false,
  styleUrl: './bus-save.component.scss',
  templateUrl: './bus-save.component.html',
})
export class BusSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  routes = signal<BusRoute[]>([]);
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  currentLanguage = signal<string>('en');
  busId: string | null = null;

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private busService: BusService,
    private routeService: BusRouteService,
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

    this.busId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.busId);
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.form = this.formBuilder.group({
      number: ['', [Validators.required, Validators.maxLength(50)]],
      plateNumber: ['', [Validators.required, Validators.maxLength(20)]],
      capacity: [null, [Validators.required, Validators.min(1)]],
      driverName: ['', [Validators.required, Validators.maxLength(200)]],
      driverPhone: ['', [Validators.required, Validators.maxLength(20)]],
      busRouteId: ['', Validators.required]
    });
  }

  /** Active routes and (when editing) the bus */
  private loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      routes: this.routeService.getAll(),
      bus: this.busId ? this.busService.getById(this.busId) : of(null)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ routes, bus }) => {
          const editedRouteId = bus?.data?.busRouteId;
          // Active routes (the edited bus's route is kept even when inactive)
          this.routes.set((routes.data || []).filter(r => r.isActive || r.id === editedRouteId));
          if (bus) {
            if (bus.status === 200 && bus.data) {
              this.populateForm(bus.data);
            } else {
              this.errorMessage.set(transportApiError(this.translate, bus.message, 'transport.loadFailed'));
            }
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading bus form:', error);
          this.errorMessage.set(this.translate.instant('transport.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(bus: Bus): void {
    this.form.patchValue({
      number: bus.number,
      plateNumber: bus.plateNumber,
      capacity: bus.capacity,
      driverName: bus.driverName,
      driverPhone: bus.driverPhone,
      busRouteId: bus.busRouteId
    });
  }

  hasError(field: string, error: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.hasError(error) && (control.dirty || control.touched));
  }

  localName(item: { nameAr: string; nameEn: string }): string {
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.translate.instant('transport.fixErrors'));
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const v = this.form.getRawValue();
    const request: CreateBusRequest = {
      number: String(v.number).trim(),
      plateNumber: String(v.plateNumber).trim(),
      capacity: Number(v.capacity),
      driverName: String(v.driverName).trim(),
      driverPhone: String(v.driverPhone).trim(),
      busRouteId: v.busRouteId
    };

    const request$ = this.isEditMode()
      ? this.busService.update({ ...request, id: this.busId! })
      : this.busService.create(request);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.successMessage.set(this.translate.instant('transport.saved'));
          this.isSaving.set(false);
          // Navigate back after 1 second
          setTimeout(() => this.router.navigate(['/transport/buses']), 1000);
        } else {
          this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.saveFailed'));
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        console.error('Error saving bus:', error);
        this.errorMessage.set(transportApiError(this.translate, error?.error?.message, 'transport.saveFailed'));
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/transport/buses']);
  }
}
