import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, forkJoin, of } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { RevenueService } from '../../core/services/revenue.service';
import { RevenueCategoryService } from '../../core/services/revenue-category.service';
import { StudentFeeService } from '../../core/services/student-fee.service';
import { StudentService } from '../../core/services/student.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { REVENUE_TYPES, Revenue } from '../../core/models/revenue.model';
import { RevenueCategory } from '../../core/models/revenue-category.model';
import { StudentFee, StudentFeePayment } from '../../core/models/student-fee.model';
import { Student } from '../../core/models/student.model';
import { AcademicYear } from '../../core/models/academic-year.model';

/** Where the money came from */
export type IncomeSource = 'schoolFees' | 'busFees' | 'revenue';

/** One income line: a fee payment or a revenue */
interface IncomeLine {
  id: string;
  source: IncomeSource;
  /** yyyy-MM-dd */
  date: string;
  title: string;
  detail: string;
  categoryId: string | null;
  amount: number;
  currency: string;
  academicYearId: string | null;
}

interface GroupTotal {
  key: string;
  label: string;
  currency: string;
  amount: number;
  count: number;
  share: number;
}

type QuickRange = '' | 'thisMonth' | 'lastMonth' | 'thisYear';

function iso(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

const SOURCES: { value: IncomeSource; key: string; icon: string; css: string }[] = [
  { value: 'schoolFees', key: 'reports.income.sources.schoolFees', icon: 'pe-7s-study', css: 'school' },
  { value: 'busFees', key: 'reports.income.sources.busFees', icon: 'pe-7s-car', css: 'bus' },
  { value: 'revenue', key: 'reports.income.sources.revenue', icon: 'pe-7s-cash', css: 'revenue' }
];

/**
 * Report: income — /reports/income
 * Money received from school fees and bus fees (the payments applied to student fees, by date) and from revenues.
 * Filters by dates, academic year, source, revenue category and currency; totals per currency and source, by month,
 * revenues by category, the fees position (billed / collected / outstanding), then every line; Excel export and print.
 */
@Component({
  selector: 'app-income-report',
  standalone: false,
  styleUrl: './income-report.component.scss',
  templateUrl: './income-report.component.html',
})
export class IncomeReportComponent implements OnInit, OnDestroy {
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);
  currentLanguage = signal('en');

  private revenues = signal<Revenue[]>([]);
  private payments = signal<StudentFeePayment[]>([]);
  private fees = signal<StudentFee[]>([]);
  private students = signal<Map<string, Student>>(new Map());
  categories = signal<RevenueCategory[]>([]);
  academicYears = signal<AcademicYear[]>([]);

  yearFilter = signal('');
  dateFrom = signal('');
  dateTo = signal('');
  quickRange = signal<QuickRange>('');
  sourceFilter = signal<IncomeSource | ''>('');
  categoryFilter = signal('');
  currencyFilter = signal('');
  searchTerm = signal('');

  readonly sources = SOURCES;
  readonly generatedOn = new Date();
  private destroy$ = new Subject<void>();

  /** Every income line with names */
  private lines = computed<IncomeLine[]>(() => {
    const lang = this.currentLanguage();
    const students = this.students();
    const studentName = (id: string) => {
      const s = students.get(id);
      return s ? ((lang === 'ar' ? s.fullNameAr || s.fullNameEn : s.fullNameEn || s.fullNameAr) || '') : '';
    };
    const feeLines: IncomeLine[] = this.payments().map(p => ({
      id: p.id,
      source: p.isBusFee ? 'busFees' : 'schoolFees',
      date: (p.paidOn || '').slice(0, 10),
      title: studentName(p.studentId) || this.translate.instant('reports.income.unknownStudent'),
      detail: this.translate.instant(p.isBusFee ? 'reports.income.busPayment' : 'reports.income.feePayment'),
      categoryId: null,
      amount: p.amount,
      currency: (p.currency || '').toUpperCase(),
      academicYearId: p.academicYearId
    }));
    const revenueLines: IncomeLine[] = this.revenues().map(r => ({
      id: r.id,
      source: 'revenue',
      date: (r.revenueDate || '').slice(0, 10),
      title: r.title,
      detail: [r.categoryName || this.categoryName(r.categoryId), this.revenueTypeLabel(r)].filter(Boolean).join(' · '),
      categoryId: r.categoryId,
      amount: r.amount,
      currency: (r.currency || '').toUpperCase(),
      academicYearId: r.academicYearId
    }));
    return [...feeLines, ...revenueLines];
  });

  currencies = computed(() => [...new Set(this.lines().map(l => l.currency).filter(Boolean))].sort());

  filtered = computed(() => {
    const year = this.yearFilter();
    const from = this.dateFrom();
    const to = this.dateTo();
    const source = this.sourceFilter();
    const category = this.categoryFilter();
    const currency = this.currencyFilter();
    const term = this.searchTerm().trim().toLowerCase();
    return this.lines()
      .filter(l => {
        if (year && l.academicYearId !== year) return false;
        if (from && l.date < from) return false;
        if (to && l.date > to) return false;
        if (source && l.source !== source) return false;
        // A category is a revenue category: fee payments are left out
        if (category && l.categoryId !== category) return false;
        if (currency && l.currency !== currency) return false;
        return !term || `${l.title} ${l.detail}`.toLowerCase().includes(term);
      })
      .sort((a, b) => b.date.localeCompare(a.date));
  });

  /** Per currency: total and each source's part */
  totals = computed(() => {
    const byCurrency = new Map<string, { currency: string; amount: number; count: number; parts: Record<IncomeSource, { amount: number; count: number }> }>();
    for (const l of this.filtered()) {
      const t = byCurrency.get(l.currency) ?? {
        currency: l.currency,
        amount: 0,
        count: 0,
        parts: { schoolFees: { amount: 0, count: 0 }, busFees: { amount: 0, count: 0 }, revenue: { amount: 0, count: 0 } }
      };
      t.amount += l.amount;
      t.count++;
      t.parts[l.source].amount += l.amount;
      t.parts[l.source].count++;
      byCurrency.set(l.currency, t);
    }
    return [...byCurrency.values()]
      .map(t => ({
        ...t,
        shares: SOURCES.map(s => ({ ...s, ...t.parts[s.value], share: t.amount > 0 ? Math.round((t.parts[s.value].amount * 100) / t.amount) : 0 }))
      }))
      .sort((a, b) => b.amount - a.amount);
  });

  /** Per month and currency, with each source's amount */
  byMonth = computed(() => {
    const rows = new Map<string, { month: string; currency: string; schoolFees: number; busFees: number; revenue: number; total: number }>();
    for (const l of this.filtered()) {
      const month = l.date.slice(0, 7);
      const key = `${month}|${l.currency}`;
      const r = rows.get(key) ?? { month, currency: l.currency, schoolFees: 0, busFees: 0, revenue: 0, total: 0 };
      r[l.source] += l.amount;
      r.total += l.amount;
      rows.set(key, r);
    }
    return [...rows.values()].sort((a, b) => b.month.localeCompare(a.month) || a.currency.localeCompare(b.currency));
  });

  /** Revenues by category */
  revenueByCategory = computed<GroupTotal[]>(() => {
    const groups = new Map<string, GroupTotal>();
    const totals = new Map<string, number>();
    for (const l of this.filtered()) {
      if (l.source !== 'revenue') continue;
      const key = `${l.categoryId}|${l.currency}`;
      const g = groups.get(key) ?? { key, label: this.categoryName(l.categoryId), currency: l.currency, amount: 0, count: 0, share: 0 };
      g.amount += l.amount;
      g.count++;
      groups.set(key, g);
      totals.set(l.currency, (totals.get(l.currency) ?? 0) + l.amount);
    }
    return [...groups.values()]
      .map(g => ({ ...g, share: (totals.get(g.currency) ?? 0) > 0 ? Math.round((g.amount * 100) / totals.get(g.currency)!) : 0 }))
      .sort((a, b) => a.currency.localeCompare(b.currency) || b.amount - a.amount);
  });

  /**
   * Fees position (not by date: by the fees of the chosen year / currency): billed (net of discounts), collected,
   * waived and still outstanding, for school fees and bus fees.
   */
  feesPosition = computed(() => {
    const year = this.yearFilter();
    const currency = this.currencyFilter();
    const source = this.sourceFilter();
    const rows = new Map<string, { kind: IncomeSource; currency: string; billed: number; collected: number; waived: number; outstanding: number; count: number }>();
    for (const f of this.fees()) {
      const kind: IncomeSource = f.busAssignmentId ? 'busFees' : 'schoolFees';
      const cur = (f.currency || '').toUpperCase();
      if (year && f.academicYearId !== year) continue;
      if (currency && cur !== currency) continue;
      if (source && source !== kind) continue;
      const key = `${kind}|${cur}`;
      const r = rows.get(key) ?? { kind, currency: cur, billed: 0, collected: 0, waived: 0, outstanding: 0, count: 0 };
      r.billed += f.netAmount;
      r.collected += f.paidAmount;
      r.waived += f.waivedAmount ?? 0;
      r.outstanding += f.status === 3 ? 0 : f.balance;
      r.count++;
      rows.set(key, r);
    }
    return [...rows.values()]
      .map(r => ({ ...r, rate: r.billed > 0 ? Math.round((r.collected * 100) / r.billed) : 0 }))
      .sort((a, b) => a.kind.localeCompare(b.kind) || a.currency.localeCompare(b.currency));
  });

  hasFilters = computed(() =>
    !!(this.yearFilter() || this.dateFrom() || this.dateTo() || this.sourceFilter() || this.categoryFilter()
      || this.currencyFilter() || this.searchTerm()));

  constructor(
    private revenueService: RevenueService,
    private categoryService: RevenueCategoryService,
    private studentFeeService: StudentFeeService,
    private studentService: StudentService,
    private academicYearService: AcademicYearService,
    private languageService: LanguageService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$.pipe(takeUntil(this.destroy$)).subscribe(l => this.currentLanguage.set(l));
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

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
    forkJoin({
      revenues: list(this.revenueService.getAll()),
      payments: list(this.studentFeeService.getPayments()),
      fees: list(this.studentFeeService.getAll()),
      students: list(this.studentService.getAll()),
      categories: list(this.categoryService.getAll()),
      years: list(this.academicYearService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe(r => {
        this.revenues.set(r.revenues);
        this.payments.set(r.payments);
        this.fees.set(r.fees);
        this.students.set(new Map(r.students.map(s => [s.id, s])));
        this.categories.set(r.categories);
        this.academicYears.set(r.years);
        if (failed) this.errorMessage.set(this.translate.instant('reports.loadFailed'));
        this.isLoading.set(false);
      });
  }

  setQuickRange(range: QuickRange): void {
    this.quickRange.set(range);
    const now = new Date();
    if (range === 'thisMonth') {
      this.dateFrom.set(iso(new Date(now.getFullYear(), now.getMonth(), 1)));
      this.dateTo.set(iso(new Date(now.getFullYear(), now.getMonth() + 1, 0)));
    } else if (range === 'lastMonth') {
      this.dateFrom.set(iso(new Date(now.getFullYear(), now.getMonth() - 1, 1)));
      this.dateTo.set(iso(new Date(now.getFullYear(), now.getMonth(), 0)));
    } else if (range === 'thisYear') {
      this.dateFrom.set(iso(new Date(now.getFullYear(), 0, 1)));
      this.dateTo.set(iso(new Date(now.getFullYear(), 11, 31)));
    }
  }

  setDate(which: 'from' | 'to', value: string): void {
    this.quickRange.set('');
    (which === 'from' ? this.dateFrom : this.dateTo).set(value);
  }

  setSource(value: IncomeSource | ''): void {
    this.sourceFilter.set(value);
    // Revenue categories only make sense for revenues
    if (value && value !== 'revenue') this.categoryFilter.set('');
  }

  clearFilters(): void {
    this.yearFilter.set('');
    this.dateFrom.set('');
    this.dateTo.set('');
    this.quickRange.set('');
    this.sourceFilter.set('');
    this.categoryFilter.set('');
    this.currencyFilter.set('');
    this.searchTerm.set('');
  }

  print(): void {
    window.print();
  }

  exportCsv(): void {
    const t = (key: string) => this.translate.instant(key);
    const header = [
      t('reports.income.date'), t('reports.income.source'), t('reports.income.from'), t('reports.income.details'),
      t('reports.income.amount'), t('reports.income.currency'), t('parents.academicYear')
    ];
    const lines = this.filtered().map(l => [
      l.date, t(this.sourceInfo(l.source).key), l.title, l.detail, l.amount, l.currency, this.yearName(l.academicYearId)
    ]);
    const cell = (v: unknown) => {
      const s = String(v ?? '');
      return /[",\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
    };
    const csv = '﻿' + [header, ...lines].map(row => row.map(cell).join(',')).join('\r\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = `income-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  sourceInfo(source: IncomeSource) {
    return SOURCES.find(s => s.value === source) ?? SOURCES[0];
  }

  categoryName(id: string | null): string {
    return (id && this.categories().find(c => c.id === id)?.name) || '-';
  }

  revenueTypeLabel(r: Revenue): string {
    const key = REVENUE_TYPES.find(t => t.value === r.type)?.key;
    return key ? this.translate.instant(key) : '';
  }

  yearName(id: string | null): string {
    return id ? this.academicYears().find(y => y.id === id)?.name ?? '' : '';
  }

  monthLabel(ym: string): string {
    const [y, m] = ym.split('-').map(Number);
    if (!y || !m) return ym || '-';
    return new Date(y, m - 1, 1).toLocaleDateString(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-GB', { month: 'long', year: 'numeric' });
  }

  amount(value: number, currency?: string): string {
    const n = new Intl.NumberFormat(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US', { maximumFractionDigits: 2 }).format(value);
    return currency ? `${n} ${currency}` : n;
  }
}
