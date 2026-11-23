import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { BusRouteService } from '../../core/services/bus-route.service';
import { BusService } from '../../core/services/bus.service';
import { Bus, BusRoute } from '../../core/models/transport.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, of, timer } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { transportApiError } from '../transport-errors';

@Component({
  selector: 'app-bus-route-list',
  standalone: false,
  styleUrl: './bus-route-list.component.scss',
  templateUrl: './bus-route-list.component.html',
})
export class BusRouteListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  routes = signal<BusRoute[]>([]);
  buses = signal<Bus[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);
  currentLanguage = signal<string>('en');
  searchTerm = signal('');

  /** Filtered by name / description / stop names, by name */
  filtered = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    return this.routes()
      .filter(r => !term || `${r.nameAr} ${r.nameEn} ${r.description || ''} ${r.stops.map(s => `${s.nameAr} ${s.nameEn}`).join(' ')}`
        .toLowerCase().includes(term))
      .sort((a, b) => this.localName(a).localeCompare(this.localName(b)));
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private routeToDelete: BusRoute | null = null;

  constructor(
    private routeService: BusRouteService,
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
    this.loadRoutes();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Routes, with the buses for the "buses on the route" column */
  loadRoutes(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      routes: this.routeService.getAll(),
      buses: this.busService.getAll().pipe(catchError(() => of(null)))
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ routes, buses }) => {
          this.buses.set(buses?.data || []);
          if (routes.status === 200 && routes.data) {
            this.routes.set(routes.data);
          } else {
            this.errorMessage.set(transportApiError(this.translate, routes.message, 'transport.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading bus routes:', error);
          this.errorMessage.set(this.translate.instant('transport.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  setSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  navigateToCreate(): void {
    this.router.navigate(['/transport/routes/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/transport/routes/save', id]);
  }

  /**
   * Activate / deactivate a route (inactive routes are not offered for new buses)
   */
  toggleActive(route: BusRoute): void {
    const request$ = route.isActive
      ? this.routeService.deactivate(route.id)
      : this.routeService.activate(route.id);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.routes.update(list => list.map(r => r.id === route.id ? { ...r, isActive: !r.isActive } : r));
        } else {
          this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.saveFailed'));
        }
      },
      error: () => this.errorMessage.set(this.translate.instant('transport.saveFailed'))
    });
  }

  openDeleteConfirmation(route: BusRoute, content: any): void {
    this.routeToDelete = route;
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  confirmDelete(): void {
    if (!this.routeToDelete) {
      return;
    }

    this.isDeleting.set(true);
    const id = this.routeToDelete.id;

    this.routeService
      .delete(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(this.translate.instant('transport.deleted'));
            this.routes.update(list => list.filter(r => r.id !== id));

            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            // e.g. the route is used by buses
            this.successMessage.set(null);
            this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.deleteFailed'));
          }
          this.deleteModalRef?.close();
          this.routeToDelete = null;
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting bus route:', error);
          this.successMessage.set(null);
          this.errorMessage.set(transportApiError(this.translate, error?.error?.message, 'transport.deleteFailed'));
          this.deleteModalRef?.close();
          this.routeToDelete = null;
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.routeToDelete = null;
  }

  localName(item: { nameAr: string; nameEn: string }): string {
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  /** Stop names in order ("A → B → C", the arrow follows the reading direction) */
  stopsText(route: BusRoute): string {
    const arrow = this.currentLanguage() === 'ar' ? ' ← ' : ' → ';
    return [...route.stops].sort((a, b) => a.order - b.order).map(s => this.localName(s)).join(arrow);
  }

  busCount(routeId: string): number {
    return this.buses().filter(b => b.busRouteId === routeId).length;
  }
}
