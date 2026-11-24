import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { BusRouteService } from '../../core/services/bus-route.service';
import { BusRoute, BusRouteStop, CreateBusRouteRequest } from '../../core/models/transport.model';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { transportApiError } from '../transport-errors';

/** Id sent for a stop that is not saved yet */
const NEW_ID = '00000000-0000-0000-0000-000000000000';

@Component({
  selector: 'app-bus-route-save',
  standalone: false,
  styleUrl: './bus-route-save.component.scss',
  templateUrl: './bus-route-save.component.html',
})
export class BusRouteSaveComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isEditMode = signal(false);
  routeId: string | null = null;

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private routeService: BusRouteService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.routeId = this.activatedRoute.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.routeId);
    if (this.routeId) {
      this.loadRoute(this.routeId);
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
      description: ['', Validators.maxLength(500)],
      stops: this.formBuilder.array([])
    });
  }

  get stops(): FormArray {
    return this.form.get('stops') as FormArray;
  }

  /** A stop row; the order is its position in the table */
  addStop(stop?: BusRouteStop): void {
    this.stops.push(this.formBuilder.group({
      id: [stop?.id ?? NEW_ID],
      nameAr: [stop?.nameAr ?? '', [Validators.required, Validators.maxLength(200)]],
      nameEn: [stop?.nameEn ?? '', [Validators.required, Validators.maxLength(200)]]
    }));
  }

  removeStop(index: number): void {
    this.stops.removeAt(index);
  }

  /** Moves a stop up (-1) or down (+1) */
  moveStop(index: number, step: -1 | 1): void {
    const target = index + step;
    if (target < 0 || target >= this.stops.length) return;
    const control = this.stops.at(index);
    this.stops.removeAt(index);
    this.stops.insert(target, control);
  }

  private loadRoute(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.routeService
      .getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.populateForm(response.data);
          } else {
            this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading bus route:', error);
          this.errorMessage.set(this.translate.instant('transport.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  private populateForm(route: BusRoute): void {
    this.form.patchValue({
      nameAr: route.nameAr,
      nameEn: route.nameEn,
      description: route.description ?? ''
    });
    this.stops.clear();
    [...(route.stops || [])].sort((a, b) => a.order - b.order).forEach(s => this.addStop(s));
  }

  hasError(field: string, error: string): boolean {
    const control = this.form.get(field);
    return !!(control && control.hasError(error) && (control.dirty || control.touched));
  }

  stopInvalid(index: number, field: string): boolean {
    const control = this.stops.at(index).get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      this.errorMessage.set(this.translate.instant('transport.fixErrors'));
      return;
    }

    const v = this.form.getRawValue();
    const stops: BusRouteStop[] = (v.stops || []).map((s: any, index: number) => ({
      id: s.id || NEW_ID,
      nameAr: String(s.nameAr).trim(),
      nameEn: String(s.nameEn).trim(),
      order: index + 1
    }));

    this.isSaving.set(true);
    this.errorMessage.set(null);

    const request: CreateBusRouteRequest = {
      nameAr: String(v.nameAr).trim(),
      nameEn: String(v.nameEn).trim(),
      description: String(v.description ?? '').trim() || null,
      stops
    };

    const request$ = this.isEditMode()
      ? this.routeService.update({ ...request, id: this.routeId! })
      : this.routeService.create(request);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200 || response.status === 201) {
          this.successMessage.set(this.translate.instant('transport.saved'));
          this.isSaving.set(false);
          // Navigate back after 1 second
          setTimeout(() => this.router.navigate(['/transport/routes']), 1000);
        } else {
          this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.saveFailed'));
          this.isSaving.set(false);
        }
      },
      error: (error) => {
        // Changing the stops of a route whose stops are used by bus assignments is refused by the API
        console.error('Error saving bus route:', error);
        this.errorMessage.set(transportApiError(this.translate, error?.error?.message, 'transport.saveFailed'));
        this.isSaving.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/transport/routes']);
  }
}
