import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ChartData, ChartOptions } from 'chart.js';
import { Observable, Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { StudentService } from '../core/services/student.service';
import { TeacherService } from '../core/services/teacher.service';
import { ParentService } from '../core/services/parent.service';
import { SectionService } from '../core/services/section.service';
import { GradeService } from '../core/services/grade.service';
import { StageService } from '../core/services/stage.service';
import { AcademicYearService } from '../core/services/academic-year.service';
import { StudentFeeService } from '../core/services/student-fee.service';
import { RevenueService } from '../core/services/revenue.service';
import { ExpenseService } from '../core/services/expense.service';
import { SchoolEventService } from '../core/services/school-event.service';
import { NotificationService } from '../core/services/notification.service';
import { LanguageService } from '../ThemeOptions/store/language.service';
import { Student } from '../core/models/student.model';
import { Teacher } from '../core/models/teacher.model';
import { Parent } from '../core/models/parent.model';
import { Section } from '../core/models/section.model';
import { Grade } from '../core/models/grade.model';
import { Stage } from '../core/models/stage.model';
import { AcademicYear } from '../core/models/academic-year.model';
import { StudentFee, FeeStatus, effectiveFeeStatus } from '../core/models/student-fee.model';
import { Revenue } from '../core/models/revenue.model';
import { Expense } from '../core/models/expense.model';
import { SchoolEvent } from '../core/models/school-event.model';
import { AppNotification, NOTIFICATION_TYPES } from '../core/models/notification.model';

/**
 * Chart tokens – the dataviz reference palette (validated: categorical slots 1–2 pass all
 * checks on the light surface; status steps are the fixed status palette, always shown with
 * an icon + label because warning/serious sit below 3:1 on light).
 */
const VIZ = {
  series1: '#2a78d6', // blue  – revenue, students per grade (single series)
  series2: '#eb6834', // orange – expenses
  good: '#0ca30c', // collected
  warning: '#fab219', // outstanding (not yet due)
  critical: '#d03b3b', // overdue
  neutral: '#c3c2b7', // waived – a non-state, recessive gray
  surface: '#fcfcfb',
  textPrimary: '#0b0b0b',
  textSecondary: '#52514e',
  muted: '#898781',
  grid: '#e1e0d9',
  axis: '#c3c2b7'
};

/** One segment of the fee collection bar */
export interface FeeSegment {
  key: 'collected' | 'waived' | 'outstanding' | 'overdue';
  amount: number;
  color: string;
  icon: string;
  percent: number;
}

interface FeeOverview {
  currency: string;
  net: number;
  collected: number;
  collectionRate: number;
  segments: FeeSegment[];
  overdueCount: number;
  feeCount: number;
}

interface MonthRow {
  key: string;
  label: string;
  revenue: number;
  expense: number;
}

const MONTHS_SHOWN = 6;

@Component({
  selector: 'app-home',
  standalone: false,
  styleUrl: './home.scss',
  templateUrl: './home.html',
})
export class HomeComponent implements OnInit, OnDestroy {
  isLoading = signal(true);
  currentLanguage = signal('en');
  /** Data sources that failed to load (the rest of the dashboard still renders) */
  failedSources = signal<string[]>([]);

  private students = signal<Student[]>([]);
  private teachers = signal<Teacher[]>([]);
  private parents = signal<Parent[]>([]);
  private sections = signal<Section[]>([]);
  private grades = signal<Grade[]>([]);
  private stages = signal<Stage[]>([]);
  private academicYears = signal<AcademicYear[]>([]);
  private fees = signal<StudentFee[]>([]);
  private revenues = signal<Revenue[]>([]);
  private expenses = signal<Expense[]>([]);
  private events = signal<SchoolEvent[]>([]);
  private notifications = signal<AppNotification[]>([]);

  /** Per-chart "show as table" toggles */
  showTable = signal<Record<string, boolean>>({});

  /** Currency for the money widgets (the one with the most records); user can switch */
  selectedCurrency = signal<string | null>(null);

  // ---------------------------------------------------------------------------
  // Stat tiles
  // ---------------------------------------------------------------------------

  stats = computed(() => {
    const students = this.students();
    const active = students.filter(s => s.isActive !== false).length;
    return {
      students: students.length,
      activeStudents: active,
      teachers: this.teachers().filter(t => t.isActive !== false).length,
      parents: this.parents().length,
      sections: this.sections().length,
      upcomingEvents: this.upcomingEvents().length
    };
  });

  currentYear = computed(() => {
    const today = this.todayIso();
    const years = this.academicYears();
    return (
      years.find(y => (y.startDate || '').substring(0, 10) <= today && today <= (y.endDate || '').substring(0, 10)) ??
      years.find(y => y.isActive) ??
      null
    );
  });

  currentSemester = computed(() => {
    const today = this.todayIso();
    return (
      this.currentYear()?.semesters?.find(
        s => (s.startDate || '').substring(0, 10) <= today && today <= (s.endDate || '').substring(0, 10)
      ) ?? null
    );
  });

  // ---------------------------------------------------------------------------
  // Money: currency handling
  // ---------------------------------------------------------------------------

  /** Currencies present in fees/revenues/expenses, most-used first */
  currencies = computed(() => {
    const counts = new Map<string, number>();
    const add = (c: string | null | undefined) => c && counts.set(c, (counts.get(c) ?? 0) + 1);
    this.fees().forEach(f => add(f.currency));
    this.revenues().forEach(r => add(r.currency));
    this.expenses().forEach(e => add(e.currency));
    return [...counts].sort((a, b) => b[1] - a[1]).map(([c]) => c);
  });

  currency = computed(() => this.selectedCurrency() ?? this.currencies()[0] ?? 'EGP');

  // ---------------------------------------------------------------------------
  // Fee collection
  // ---------------------------------------------------------------------------

  feeOverview = computed<FeeOverview>(() => {
    const currency = this.currency();
    const fees = this.fees().filter(f => f.currency === currency);
    let net = 0, collected = 0, waived = 0, outstanding = 0, overdue = 0, overdueCount = 0;

    for (const f of fees) {
      net += f.netAmount || 0;
      collected += f.paidAmount || 0;
      waived += f.waivedAmount || 0;
      if (effectiveFeeStatus(f) === FeeStatus.Overdue) {
        overdue += f.balance || 0;
        overdueCount++;
      } else {
        outstanding += f.balance || 0;
      }
    }

    const total = collected + waived + outstanding + overdue;
    const pct = (v: number) => (total > 0 ? (v / total) * 100 : 0);
    const segments: FeeSegment[] = [
      { key: 'collected', amount: collected, color: VIZ.good, icon: 'pe-7s-check', percent: pct(collected) },
      { key: 'waived', amount: waived, color: VIZ.neutral, icon: 'pe-7s-gift', percent: pct(waived) },
      { key: 'outstanding', amount: outstanding, color: VIZ.warning, icon: 'pe-7s-clock', percent: pct(outstanding) },
      { key: 'overdue', amount: overdue, color: VIZ.critical, icon: 'pe-7s-attention', percent: pct(overdue) }
    ];

    // Collection rate: share of what is still collectable (waived amounts are excluded)
    const collectable = collected + outstanding + overdue;
    return {
      currency,
      net,
      collected,
      collectionRate: collectable > 0 ? Math.round((collected / collectable) * 100) : 0,
      segments,
      overdueCount,
      feeCount: fees.length
    };
  });

  // ---------------------------------------------------------------------------
  // Students per grade
  // ---------------------------------------------------------------------------

  gradeRows = computed(() => {
    const lang = this.currentLanguage();
    const stageOrder = new Map(this.stages().map((s, i) => [s.id, i]));
    const counts = new Map<string, number>();
    for (const s of this.students()) counts.set(s.gradeId, (counts.get(s.gradeId) ?? 0) + 1);

    return this.grades()
      .filter(g => g.isActive !== false || counts.has(g.id))
      .map((g, index) => ({
        id: g.id,
        label: this.localized(g, lang),
        stage: this.localized(this.stages().find(s => s.id === g.stageId), lang),
        count: counts.get(g.id) ?? 0,
        order: (stageOrder.get(g.stageId) ?? 99) * 1000 + index
      }))
      .sort((a, b) => a.order - b.order);
  });

  gradeChartData = computed<ChartData<'bar'>>(() => {
    const rows = this.gradeRows();
    return {
      labels: rows.map(r => r.label),
      datasets: [
        {
          label: this.translate.instant('dashboard.students'),
          data: rows.map(r => r.count),
          backgroundColor: VIZ.series1,
          hoverBackgroundColor: '#256abf',
          borderRadius: { topLeft: 4, topRight: 4 },
          borderSkipped: 'bottom',
          maxBarThickness: 28,
          categoryPercentage: 0.7,
          barPercentage: 0.9
        }
      ]
    };
  });

  gradeChartOptions = computed<ChartOptions<'bar'>>(() => ({
    ...this.baseOptions(),
    plugins: {
      ...this.baseOptions().plugins,
      legend: { display: false }, // single series – the card title names it
      tooltip: {
        ...this.tooltipStyle(),
        callbacks: {
          title: items => {
            const row = this.gradeRows()[items[0].dataIndex];
            return row ? `${row.label} · ${row.stage}` : '';
          },
          label: item => ` ${item.formattedValue} ${this.translate.instant('dashboard.studentsUnit')}`
        }
      }
    },
    scales: {
      x: this.categoryAxis(),
      y: { ...this.valueAxis(), ticks: { ...this.valueAxis().ticks, precision: 0 } }
    }
  }));

  // ---------------------------------------------------------------------------
  // Revenue vs expenses (one shared axis – same unit)
  // ---------------------------------------------------------------------------

  monthRows = computed<MonthRow[]>(() => {
    const currency = this.currency();
    const rows: MonthRow[] = [];
    const now = new Date();
    for (let i = MONTHS_SHOWN - 1; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      rows.push({
        key: `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`,
        label: d.toLocaleDateString(this.currentLanguage() === 'ar' ? 'ar-EG' : undefined, { month: 'short', year: '2-digit' }),
        revenue: 0,
        expense: 0
      });
    }
    const byKey = new Map(rows.map(r => [r.key, r]));
    for (const r of this.revenues()) {
      if (r.currency !== currency) continue;
      const row = byKey.get((r.revenueDate || '').substring(0, 7));
      if (row) row.revenue += r.amount || 0;
    }
    for (const e of this.expenses()) {
      if (e.currency !== currency) continue;
      const row = byKey.get((e.expenseDate || '').substring(0, 7));
      if (row) row.expense += e.amount || 0;
    }
    return rows;
  });

  financeTotals = computed(() => {
    const rows = this.monthRows();
    const revenue = rows.reduce((s, r) => s + r.revenue, 0);
    const expense = rows.reduce((s, r) => s + r.expense, 0);
    return { revenue, expense, net: revenue - expense };
  });

  financeChartData = computed<ChartData<'line'>>(() => {
    const rows = this.monthRows();
    const line = (label: string, data: number[], color: string) => ({
      label,
      data,
      borderColor: color,
      backgroundColor: color,
      borderWidth: 2,
      pointRadius: 4, // 8px markers
      pointHoverRadius: 6,
      pointBorderColor: VIZ.surface, // 2px surface ring on overlapping marks
      pointBorderWidth: 2,
      tension: 0.25,
      fill: false
    });
    return {
      labels: rows.map(r => r.label),
      datasets: [
        line(this.translate.instant('dashboard.revenue'), rows.map(r => r.revenue), VIZ.series1),
        line(this.translate.instant('dashboard.expenses'), rows.map(r => r.expense), VIZ.series2)
      ]
    };
  });

  financeChartOptions = computed<ChartOptions<'line'>>(() => ({
    ...this.baseOptions(),
    interaction: { mode: 'index', intersect: false }, // crosshair-style: hover a month, see both
    plugins: {
      ...this.baseOptions().plugins,
      legend: {
        display: true,
        position: 'top',
        align: 'end',
        labels: { color: VIZ.textSecondary, usePointStyle: true, pointStyle: 'circle', boxWidth: 8, boxHeight: 8, padding: 16 }
      },
      tooltip: {
        ...this.tooltipStyle(),
        callbacks: {
          label: item => ` ${item.dataset.label}: ${this.formatAmount(Number(item.raw), this.currency())}`,
          footer: items => {
            const row = this.monthRows()[items[0]?.dataIndex ?? -1];
            return row ? `${this.translate.instant('dashboard.net')}: ${this.formatAmount(row.revenue - row.expense, this.currency())}` : '';
          }
        }
      }
    },
    scales: {
      x: this.categoryAxis(),
      y: {
        ...this.valueAxis(),
        ticks: { ...this.valueAxis().ticks, callback: value => this.compactNumber(Number(value)) }
      }
    }
  }));

  // ---------------------------------------------------------------------------
  // Lists
  // ---------------------------------------------------------------------------

  upcomingEvents = computed(() => {
    const today = this.todayIso();
    return this.events()
      .filter(e => e.eventDate >= today)
      .sort((a, b) => (a.eventDate + a.startTime).localeCompare(b.eventDate + b.startTime));
  });

  nextEvents = computed(() => this.upcomingEvents().slice(0, 5));

  latestNotifications = computed(() =>
    [...this.notifications()]
      .filter(n => n.sentOn <= this.todayIso())
      .sort((a, b) => (b.sentOn + b.createdOn).localeCompare(a.sentOn + a.createdOn))
      .slice(0, 5)
  );

  private destroy$ = new Subject<void>();

  constructor(
    private studentService: StudentService,
    private teacherService: TeacherService,
    private parentService: ParentService,
    private sectionService: SectionService,
    private gradeService: GradeService,
    private stageService: StageService,
    private academicYearService: AcademicYearService,
    private studentFeeService: StudentFeeService,
    private revenueService: RevenueService,
    private expenseService: ExpenseService,
    private schoolEventService: SchoolEventService,
    private notificationService: NotificationService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(lang => this.currentLanguage.set(lang)); // chart labels recompute from this signal

    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadData(): void {
    this.isLoading.set(true);
    const failed: string[] = [];

    // Every source is optional: one service being down must not blank the dashboard
    const safe = <T>(name: string, obs: Observable<{ status: number; data: T[] }>) =>
      obs.pipe(
        catchError(() => {
          failed.push(name);
          return of({ status: 0, data: [] as T[] });
        })
      );
    const list = <T>(r: { status: number; data: T[] }) => (r.status === 200 && r.data ? r.data : []);

    forkJoin({
      students: safe('management', this.studentService.getAll()),
      teachers: safe('management', this.teacherService.getAll()),
      parents: safe('management', this.parentService.getAll()),
      sections: safe('management', this.sectionService.getAll()),
      grades: safe('management', this.gradeService.getAllGrades()),
      stages: safe('management', this.stageService.getAllStages()),
      academicYears: safe('management', this.academicYearService.getAll()),
      fees: safe('financial', this.studentFeeService.getAll()),
      revenues: safe('financial', this.revenueService.getAll()),
      expenses: safe('financial', this.expenseService.getAll()),
      events: safe('notification', this.schoolEventService.getAll()),
      notifications: safe('notification', this.notificationService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe(r => {
        this.students.set(list(r.students));
        this.teachers.set(list(r.teachers));
        this.parents.set(list(r.parents));
        this.sections.set(list(r.sections));
        this.grades.set(list(r.grades));
        this.stages.set(list(r.stages));
        this.academicYears.set(list(r.academicYears));
        this.fees.set(list(r.fees));
        this.revenues.set(list(r.revenues));
        this.expenses.set(list(r.expenses));
        this.events.set(list(r.events));
        this.notifications.set(list(r.notifications));
        this.failedSources.set([...new Set(failed)]);
        this.isLoading.set(false);
      });
  }

  // ---------------------------------------------------------------------------
  // UI actions
  // ---------------------------------------------------------------------------

  toggleTable(chart: string): void {
    this.showTable.update(t => ({ ...t, [chart]: !t[chart] }));
  }

  isTable(chart: string): boolean {
    return !!this.showTable()[chart];
  }

  setCurrency(currency: string): void {
    this.selectedCurrency.set(currency);
  }

  go(path: string): void {
    this.router.navigate([path]);
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  notificationType(n: AppNotification) {
    return NOTIFICATION_TYPES.find(t => t.value === n.notificationType) ?? NOTIFICATION_TYPES[NOTIFICATION_TYPES.length - 1];
  }

  isToday(date: string): boolean {
    return (date || '').substring(0, 10) === this.todayIso();
  }

  dayOfMonth(date: string): string {
    return String(Number((date || '').substring(8, 10)) || '');
  }

  monthShort(date: string): string {
    const d = new Date((date || '').substring(0, 10) + 'T00:00:00');
    return isNaN(d.getTime()) ? '' : d.toLocaleDateString(this.currentLanguage() === 'ar' ? 'ar-EG' : undefined, { month: 'short' });
  }

  formatDate(date: string): string {
    const d = new Date((date || '').substring(0, 10) + 'T00:00:00');
    return isNaN(d.getTime())
      ? date
      : d.toLocaleDateString(this.currentLanguage() === 'ar' ? 'ar-EG' : undefined, { day: 'numeric', month: 'short', year: 'numeric' });
  }

  formatTime(time: string): string {
    const [h, m] = (time || '').substring(0, 5).split(':').map(Number);
    if (isNaN(h)) return '';
    const d = new Date();
    d.setHours(h, m || 0, 0, 0);
    return d.toLocaleTimeString(this.currentLanguage() === 'ar' ? 'ar-EG' : undefined, { hour: 'numeric', minute: '2-digit' });
  }

  formatAmount(amount: number, currency?: string): string {
    const formatted = (amount ?? 0).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 2 });
    return currency ? `${formatted} ${currency}` : formatted;
  }

  /** 12,500 → 12.5K for axis ticks */
  compactNumber(value: number): string {
    try {
      return new Intl.NumberFormat(undefined, { notation: 'compact', maximumFractionDigits: 1 }).format(value);
    } catch {
      return String(value);
    }
  }

  private localized(item: { nameAr?: string; nameEn?: string } | undefined, lang: string): string {
    if (!item) return '-';
    return (lang === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr) || '-';
  }

  private todayIso(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  // ---------------------------------------------------------------------------
  // Shared chart styling (recessive grid/axes, ink-colored text, never series-colored)
  // ---------------------------------------------------------------------------

  /** Chart-type-agnostic base options (spread into each typed chart's options) */
  private baseOptions() {
    return {
      responsive: true,
      maintainAspectRatio: false,
      animation: false as const,
      layout: { padding: { top: 4, right: 4, bottom: 0, left: 0 } },
      plugins: { legend: { display: false }, tooltip: this.tooltipStyle() }
    };
  }

  private tooltipStyle(): any {
    return {
      backgroundColor: '#ffffff',
      titleColor: VIZ.textPrimary,
      bodyColor: VIZ.textSecondary,
      footerColor: VIZ.textPrimary,
      borderColor: 'rgba(11,11,11,0.10)',
      borderWidth: 1,
      padding: 10,
      cornerRadius: 6,
      usePointStyle: true,
      boxPadding: 4,
      titleFont: { weight: '600' },
      footerFont: { weight: '600' }
    };
  }

  private categoryAxis(): any {
    return {
      grid: { display: false },
      border: { color: VIZ.axis },
      ticks: { color: VIZ.muted, font: { size: 11 }, maxRotation: 0, autoSkip: true }
    };
  }

  private valueAxis(): any {
    return {
      beginAtZero: true,
      grid: { color: VIZ.grid, drawTicks: false },
      border: { display: false },
      ticks: { color: VIZ.muted, font: { size: 11 }, padding: 8 }
    };
  }
}
