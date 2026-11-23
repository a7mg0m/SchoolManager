import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { TripService } from '../../core/services/trip.service';
import { BusService } from '../../core/services/bus.service';
import { BusRouteService } from '../../core/services/bus-route.service';
import { BusAssignmentService } from '../../core/services/bus-assignment.service';
import { StudentService } from '../../core/services/student.service';
import {
  Bus,
  BusAssignment,
  BusRoute,
  TRIP_STATUSES,
  TRIP_STUDENT_STATUSES,
  TRIP_TYPES,
  Trip,
  TripStatus,
  TripStudent,
  TripStudentStatus
} from '../../core/models/transport.model';
import { Student } from '../../core/models/student.model';
import { ApiResponse } from '../../core/models/stage.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Observable, Subject, Subscription, forkJoin, of, timer } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { transportApiError } from '../transport-errors';
import { formatDate } from '../../notification/shared/audience';

/** Trip action waiting for confirmation */
type TripAction = 'start' | 'complete' | 'cancel';

/** How often a running trip is reloaded (bus position and students) */
const REFRESH_MS = 15000;

@Component({
  selector: 'app-trip-details',
  standalone: false,
  styleUrl: './trip-details.component.scss',
  templateUrl: './trip-details.component.html',
})
export class TripDetailsComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  trip = signal<Trip | null>(null);
  bus = signal<Bus | null>(null);
  route = signal<BusRoute | null>(null);
  assignments = signal<BusAssignment[]>([]);
  students = signal<Student[]>([]);
  isLoading = signal(false);
  isSaving = signal(false);
  /** Student being marked (its row buttons show a spinner) */
  markingStudentId = signal<string | null>(null);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  lastRefresh = signal<Date | null>(null);
  pendingAction = signal<TripAction>('start');
  currentLanguage = signal<string>('en');

  readonly statuses = TRIP_STATUSES;
  readonly studentStatuses = TRIP_STUDENT_STATUSES;
  readonly TripStatus = TripStatus;
  readonly TripStudentStatus = TripStudentStatus;

  isRunning = computed(() => this.trip()?.status === TripStatus.InProgress);

  /** Students by stop order, then name */
  sortedStudents = computed(() => {
    const trip = this.trip();
    if (!trip) return [];
    return [...trip.students].sort((a, b) =>
      this.stopOrder(a.studentId) - this.stopOrder(b.studentId) || this.studentName(a.studentId).localeCompare(this.studentName(b.studentId)));
  });

  /** Students per status */
  counts = computed(() => {
    const list = this.trip()?.students || [];
    return this.studentStatuses.map(s => ({ ...s, count: list.filter(x => x.status === s.value).length }));
  });

  hasLocation = computed(() => {
    const t = this.trip();
    return !!t && t.currentLatitude !== null && t.currentLongitude !== null;
  });

  /** Google Maps link of the last position (no map library) */
  mapsUrl = computed(() => {
    const t = this.trip();
    return t && this.hasLocation() ? `https://www.google.com/maps?q=${t.currentLatitude},${t.currentLongitude}` : '';
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private refresh?: Subscription;
  private modalRef: NgbModalRef | null = null;
  private tripId = '';

  constructor(
    private tripService: TripService,
    private busService: BusService,
    private routeService: BusRouteService,
    private assignmentService: BusAssignmentService,
    private studentService: StudentService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.tripId = this.activatedRoute.snapshot.paramMap.get('id') || '';
    this.loadTrip();
  }

  ngOnDestroy(): void {
    this.stopRefresh();
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** The trip, then its bus, route (stop names), the bus's assignments (stops of the students) and students */
  private loadTrip(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.tripService
      .getById(this.tripId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.applyTrip(response.data);
            this.loadLookups(response.data.busId);
          } else {
            this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.tripNotFound'));
            this.isLoading.set(false);
          }
        },
        error: (error) => {
          console.error('Error loading trip:', error);
          this.errorMessage.set(this.translate.instant(error?.status === 404 ? 'transport.tripNotFound' : 'transport.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  private loadLookups(busId: string): void {
    forkJoin({
      bus: this.busService.getById(busId).pipe(catchError(() => of(null))),
      routes: this.routeService.getAll().pipe(catchError(() => of(null))),
      assignments: this.assignmentService.getByBus(busId).pipe(catchError(() => of(null))),
      students: this.studentService.getAll().pipe(catchError(() => of(null)))
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ bus, routes, assignments, students }) => {
        this.bus.set(bus?.data ?? null);
        this.route.set((routes?.data || []).find(r => r.id === bus?.data?.busRouteId) ?? null);
        this.assignments.set(assignments?.data || []);
        this.students.set(students?.data || []);
        this.isLoading.set(false);
      });
  }

  /** Silent reload (no spinner) of the trip only */
  reload(): void {
    this.tripService
      .getById(this.tripId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.applyTrip(response.data);
          }
        },
        error: (error) => console.error('Error refreshing trip:', error)
      });
  }

  /** Shows the trip and refreshes it every 15 s while it runs */
  private applyTrip(trip: Trip): void {
    this.trip.set(trip);
    this.lastRefresh.set(new Date());
    if (trip.status === TripStatus.InProgress) {
      if (!this.refresh) {
        this.refresh = timer(REFRESH_MS, REFRESH_MS)
          .pipe(takeUntil(this.destroy$))
          .subscribe(() => this.reload());
      }
    } else {
      this.stopRefresh();
    }
  }

  private stopRefresh(): void {
    this.refresh?.unsubscribe();
    this.refresh = undefined;
  }

  // ---------------------------------------------------------------------------
  // Start / complete / cancel
  // ---------------------------------------------------------------------------

  canStart(): boolean {
    return this.trip()?.status === TripStatus.Planned;
  }

  canComplete(): boolean {
    return this.trip()?.status === TripStatus.InProgress;
  }

  canCancel(): boolean {
    const status = this.trip()?.status;
    return status === TripStatus.Planned || status === TripStatus.InProgress;
  }

  openAction(action: TripAction, content: any): void {
    this.pendingAction.set(action);
    this.modalRef = this.modalService.open(content, { centered: true, backdrop: 'static', keyboard: false });
  }

  confirmAction(): void {
    const action = this.pendingAction();
    const request$: Observable<ApiResponse<null>> =
      action === 'start' ? this.tripService.start(this.tripId)
        : action === 'complete' ? this.tripService.complete(this.tripId)
          : this.tripService.cancel(this.tripId);
    const doneKey = action === 'start' ? 'transport.tripStarted'
      : action === 'complete' ? 'transport.tripCompleted'
        : 'transport.tripCancelled';

    this.isSaving.set(true);
    this.errorMessage.set(null);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        this.modalRef?.close();
        if (response.status === 200) {
          this.showSuccess(this.translate.instant(doneKey));
          this.reload();
        } else {
          this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.saveFailed'));
        }
        this.isSaving.set(false);
      },
      error: (error) => {
        console.error('Error updating trip:', error);
        this.modalRef?.close();
        this.errorMessage.set(transportApiError(this.translate, error?.error?.message, 'transport.saveFailed'));
        this.isSaving.set(false);
      }
    });
  }

  cancelModal(): void {
    this.modalRef?.close();
    this.modalRef = null;
  }

  // ---------------------------------------------------------------------------
  // Students
  // ---------------------------------------------------------------------------

  canBoard(s: TripStudent): boolean {
    return this.isRunning() && s.status === TripStudentStatus.Pending;
  }

  canDropOff(s: TripStudent): boolean {
    return this.isRunning() && s.status === TripStudentStatus.Boarded;
  }

  canMarkAbsent(s: TripStudent): boolean {
    return this.isRunning() && s.status === TripStudentStatus.Pending;
  }

  markStudent(s: TripStudent, status: TripStudentStatus): void {
    this.markingStudentId.set(s.studentId);
    this.errorMessage.set(null);

    this.tripService
      .markStudent(this.tripId, { studentId: s.studentId, status })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.reload();
          } else {
            this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.saveFailed'));
          }
          this.markingStudentId.set(null);
        },
        error: (error) => {
          console.error('Error marking trip student:', error);
          this.errorMessage.set(transportApiError(this.translate, error?.error?.message, 'transport.saveFailed'));
          this.markingStudentId.set(null);
        }
      });
  }

  back(): void {
    this.router.navigate(['/transport/trips']);
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  statusInfo(status: TripStatus) {
    return this.statuses.find(s => s.value === status) ?? this.statuses[0];
  }

  studentStatusInfo(status: TripStudentStatus) {
    return this.studentStatuses.find(s => s.value === status) ?? this.studentStatuses[0];
  }

  typeKey(): string {
    const trip = this.trip();
    return TRIP_TYPES.find(t => t.value === trip?.type)?.key ?? 'transport.tripType.toSchool';
  }

  studentName(id: string): string {
    const s = this.students().find(x => x.id === id);
    if (!s) return '-';
    return this.currentLanguage() === 'ar' ? (s.fullNameAr || s.fullNameEn) : (s.fullNameEn || s.fullNameAr);
  }

  /** The student's stop on this bus (active assignment) */
  stopName(studentId: string): string {
    const stop = this.stopOf(studentId);
    if (!stop) return '-';
    return `${stop.order}. ${this.currentLanguage() === 'ar' ? (stop.nameAr || stop.nameEn) : (stop.nameEn || stop.nameAr)}`;
  }

  dateText(value: string): string {
    return formatDate(value) || value;
  }

  dateTime(value: string | null): string {
    if (!value) return '-';
    const d = new Date(value);
    return isNaN(d.getTime())
      ? value
      : d.toLocaleString(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US', { dateStyle: 'medium', timeStyle: 'short' });
  }

  time(value: string | null): string {
    if (!value) return '-';
    const d = new Date(value);
    return isNaN(d.getTime())
      ? value
      : d.toLocaleTimeString(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US', { hour: 'numeric', minute: '2-digit' });
  }

  private stopOf(studentId: string) {
    const assignment = this.assignments().find(a => a.studentId === studentId && a.isActive)
      ?? this.assignments().find(a => a.studentId === studentId);
    return assignment?.busRouteStopId ? this.route()?.stops.find(s => s.id === assignment.busRouteStopId) : undefined;
  }

  private stopOrder(studentId: string): number {
    return this.stopOf(studentId)?.order ?? Number.MAX_SAFE_INTEGER;
  }

  private showSuccess(message: string): void {
    this.successMessage.set(message);
    timer(4000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.successMessage.set(null));
  }
}
