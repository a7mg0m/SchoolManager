import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { TripService } from '../../core/services/trip.service';
import { BusService } from '../../core/services/bus.service';
import {
  Bus,
  TRIP_STATUSES,
  TRIP_TYPES,
  Trip,
  TripStatus,
  TripStudentStatus,
  TripType
} from '../../core/models/transport.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { transportApiError } from '../transport-errors';
import { todayIso } from '../../notification/shared/audience';

@Component({
  selector: 'app-trip-list',
  standalone: false,
  styleUrl: './trip-list.component.scss',
  templateUrl: './trip-list.component.html',
})
export class TripListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  buses = signal<Bus[]>([]);
  trips = signal<Trip[]>([]);
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  modalError = signal<string | null>(null);
  currentLanguage = signal<string>('en');

  // Filters
  date = signal(todayIso());
  busId = signal('');

  // New trip modal
  newBusId = signal('');
  newDate = signal(todayIso());
  newType = signal<TripType>(TripType.ToSchool);

  readonly tripTypes = TRIP_TYPES;
  readonly tripStatuses = TRIP_STATUSES;

  /** Running trips first, then by bus number and direction */
  sortedTrips = computed(() =>
    [...this.trips()].sort((a, b) =>
      Number(b.status === TripStatus.InProgress) - Number(a.status === TripStatus.InProgress) ||
      this.busNumber(a.busId).localeCompare(this.busNumber(b.busId), undefined, { numeric: true }) ||
      a.type - b.type));

  activeBuses = computed(() =>
    this.buses().filter(b => b.isActive).sort((a, b) => a.number.localeCompare(b.number, undefined, { numeric: true })));

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private modalRef: NgbModalRef | null = null;

  constructor(
    private tripService: TripService,
    private busService: BusService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.busService.getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => this.buses.set(response.data || []),
        error: () => this.buses.set([])
      });
    this.loadTrips();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Trips of the chosen date (of one bus when chosen) */
  loadTrips(): void {
    if (!this.date()) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.tripService
      .getByDate(this.date(), this.busId() || null)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.trips.set(response.data || []);
          } else {
            this.trips.set([]);
            this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading trips:', error);
          this.trips.set([]);
          this.errorMessage.set(this.translate.instant('transport.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  onDateChange(event: Event): void {
    this.date.set((event.target as HTMLInputElement).value || todayIso());
    this.loadTrips();
  }

  onBusChange(event: Event): void {
    this.busId.set((event.target as HTMLSelectElement).value);
    this.loadTrips();
  }

  navigateToDetails(id: string): void {
    this.router.navigate(['/transport/trips', id]);
  }

  // ---------------------------------------------------------------------------
  // New trip
  // ---------------------------------------------------------------------------

  openNewTrip(content: any): void {
    this.modalError.set(null);
    this.newBusId.set(this.busId() && this.activeBuses().some(b => b.id === this.busId()) ? this.busId() : '');
    this.newDate.set(this.date());
    this.newType.set(TripType.ToSchool);
    this.modalRef = this.modalService.open(content, { centered: true, backdrop: 'static', keyboard: false });
  }

  onNewBus(event: Event): void {
    this.newBusId.set((event.target as HTMLSelectElement).value);
  }

  onNewDate(event: Event): void {
    this.newDate.set((event.target as HTMLInputElement).value);
  }

  onNewType(type: TripType): void {
    this.newType.set(type);
  }

  /** Plans the trip with every student riding the bus on that date, then opens it */
  confirmNewTrip(): void {
    if (!this.newBusId() || !this.newDate()) return;

    this.isSaving.set(true);
    this.modalError.set(null);

    this.tripService
      .create({ busId: this.newBusId(), date: this.newDate(), type: this.newType() })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.modalRef?.close();
            this.isSaving.set(false);
            if (response.insertedId) {
              this.router.navigate(['/transport/trips', response.insertedId]);
              return;
            }
            this.showSuccess(this.translate.instant('transport.tripCreated'));
            this.loadTrips();
          } else {
            this.modalError.set(transportApiError(this.translate, response.message, 'transport.saveFailed'));
            this.isSaving.set(false);
          }
        },
        error: (error) => {
          console.error('Error creating trip:', error);
          this.modalError.set(transportApiError(this.translate, error?.error?.message, 'transport.saveFailed'));
          this.isSaving.set(false);
        }
      });
  }

  cancelModal(): void {
    this.modalRef?.close();
    this.modalRef = null;
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  busNumber(busId: string): string {
    return this.buses().find(b => b.id === busId)?.number ?? '-';
  }

  bus(busId: string): Bus | undefined {
    return this.buses().find(b => b.id === busId);
  }

  statusInfo(status: TripStatus) {
    return this.tripStatuses.find(s => s.value === status) ?? this.tripStatuses[0];
  }

  typeKey(type: TripType): string {
    return this.tripTypes.find(t => t.value === type)?.key ?? 'transport.tripType.toSchool';
  }

  /** "boarded or dropped off / all" students */
  ridersText(trip: Trip): string {
    const done = trip.students.filter(s => s.status === TripStudentStatus.Boarded || s.status === TripStudentStatus.DroppedOff).length;
    return `${done} / ${trip.students.length}`;
  }

  absentCount(trip: Trip): number {
    return trip.students.filter(s => s.status === TripStudentStatus.Absent).length;
  }

  time(value: string | null): string {
    if (!value) return '-';
    const d = new Date(value);
    return isNaN(d.getTime())
      ? value
      : d.toLocaleTimeString(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US', { hour: 'numeric', minute: '2-digit' });
  }

  private showSuccess(message: string): void {
    this.errorMessage.set(null);
    this.successMessage.set(message);
    timer(4000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.successMessage.set(null));
  }
}
