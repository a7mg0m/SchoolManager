import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { BusService } from '../../core/services/bus.service';
import { BusRouteService } from '../../core/services/bus-route.service';
import { Bus, BusRoute, CreateBusRequest } from '../../core/models/transport.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { transportApiError } from '../transport-errors';

/** confirmPassword must equal driverPassword (only checked when a password is typed) */
const passwordsMatch: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const password = group.get('driverPassword')?.value || '';
  const confirm = group.get('confirmPassword')?.value || '';
  return password && password !== confirm ? { passwordMismatch: true } : null;
};

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

  /** The driver already has a login (edit mode): an empty password keeps it */
  hasDriverLogin = signal(false);
  showPassword = signal(false);
  readonly loginNameMinLength = 3;
  readonly loginNameMaxLength = 100;
  readonly passwordMinLength = 6;

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
      busRouteId: ['', Validators.required],
      // Driver's login (Identity service): the password is required until the driver has a login
      driverLoginName: ['', [
        Validators.required,
        Validators.minLength(this.loginNameMinLength),
        Validators.maxLength(this.loginNameMaxLength)
      ]],
      driverPassword: ['', [Validators.required, Validators.minLength(this.passwordMinLength)]],
      confirmPassword: ['']
    }, { validators: passwordsMatch });
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
      busRouteId: bus.busRouteId,
      driverLoginName: bus.driverLoginName || ''
    });

    // Driver with a login: the password is optional (empty keeps it). Without one: it must be set now.
    this.hasDriverLogin.set(!!bus.driverLoginName);
    const password = this.form.get('driverPassword')!;
    password.setValidators(this.hasDriverLogin()
      ? [Validators.minLength(this.passwordMinLength)]
      : [Validators.required, Validators.minLength(this.passwordMinLength)]);
    password.updateValueAndValidity();
  }

  get passwordMismatch(): boolean {
    const confirm = this.form.get('confirmPassword');
    return this.form.hasError('passwordMismatch') && !!confirm && (confirm.dirty || confirm.touched);
  }

  /** Show "login name already used" on the field and in the page message */
  private loginNameTaken(): void {
    const control = this.form.get('driverLoginName')!;
    control.setErrors({ ...(control.errors ?? {}), taken: true });
    control.markAsTouched();
    this.errorMessage.set(this.translate.instant('transport.errors.LoginNameExists'));
    this.isSaving.set(false);
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

    // The driver's login is created later (through Kafka), so check now that no employee,
    // teacher, parent or other driver already uses the login name
    const loginName = String(this.form.get('driverLoginName')!.value).trim();
    this.busService.isLoginNameAvailable(loginName, this.busId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data === false) {
            this.loginNameTaken();
            return;
          }
          this.save();
        },
        error: (error) => {
          console.error('Error checking the login name:', error);
          this.errorMessage.set(this.translate.instant('transport.loginNameCheckFailed'));
          this.isSaving.set(false);
        }
      });
  }

  private save(): void {
    const v = this.form.getRawValue();
    const request: CreateBusRequest = {
      number: String(v.number).trim(),
      plateNumber: String(v.plateNumber).trim(),
      capacity: Number(v.capacity),
      driverName: String(v.driverName).trim(),
      driverPhone: String(v.driverPhone).trim(),
      busRouteId: v.busRouteId,
      driverLoginName: String(v.driverLoginName).trim(),
      // Empty: keep the current password
      driverPassword: v.driverPassword || null
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
