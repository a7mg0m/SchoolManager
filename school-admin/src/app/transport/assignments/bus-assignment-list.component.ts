import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { BusAssignmentService } from '../../core/services/bus-assignment.service';
import { BusService } from '../../core/services/bus.service';
import { BusRouteService } from '../../core/services/bus-route.service';
import { StudentService } from '../../core/services/student.service';
import { AcademicContextService } from '../../core/services/academic-context.service';
import { Bus, BusAssignment, BusRoute, BusRouteStop } from '../../core/models/transport.model';
import { Student } from '../../core/models/student.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Observable, Subject, forkJoin, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ApiResponse } from '../../core/models/stage.model';
import { transportApiError } from '../transport-errors';
import { formatDate, todayIso } from '../../notification/shared/audience';

/** Which row-action modal is open */
type AssignmentAction = 'fee' | 'move' | 'end';

@Component({
  selector: 'app-bus-assignment-list',
  standalone: false,
  styleUrl: './bus-assignment-list.component.scss',
  templateUrl: './bus-assignment-list.component.html',
})
export class BusAssignmentListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  buses = signal<Bus[]>([]);
  routes = signal<BusRoute[]>([]);
  students = signal<Student[]>([]);
  assignments = signal<BusAssignment[]>([]);
  isLoading = signal(false);
  isLoadingAssignments = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  modalError = signal<string | null>(null);
  currentLanguage = signal<string>('en');

  // Filters
  busId = signal('');
  searchTerm = signal('');
  showEnded = signal(false);

  // Modal fields
  editing = signal<BusAssignment | null>(null);
  action = signal<AssignmentAction>('fee');
  feeAmount = signal<number | null>(null);
  currency = signal('');
  moveBusId = signal('');
  moveStopId = signal('');
  endDate = signal(todayIso());

  selectedBus = computed(() => this.buses().find(b => b.id === this.busId()) ?? null);

  /** Assignments of the bus in the header's academic year, active first, by student name */
  filtered = computed(() => {
    const yearId = this.academic.yearId();
    const term = this.searchTerm().trim().toLowerCase();
    return this.assignments()
      .filter(a => !yearId || a.academicYearId === yearId)
      .filter(a => this.showEnded() || a.isActive)
      .filter(a => !term || this.studentSearchText(a.studentId).includes(term))
      .sort((a, b) => Number(b.isActive) - Number(a.isActive) || this.studentName(a.studentId).localeCompare(this.studentName(b.studentId)));
  });

  /** Seats taken on the selected bus in the header's year */
  seatsTaken = computed(() => {
    const yearId = this.academic.yearId();
    return this.assignments().filter(a => a.isActive && (!yearId || a.academicYearId === yearId)).length;
  });

  /** Stops of the bus chosen in the move modal */
  moveStops = computed(() => this.stopsOfBus(this.moveBusId()));

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private modalRef: NgbModalRef | null = null;

  constructor(
    private assignmentService: BusAssignmentService,
    private busService: BusService,
    private routeService: BusRouteService,
    private studentService: StudentService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
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
    this.loadLookups(this.activatedRoute.snapshot.queryParamMap.get('busId'));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Buses, routes (for stop names) and students; [busId] = bus chosen beforehand (from the bus list) */
  private loadLookups(busId: string | null): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      buses: this.busService.getAll(),
      routes: this.routeService.getAll(),
      students: this.studentService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ buses, routes, students }) => {
          const list = [...(buses.data || [])].sort((a, b) => a.number.localeCompare(b.number, undefined, { numeric: true }));
          this.buses.set(list);
          this.routes.set(routes.data || []);
          this.students.set(students.data || []);
          this.isLoading.set(false);
          const preset = list.find(b => b.id === busId) ?? (list.length === 1 ? list[0] : undefined);
          if (preset) {
            this.busId.set(preset.id);
            this.loadAssignments();
          }
        },
        error: (error) => {
          console.error('Error loading assignment lookups:', error);
          this.errorMessage.set(this.translate.instant('transport.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /** All assignments of the selected bus (the year is filtered on the page, so changing it needs no reload) */
  loadAssignments(): void {
    if (!this.busId()) {
      this.assignments.set([]);
      return;
    }

    this.isLoadingAssignments.set(true);
    this.errorMessage.set(null);

    this.assignmentService
      .getByBus(this.busId())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.assignments.set(response.data || []);
          } else {
            this.assignments.set([]);
            this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.loadFailed'));
          }
          this.isLoadingAssignments.set(false);
        },
        error: (error) => {
          console.error('Error loading bus assignments:', error);
          this.assignments.set([]);
          this.errorMessage.set(this.translate.instant('transport.loadFailed'));
          this.isLoadingAssignments.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Filters
  // ---------------------------------------------------------------------------

  onBusChange(event: Event): void {
    this.busId.set((event.target as HTMLSelectElement).value);
    this.router.navigate([], { queryParams: { busId: this.busId() || null }, replaceUrl: true });
    this.loadAssignments();
  }

  setSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  toggleShowEnded(): void {
    this.showEnded.update(v => !v);
  }

  navigateToAssign(): void {
    this.router.navigate(['/transport/assignments/assign'], this.busId() ? { queryParams: { busId: this.busId() } } : {});
  }

  // ---------------------------------------------------------------------------
  // Row actions (modals)
  // ---------------------------------------------------------------------------

  openAction(assignment: BusAssignment, action: AssignmentAction, content: any): void {
    this.editing.set(assignment);
    this.action.set(action);
    this.modalError.set(null);
    this.feeAmount.set(assignment.feeAmount);
    this.currency.set(assignment.currency);
    this.moveBusId.set('');
    this.moveStopId.set('');
    this.endDate.set(todayIso() < assignment.startDate ? assignment.startDate : todayIso());
    this.modalRef = this.modalService.open(content, { centered: true, backdrop: 'static', keyboard: false });
  }

  onFeeAmount(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.feeAmount.set(value === '' ? null : Number(value));
  }

  onCurrency(event: Event): void {
    this.currency.set((event.target as HTMLInputElement).value.toUpperCase());
  }

  onMoveBus(event: Event): void {
    this.moveBusId.set((event.target as HTMLSelectElement).value);
    this.moveStopId.set('');
  }

  onMoveStop(event: Event): void {
    this.moveStopId.set((event.target as HTMLSelectElement).value);
  }

  onEndDate(event: Event): void {
    this.endDate.set((event.target as HTMLInputElement).value);
  }

  /** Other active buses to move to */
  moveTargets(): Bus[] {
    return this.buses().filter(b => b.isActive && b.id !== this.editing()?.busId);
  }

  canConfirm(): boolean {
    const a = this.editing();
    if (!a || this.isSaving()) return false;
    switch (this.action()) {
      case 'fee': return (this.feeAmount() ?? 0) > 0 && !!this.currency().trim();
      case 'move': return !!this.moveBusId();
      case 'end': return !!this.endDate() && this.endDate() >= a.startDate;
    }
  }

  confirmAction(): void {
    const a = this.editing();
    if (!a || !this.canConfirm()) return;

    let request$: Observable<ApiResponse<null>>;
    let doneKey: string;
    switch (this.action()) {
      case 'fee':
        request$ = this.assignmentService.changeFee(a.id, { feeAmount: Number(this.feeAmount()), currency: this.currency().trim() });
        doneKey = 'transport.feeChanged';
        break;
      case 'move':
        request$ = this.assignmentService.move(a.id, { busId: this.moveBusId(), busRouteStopId: this.moveStopId() || null });
        doneKey = 'transport.moved';
        break;
      default:
        request$ = this.assignmentService.end(a.id, { endDate: this.endDate() });
        doneKey = 'transport.ended';
        break;
    }

    this.isSaving.set(true);
    this.modalError.set(null);

    request$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.closeModal();
          this.showSuccess(this.translate.instant(doneKey));
          this.loadAssignments();
        } else {
          this.modalError.set(transportApiError(this.translate, response.message, 'transport.saveFailed'));
        }
        this.isSaving.set(false);
      },
      error: (error) => {
        console.error('Error updating bus assignment:', error);
        this.modalError.set(transportApiError(this.translate, error?.error?.message, 'transport.saveFailed'));
        this.isSaving.set(false);
      }
    });
  }

  cancelModal(): void {
    this.closeModal();
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  localName(item: { nameAr: string; nameEn: string } | undefined): string {
    if (!item) return '-';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  studentName(id: string): string {
    const s = this.students().find(x => x.id === id);
    if (!s) return '-';
    return this.currentLanguage() === 'ar' ? (s.fullNameAr || s.fullNameEn) : (s.fullNameEn || s.fullNameAr);
  }

  studentNationalId(id: string): string {
    return this.students().find(x => x.id === id)?.nationalId ?? '';
  }

  stopName(stopId: string | null): string {
    if (!stopId) return '-';
    for (const route of this.routes()) {
      const stop = route.stops.find(s => s.id === stopId);
      if (stop) return this.localName(stop);
    }
    return '-';
  }

  busLabel(bus: Bus): string {
    return `${bus.number} · ${bus.plateNumber}`;
  }

  formatAmount(amount: number, currency: string): string {
    const formatted = (amount ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    return `${formatted} ${currency}`;
  }

  day(value: string | null): string {
    return value ? formatDate(value, { year: 'numeric', month: 'short', day: 'numeric' }) : '-';
  }

  private stopsOfBus(busId: string): BusRouteStop[] {
    const bus = this.buses().find(b => b.id === busId);
    const route = bus ? this.routes().find(r => r.id === bus.busRouteId) : undefined;
    return [...(route?.stops || [])].sort((a, b) => a.order - b.order);
  }

  private studentSearchText(id: string): string {
    const s = this.students().find(x => x.id === id);
    return s ? `${s.fullNameAr} ${s.fullNameEn} ${s.nationalId}`.toLowerCase() : '';
  }

  private closeModal(): void {
    this.modalRef?.close();
    this.modalRef = null;
    this.editing.set(null);
  }

  private showSuccess(message: string): void {
    this.errorMessage.set(null);
    this.successMessage.set(message);
    timer(4000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.successMessage.set(null));
  }
}
