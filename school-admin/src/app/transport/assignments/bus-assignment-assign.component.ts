import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { BusAssignmentService } from '../../core/services/bus-assignment.service';
import { BusService } from '../../core/services/bus.service';
import { BusRouteService } from '../../core/services/bus-route.service';
import { StudentService } from '../../core/services/student.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { AcademicContextService } from '../../core/services/academic-context.service';
import { Bus, BusAssignment, BusRoute, BusRouteStop } from '../../core/models/transport.model';
import { Student } from '../../core/models/student.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, map, switchMap, takeUntil } from 'rxjs/operators';
import { transportApiError } from '../transport-errors';
import { todayIso } from '../../notification/shared/audience';

/** Suggestions for the currency field (same list as the fee page; free text is still allowed) */
const CURRENCY_SUGGESTIONS = ['EGP', 'SAR', 'AED', 'KWD', 'QAR', 'BHD', 'OMR', 'JOD', 'USD', 'EUR', 'GBP'];

/** Last currency used on the fee page (browser-local convenience) */
const LAST_CURRENCY_KEY = 'fees.lastCurrency';

@Component({
  selector: 'app-bus-assignment-assign',
  standalone: false,
  styleUrl: './bus-assignment-assign.component.scss',
  templateUrl: './bus-assignment-assign.component.html',
})
export class BusAssignmentAssignComponent implements OnInit, OnDestroy {
  // Lookups
  buses = signal<Bus[]>([]);
  routes = signal<BusRoute[]>([]);
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  sections = signal<Section[]>([]);
  students = signal<Student[]>([]);
  /** Active assignments of every bus (seats taken / students already riding) */
  activeAssignments = signal<BusAssignment[]>([]);
  currentLanguage = signal<string>('en');

  // State
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  submitted = signal(false);

  // Selections
  busId = signal('');
  stopId = signal('');
  gradeId = signal('');
  sectionId = signal('');
  studentSearch = signal('');
  selectedStudentIds = signal<Set<string>>(new Set());
  feeAmount = signal<number | null>(null);
  currency = signal(this.readLastCurrency() || 'EGP');
  startDate = signal(todayIso());
  dueDate = signal(this.defaultDueDate());

  readonly currencies = CURRENCY_SUGGESTIONS;

  /** Active buses, by number */
  activeBuses = computed(() =>
    this.buses().filter(b => b.isActive).sort((a, b) => a.number.localeCompare(b.number, undefined, { numeric: true })));

  selectedBus = computed(() => this.buses().find(b => b.id === this.busId()) ?? null);

  /** Stops on the selected bus's route, in order */
  busStops = computed<BusRouteStop[]>(() => {
    const bus = this.selectedBus();
    const route = bus ? this.routes().find(r => r.id === bus.busRouteId) : undefined;
    return [...(route?.stops || [])].sort((a, b) => a.order - b.order);
  });

  /** Active assignments of the header's academic year */
  private yearAssignments = computed(() => {
    const yearId = this.academic.yearId();
    return this.activeAssignments().filter(a => a.academicYearId === yearId);
  });

  /** studentId -> bus they already ride this year (they cannot be assigned again) */
  ridingBus = computed(() => new Map(this.yearAssignments().map(a => [a.studentId, a.busId])));

  /** Free seats on a bus in the header's year */
  freeSeats(bus: Bus): number {
    const taken = this.yearAssignments().filter(a => a.busId === bus.id).length;
    return Math.max(bus.capacity - taken, 0);
  }

  selectedFreeSeats = computed(() => {
    const bus = this.selectedBus();
    return bus ? this.freeSeats(bus) : 0;
  });

  overCapacity = computed(() => !!this.selectedBus() && this.selectedStudentIds().size > this.selectedFreeSeats());

  activeGrades = computed(() => this.grades().filter(g => g.isActive !== false));

  gradeSections = computed(() => this.sections().filter(s => s.gradeId === this.gradeId()));

  /** Active students of the chosen grade (and section), by name */
  gradeStudents = computed(() =>
    this.students()
      .filter(s => !!this.gradeId() && s.gradeId === this.gradeId() && s.isActive !== false)
      .filter(s => !this.sectionId() || s.sectionId === this.sectionId())
      .sort((a, b) => this.studentName(a).localeCompare(this.studentName(b))));

  visibleStudents = computed(() => {
    const term = this.studentSearch().trim().toLowerCase();
    if (!term) return this.gradeStudents();
    return this.gradeStudents().filter(s => `${s.fullNameAr} ${s.fullNameEn} ${s.nationalId}`.toLowerCase().includes(term));
  });

  /** Visible students that can be picked (not riding a bus this year yet) */
  pickableVisible = computed(() => this.visibleStudents().filter(s => !this.ridingBus().has(s.id)));

  allVisibleSelected = computed(() => {
    const visible = this.pickableVisible();
    return visible.length > 0 && visible.every(s => this.selectedStudentIds().has(s.id));
  });

  totalFees = computed(() => (this.feeAmount() || 0) * this.selectedStudentIds().size);

  canSubmit = computed(() =>
    !!this.academic.yearId() &&
    !!this.busId() &&
    this.selectedStudentIds().size > 0 &&
    !this.overCapacity() &&
    (this.feeAmount() ?? 0) > 0 &&
    !!this.currency().trim() &&
    !!this.startDate() &&
    !!this.dueDate() &&
    !this.isSaving()
  );

  private destroy$ = new Subject<void>();
  /** Bus pre-selected from the query string (?busId=...) */
  private presetBusId: string | null = null;

  constructor(
    private assignmentService: BusAssignmentService,
    private busService: BusService,
    private routeService: BusRouteService,
    private studentService: StudentService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router,
    public academic: AcademicContextService
  ) {
    this.academic.load();
  }

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.presetBusId = this.activatedRoute.snapshot.queryParamMap.get('busId');
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      buses: this.busService.getAll(),
      routes: this.routeService.getAll(),
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getAllGrades(),
      sections: this.sectionService.getAll(),
      students: this.studentService.getAll()
    })
      .pipe(
        switchMap(res => {
          this.buses.set(res.buses.data || []);
          this.routes.set(res.routes.data || []);
          this.stages.set(res.stages.data || []);
          this.grades.set(res.grades.data || []);
          this.sections.set(res.sections.data || []);
          this.students.set(res.students.data || []);
          const buses = res.buses.data || [];
          return buses.length === 0
            ? of([] as BusAssignment[][])
            : forkJoin(buses.map(b => this.assignmentService.getByBus(b.id).pipe(
                map(r => (r.data || []).filter(a => a.isActive)),
                catchError(() => of([] as BusAssignment[]))
              )));
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (lists) => {
          this.activeAssignments.set(lists.flat());
          const preset = this.activeBuses().find(b => b.id === this.presetBusId);
          if (preset) this.busId.set(preset.id);
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading assign data:', error);
          this.errorMessage.set(this.translate.instant('transport.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Selection handlers
  // ---------------------------------------------------------------------------

  onBusChange(event: Event): void {
    this.busId.set((event.target as HTMLSelectElement).value);
    this.stopId.set('');
  }

  onStopChange(event: Event): void {
    this.stopId.set((event.target as HTMLSelectElement).value);
  }

  onGradeChange(event: Event): void {
    this.gradeId.set((event.target as HTMLSelectElement).value);
    this.sectionId.set('');
    this.studentSearch.set('');
  }

  onSectionChange(event: Event): void {
    this.sectionId.set((event.target as HTMLSelectElement).value);
  }

  onStudentSearch(event: Event): void {
    this.studentSearch.set((event.target as HTMLInputElement).value);
  }

  toggleStudent(id: string): void {
    if (this.ridingBus().has(id)) return;
    this.selectedStudentIds.update(set => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  toggleAllVisible(): void {
    const visibleIds = this.pickableVisible().map(s => s.id);
    const selectAll = !this.allVisibleSelected();
    this.selectedStudentIds.update(set => {
      const next = new Set(set);
      visibleIds.forEach(id => (selectAll ? next.add(id) : next.delete(id)));
      return next;
    });
  }

  clearSelection(): void {
    this.selectedStudentIds.set(new Set());
  }

  onFeeAmount(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.feeAmount.set(value === '' ? null : Number(value));
  }

  onCurrency(event: Event): void {
    this.currency.set((event.target as HTMLInputElement).value.toUpperCase());
  }

  onStartDate(event: Event): void {
    this.startDate.set((event.target as HTMLInputElement).value);
  }

  onDueDate(event: Event): void {
    this.dueDate.set((event.target as HTMLInputElement).value);
  }

  // ---------------------------------------------------------------------------
  // Submit
  // ---------------------------------------------------------------------------

  onSubmit(): void {
    this.submitted.set(true);
    if (!this.canSubmit()) return;

    this.isSaving.set(true);
    this.errorMessage.set(null);

    this.assignmentService
      .assign({
        busId: this.busId(),
        academicYearId: this.academic.yearId(),
        busRouteStopId: this.stopId() || null,
        studentIds: [...this.selectedStudentIds()],
        feeAmount: Number(this.feeAmount()),
        currency: this.currency().trim(),
        startDate: this.startDate(),
        dueDate: `${this.dueDate()}T00:00:00`
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            const count = response.data?.length ?? this.selectedStudentIds().size;
            this.successMessage.set(this.translate.instant('transport.assigned', { count }));
            this.isSaving.set(false);
            setTimeout(() => this.navigateBack(), 1000);
          } else {
            // Domain validation errors come back as status 400 with a message
            this.errorMessage.set(transportApiError(this.translate, response.message, 'transport.saveFailed'));
            this.isSaving.set(false);
          }
        },
        error: (error) => {
          console.error('Error assigning students to bus:', error);
          this.errorMessage.set(transportApiError(this.translate, error?.error?.message, 'transport.saveFailed'));
          this.isSaving.set(false);
        }
      });
  }

  navigateBack(): void {
    this.router.navigate(['/transport/assignments'], this.busId() ? { queryParams: { busId: this.busId() } } : {});
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  localName(item: { nameAr: string; nameEn: string } | undefined): string {
    if (!item) return '-';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  stageName(stageId: string): string {
    const stage = this.stages().find(s => s.id === stageId);
    return stage ? this.localName(stage) : '';
  }

  studentName(s: Student): string {
    return this.currentLanguage() === 'ar' ? (s.fullNameAr || s.fullNameEn) : (s.fullNameEn || s.fullNameAr);
  }

  busNumber(busId: string): string {
    return this.buses().find(b => b.id === busId)?.number ?? '-';
  }

  formatAmount(amount: number, currency?: string): string {
    const formatted = (amount ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    return currency ? `${formatted} ${currency}` : formatted;
  }

  /** Default due date: one month from today */
  private defaultDueDate(): string {
    const d = new Date();
    d.setMonth(d.getMonth() + 1);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  private readLastCurrency(): string {
    try {
      return localStorage.getItem(LAST_CURRENCY_KEY) || '';
    } catch {
      return '';
    }
  }
}
