import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, Subscription, forkJoin, of } from 'rxjs';
import { catchError, map, switchMap, takeUntil } from 'rxjs/operators';
import { BusService } from '../../core/services/bus.service';
import { BusRouteService } from '../../core/services/bus-route.service';
import { BusAssignmentService } from '../../core/services/bus-assignment.service';
import { TripService } from '../../core/services/trip.service';
import { StudentFeeService } from '../../core/services/student-fee.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Bus, BusAssignment, BusRoute, Trip, TripStatus, TripStudentStatus, TripType } from '../../core/models/transport.model';
import { StudentFee } from '../../core/models/student-fee.model';
import { AcademicYear } from '../../core/models/academic-year.model';

/** Money of one currency */
interface Amounts {
  currency: string;
  billed: number;
  collected: number;
  outstanding: number;
}

interface BusRow {
  bus: Bus;
  route: string;
  students: number;
  occupancy: number;
  fees: Amounts[];
  trips: number;
  completed: number;
  cancelled: number;
  boarded: number;
  absent: number;
  /** Average minutes from start to end of a completed trip (null: none) */
  avgMinutes: number | null;
}

interface RouteRow {
  route: BusRoute | null;
  name: string;
  stops: number;
  buses: number;
  capacity: number;
  students: number;
  occupancy: number;
}

type QuickRange = '' | 'thisWeek' | 'thisMonth' | 'lastMonth';

function iso(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/** Students marked on the trip (boarded / dropped off) and absent */
function tripMarks(t: Trip): { boarded: number; absent: number } {
  return {
    boarded: t.students.filter(s => s.status === TripStudentStatus.Boarded || s.status === TripStudentStatus.DroppedOff).length,
    absent: t.students.filter(s => s.status === TripStudentStatus.Absent).length
  };
}

function minutesOf(t: Trip): number | null {
  if (!t.startedAt || !t.endedAt) return null;
  const m = (new Date(t.endedAt).getTime() - new Date(t.startedAt).getTime()) / 60000;
  return m > 0 && m < 24 * 60 ? m : null;
}

/**
 * Report: school transport — /reports/transport
 * The fleet (buses / routes, capacity, assigned students, occupancy), the bus fees of the academic year (billed,
 * collected, outstanding per bus) and the trips of a period (completed / cancelled, students boarded / absent,
 * average duration) by bus and by day; Excel export (per bus) and print.
 */
@Component({
  selector: 'app-transport-report',
  standalone: false,
  styleUrl: './transport-report.component.scss',
  templateUrl: './transport-report.component.html',
})
export class TransportReportComponent implements OnInit, OnDestroy {
  isLoading = signal(true);
  isLoadingTrips = signal(false);
  errorMessage = signal<string | null>(null);
  currentLanguage = signal('en');

  buses = signal<Bus[]>([]);
  private routes = signal<BusRoute[]>([]);
  private assignments = signal<BusAssignment[]>([]);
  private fees = signal<Map<string, StudentFee>>(new Map());
  private trips = signal<Trip[]>([]);
  academicYears = signal<AcademicYear[]>([]);

  yearFilter = signal('');
  busFilter = signal('');
  routeFilter = signal('');
  dateFrom = signal('');
  dateTo = signal('');
  quickRange = signal<QuickRange>('thisMonth');
  showInactive = signal(false);

  readonly generatedOn = new Date();
  readonly TripType = TripType;
  private destroy$ = new Subject<void>();
  private tripsSub?: Subscription;
  private assignmentsSub?: Subscription;

  routeOptions = computed(() => this.routes());

  /** Buses in the report (route / bus / active filters) */
  private shownBuses = computed(() =>
    this.buses().filter(b =>
      (this.showInactive() || b.isActive) && (!this.routeFilter() || b.busRouteId === this.routeFilter()) && (!this.busFilter() || b.id === this.busFilter())));

  /** Active assignments of the shown buses (academic year chosen) */
  private activeAssignments = computed(() => {
    const ids = new Set(this.shownBuses().map(b => b.id));
    const year = this.yearFilter();
    return this.assignments().filter(a => a.isActive && ids.has(a.busId) && (!year || a.academicYearId === year));
  });

  private shownTrips = computed(() => {
    const ids = new Set(this.shownBuses().map(b => b.id));
    return this.trips().filter(t => ids.has(t.busId));
  });

  busRows = computed<BusRow[]>(() => {
    const lang = this.currentLanguage();
    const routes = new Map(this.routes().map(r => [r.id, r]));
    const fees = this.fees();
    const year = this.yearFilter();
    return this.shownBuses()
      .map(bus => {
        const assigned = this.activeAssignments().filter(a => a.busId === bus.id);
        // Fees of every assignment of the year (ended ones were billed too)
        const yearAssignments = this.assignments().filter(a => a.busId === bus.id && (!year || a.academicYearId === year));
        const money = new Map<string, Amounts>();
        for (const a of yearAssignments) {
          const fee = a.studentFeeId ? fees.get(a.studentFeeId) : undefined;
          const currency = (fee?.currency || a.currency || '-').toUpperCase();
          const m = money.get(currency) ?? { currency, billed: 0, collected: 0, outstanding: 0 };
          m.billed += fee ? fee.netAmount : a.feeAmount;
          m.collected += fee?.paidAmount ?? 0;
          m.outstanding += fee ? (fee.status === 3 ? 0 : fee.balance) : a.feeAmount;
          money.set(currency, m);
        }
        const trips = this.shownTrips().filter(t => t.busId === bus.id);
        const completed = trips.filter(t => t.status === TripStatus.Completed);
        const durations = completed.map(minutesOf).filter((m): m is number => m !== null);
        const marks = trips.reduce((acc, t) => {
          const m = tripMarks(t);
          return { boarded: acc.boarded + m.boarded, absent: acc.absent + m.absent };
        }, { boarded: 0, absent: 0 });
        return {
          bus,
          route: this.nameOf(routes.get(bus.busRouteId), lang) || '-',
          students: assigned.length,
          occupancy: bus.capacity > 0 ? Math.round((assigned.length * 100) / bus.capacity) : 0,
          fees: [...money.values()],
          trips: trips.length,
          completed: completed.length,
          cancelled: trips.filter(t => t.status === TripStatus.Cancelled).length,
          ...marks,
          avgMinutes: durations.length ? Math.round(durations.reduce((a, b) => a + b, 0) / durations.length) : null
        };
      })
      .sort((a, b) => a.bus.number.localeCompare(b.bus.number, undefined, { numeric: true }));
  });

  routeRows = computed<RouteRow[]>(() => {
    const lang = this.currentLanguage();
    const groups = new Map<string, BusRow[]>();
    for (const r of this.busRows()) groups.set(r.bus.busRouteId, [...(groups.get(r.bus.busRouteId) ?? []), r]);
    return [...groups.entries()].map(([routeId, rows]) => {
      const route = this.routes().find(r => r.id === routeId) ?? null;
      const capacity = rows.reduce((s, r) => s + r.bus.capacity, 0);
      const students = rows.reduce((s, r) => s + r.students, 0);
      return {
        route,
        name: this.nameOf(route ?? undefined, lang) || '-',
        stops: route?.stops?.length ?? 0,
        buses: rows.length,
        capacity,
        students,
        occupancy: capacity > 0 ? Math.round((students * 100) / capacity) : 0
      };
    });
  });

  fleet = computed(() => {
    const rows = this.busRows();
    const capacity = rows.reduce((s, r) => s + r.bus.capacity, 0);
    const students = rows.reduce((s, r) => s + r.students, 0);
    return {
      buses: rows.length,
      routes: new Set(rows.map(r => r.bus.busRouteId)).size,
      capacity,
      students,
      free: Math.max(0, capacity - students),
      occupancy: capacity > 0 ? Math.round((students * 100) / capacity) : 0,
      full: rows.filter(r => r.bus.capacity > 0 && r.students >= r.bus.capacity).length
    };
  });

  /** Bus fees of the year per currency */
  feeTotals = computed<Amounts[]>(() => {
    const totals = new Map<string, Amounts>();
    for (const r of this.busRows()) {
      for (const f of r.fees) {
        const t = totals.get(f.currency) ?? { currency: f.currency, billed: 0, collected: 0, outstanding: 0 };
        t.billed += f.billed;
        t.collected += f.collected;
        t.outstanding += f.outstanding;
        totals.set(f.currency, t);
      }
    }
    return [...totals.values()];
  });

  tripSummary = computed(() => {
    const trips = this.shownTrips();
    const marks = trips.reduce((acc, t) => {
      const m = tripMarks(t);
      return { boarded: acc.boarded + m.boarded, absent: acc.absent + m.absent };
    }, { boarded: 0, absent: 0 });
    const durations = trips.filter(t => t.status === TripStatus.Completed).map(minutesOf).filter((m): m is number => m !== null);
    return {
      total: trips.length,
      completed: trips.filter(t => t.status === TripStatus.Completed).length,
      inProgress: trips.filter(t => t.status === TripStatus.InProgress).length,
      planned: trips.filter(t => t.status === TripStatus.Planned).length,
      cancelled: trips.filter(t => t.status === TripStatus.Cancelled).length,
      toSchool: trips.filter(t => t.type === TripType.ToSchool).length,
      toHome: trips.filter(t => t.type === TripType.ToHome).length,
      ...marks,
      avgMinutes: durations.length ? Math.round(durations.reduce((a, b) => a + b, 0) / durations.length) : null
    };
  });

  /** Trips per day, newest first */
  byDay = computed(() => {
    const groups = new Map<string, Trip[]>();
    for (const t of this.shownTrips()) {
      const d = (t.date || '').slice(0, 10);
      groups.set(d, [...(groups.get(d) ?? []), t]);
    }
    return [...groups.entries()]
      .map(([date, trips]) => {
        const marks = trips.reduce((acc, t) => {
          const m = tripMarks(t);
          return { boarded: acc.boarded + m.boarded, absent: acc.absent + m.absent };
        }, { boarded: 0, absent: 0 });
        return {
          date,
          trips: trips.length,
          completed: trips.filter(t => t.status === TripStatus.Completed).length,
          cancelled: trips.filter(t => t.status === TripStatus.Cancelled).length,
          ...marks
        };
      })
      .sort((a, b) => b.date.localeCompare(a.date));
  });

  constructor(
    private busService: BusService,
    private routeService: BusRouteService,
    private assignmentService: BusAssignmentService,
    private tripService: TripService,
    private studentFeeService: StudentFeeService,
    private academicYearService: AcademicYearService,
    private languageService: LanguageService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$.pipe(takeUntil(this.destroy$)).subscribe(l => this.currentLanguage.set(l));
    this.setQuickRange('thisMonth', false);
    this.load();
  }

  ngOnDestroy(): void {
    this.tripsSub?.unsubscribe();
    this.assignmentsSub?.unsubscribe();
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Buses, routes, years, fees; then each bus's assignments; then the trips */
  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    let failed = false;
    const list = <T>(obs: Observable<{ status: number; data: T[] }>) =>
      obs.pipe(
        map(r => (r.status === 200 && r.data ? r.data : [])),
        catchError(() => {
          failed = true;
          return of([] as T[]);
        })
      );
    this.assignmentsSub?.unsubscribe();
    this.assignmentsSub = forkJoin({
      buses: list(this.busService.getAll()),
      routes: list(this.routeService.getAll()),
      years: list(this.academicYearService.getAll()),
      fees: list(this.studentFeeService.getAll())
    })
      .pipe(
        switchMap(base => {
          this.buses.set(base.buses);
          this.routes.set(base.routes);
          this.academicYears.set(base.years);
          this.fees.set(new Map(base.fees.map(f => [f.id, f])));
          if (base.buses.length === 0) return of([] as BusAssignment[][]);
          return forkJoin(base.buses.map(b => list(this.assignmentService.getByBus(b.id))));
        }),
        takeUntil(this.destroy$)
      )
      .subscribe(perBus => {
        this.assignments.set(perBus.flat());
        if (failed) this.errorMessage.set(this.translate.instant('reports.loadFailed'));
        this.isLoading.set(false);
        this.loadTrips();
      });
  }

  loadTrips(): void {
    if (!this.dateFrom() || !this.dateTo()) return;
    this.tripsSub?.unsubscribe();
    this.isLoadingTrips.set(true);
    this.tripsSub = this.tripService.getReport(this.dateFrom(), this.dateTo()).subscribe({
      next: r => {
        this.trips.set(r.status === 200 && r.data ? r.data : []);
        this.isLoadingTrips.set(false);
      },
      error: () => {
        this.trips.set([]);
        this.isLoadingTrips.set(false);
        this.errorMessage.set(this.translate.instant('reports.transport.tripsFailed'));
      }
    });
  }

  setQuickRange(range: QuickRange, reload = true): void {
    this.quickRange.set(range);
    const now = new Date();
    if (range === 'thisWeek') {
      const start = new Date(now.getFullYear(), now.getMonth(), now.getDate() - ((now.getDay() + 1) % 7));
      this.dateFrom.set(iso(start));
      this.dateTo.set(iso(new Date(start.getFullYear(), start.getMonth(), start.getDate() + 6)));
    } else if (range === 'thisMonth') {
      this.dateFrom.set(iso(new Date(now.getFullYear(), now.getMonth(), 1)));
      this.dateTo.set(iso(new Date(now.getFullYear(), now.getMonth() + 1, 0)));
    } else if (range === 'lastMonth') {
      this.dateFrom.set(iso(new Date(now.getFullYear(), now.getMonth() - 1, 1)));
      this.dateTo.set(iso(new Date(now.getFullYear(), now.getMonth(), 0)));
    }
    if (reload) this.loadTrips();
  }

  setDate(which: 'from' | 'to', value: string): void {
    this.quickRange.set('');
    (which === 'from' ? this.dateFrom : this.dateTo).set(value);
    this.loadTrips();
  }

  setRoute(id: string): void {
    this.routeFilter.set(id);
    if (this.busFilter() && !this.buses().some(b => b.id === this.busFilter() && (!id || b.busRouteId === id))) this.busFilter.set('');
  }

  busesOfRoute = computed(() => this.buses().filter(b => !this.routeFilter() || b.busRouteId === this.routeFilter()));

  print(): void {
    window.print();
  }

  /** One row per bus, as a CSV file for Excel (UTF-8 with BOM) */
  exportCsv(): void {
    const t = (key: string) => this.translate.instant(key);
    const header = [
      t('reports.transport.bus'), t('reports.transport.plate'), t('reports.transport.route'), t('reports.transport.driver'),
      t('reports.transport.capacity'), t('reports.transport.students'), t('reports.transport.occupancy'),
      t('reports.income.billed'), t('reports.income.collected'), t('reports.income.outstanding'), t('reports.transport.currency'),
      t('reports.transport.trips'), t('reports.transport.completed'), t('reports.transport.cancelled'),
      t('reports.transport.boarded'), t('reports.transport.absent'), t('reports.transport.avgDuration')
    ];
    const lines = this.busRows().flatMap(r => {
      const fees = r.fees.length ? r.fees : [{ currency: '', billed: 0, collected: 0, outstanding: 0 }];
      return fees.map(f => [
        r.bus.number, r.bus.plateNumber, r.route, r.bus.driverName, r.bus.capacity, r.students, `${r.occupancy}%`,
        f.billed, f.collected, f.outstanding, f.currency, r.trips, r.completed, r.cancelled, r.boarded, r.absent, r.avgMinutes ?? ''
      ]);
    });
    const cell = (v: unknown) => {
      const s = String(v ?? '');
      return /[",\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
    };
    const csv = '﻿' + [header, ...lines].map(row => row.map(cell).join(',')).join('\r\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = `transport-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  /** Red when full, amber from 85%, green below */
  occupancyClass(p: number): string {
    return p >= 100 ? 'bad' : p >= 85 ? 'warn' : 'good';
  }

  rate(part: number, total: number): number {
    return total > 0 ? Math.round((part * 100) / total) : 0;
  }

  amount(value: number, currency?: string): string {
    const n = new Intl.NumberFormat(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US', { maximumFractionDigits: 2 }).format(value);
    return currency ? `${n} ${currency}` : n;
  }

  dayLabel(date: string): string {
    const d = new Date(date + 'T00:00:00');
    return isNaN(d.getTime())
      ? date
      : d.toLocaleDateString(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-GB', { weekday: 'short', day: 'numeric', month: 'short' });
  }

  nameOf(item: { nameAr: string; nameEn: string } | undefined, lang = this.currentLanguage()): string {
    return item ? ((lang === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr) || '') : '';
  }
}
