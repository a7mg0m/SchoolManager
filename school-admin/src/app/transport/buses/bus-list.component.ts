import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { BusService } from '../../core/services/bus.service';
import { BusRouteService } from '../../core/services/bus-route.service';
import { BusAssignmentService } from '../../core/services/bus-assignment.service';
import { AcademicContextService } from '../../core/services/academic-context.service';
import { Bus, BusAssignment, BusRoute } from '../../core/models/transport.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, of, timer } from 'rxjs';
import { catchError, map, switchMap, takeUntil } from 'rxjs/operators';
import { transportApiError } from '../transport-errors';

@Component({
  selector: 'app-bus-list',
  standalone: false,
  styleUrl: './bus-list.component.scss',
  templateUrl: './bus-list.component.html',
})
export class BusListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  buses = signal<Bus[]>([]);
  routes = signal<BusRoute[]>([]);
  /** Active assignments of every bus (all years) */
  activeAssignments = signal<BusAssignment[]>([]);

  /** busId -> seats taken in the header's academic year (the API counts seats per year) */
  assigned = computed(() => {
    const yearId = this.academic.yearId();
    const counts = new Map<string, number>();
    for (const a of this.activeAssignments()) {
      if (yearId && a.academicYearId !== yearId) continue;
      counts.set(a.busId, (counts.get(a.busId) || 0) + 1);
    }
    return counts;
  });
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);
  currentLanguage = signal<string>('en');
  searchTerm = signal('');
  routeFilter = signal('');

  /** Filtered by number / plate / driver and route, by number */
  filtered = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const routeId = this.routeFilter();
    return this.buses()
      .filter(b => !routeId || b.busRouteId === routeId)
      .filter(b => !term || `${b.number} ${b.plateNumber} ${b.driverName} ${b.driverPhone}`.toLowerCase().includes(term))
      .sort((a, b) => a.number.localeCompare(b.number, undefined, { numeric: true }));
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private busToDelete: Bus | null = null;

  constructor(
    private busService: BusService,
    private routeService: BusRouteService,
    private assignmentService: BusAssignmentService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private router: Router,
    private modalService: NgbModal,
    public academic: AcademicContextService
  ) {
    this.academic.load();
  }

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));
    this.loadBuses();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Buses and routes, then each bus's active assignments (seats taken) */
  loadBuses(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      buses: this.busService.getAll(),
      routes: this.routeService.getAll().pipe(catchError(() => of(null)))
    })
      .pipe(
        switchMap(({ buses, routes }) => {
          this.routes.set(routes?.data || []);
          if (buses.status !== 200 || !buses.data) {
            this.errorMessage.set(transportApiError(this.translate, buses.message, 'transport.loadFailed'));
            return of([] as Bus[]);
          }
          this.buses.set(buses.data);
          return of(buses.data);
        }),
        switchMap(buses => buses.length === 0
          ? of([] as BusAssignment[][])
          : forkJoin(buses.map(b => this.assignmentService.getByBus(b.id).pipe(
              map(r => (r.data || []).filter(a => a.isActive)),
              catchError(() => of([] as BusAssignment[]))
            )))),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (lists) => {
          this.activeAssignments.set(lists.flat());
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading buses:', error);
          this.errorMessage.set(this.translate.instant('transport.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  setSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  setRoute(event: Event): void {
    this.routeFilter.set((event.target as HTMLSelectElement).value);
  }

  navigateToCreate(): void {
    this.router.navigate(['/transport/buses/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/transport/buses/save', id]);
  }

  navigateToAssignments(busId: string): void {
    this.router.navigate(['/transport/assignments'], { queryParams: { busId } });
  }

  /**
   * Activate / deactivate a bus (inactive buses get no new students or trips)
   */
  toggleActive(bus: Bus): void {
    const request$ = bus.isActive
      ? this.busService.deactivate(bus.id)
      : this.busService.activate(bus.id);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.buses.update(list => list.map(b => b.id === bus.id ? { ...b, isActive: !b.isActive } : b));
        } else {
          this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.saveFailed'));
        }
      },
      error: () => this.errorMessage.set(this.translate.instant('transport.saveFailed'))
    });
  }

  openDeleteConfirmation(bus: Bus, content: any): void {
    this.busToDelete = bus;
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  confirmDelete(): void {
    if (!this.busToDelete) {
      return;
    }

    this.isDeleting.set(true);
    const id = this.busToDelete.id;

    this.busService
      .delete(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(this.translate.instant('transport.deleted'));
            this.buses.update(list => list.filter(b => b.id !== id));

            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            // e.g. the bus still has students assigned
            this.successMessage.set(null);
            this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.deleteFailed'));
          }
          this.deleteModalRef?.close();
          this.busToDelete = null;
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting bus:', error);
          this.successMessage.set(null);
          this.errorMessage.set(transportApiError(this.translate, error?.error?.message, 'transport.deleteFailed'));
          this.deleteModalRef?.close();
          this.busToDelete = null;
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.busToDelete = null;
  }

  localName(item: { nameAr: string; nameEn: string }): string {
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  routeName(id: string): string {
    const route = this.routes().find(r => r.id === id);
    return route ? this.localName(route) : '-';
  }

  seatsTaken(bus: Bus): number {
    return this.assigned().get(bus.id) ?? 0;
  }

  /** Bar colour: full (red), almost full (orange), else green */
  seatsClass(bus: Bus): string {
    const ratio = bus.capacity > 0 ? this.seatsTaken(bus) / bus.capacity : 0;
    return ratio >= 1 ? 'bg-danger' : ratio >= 0.8 ? 'bg-warning' : 'bg-success';
  }

  seatsPercent(bus: Bus): number {
    return bus.capacity > 0 ? Math.min(100, Math.round((this.seatsTaken(bus) / bus.capacity) * 100)) : 0;
  }
}
