import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, forkJoin, of } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { ExpenseService } from '../../core/services/expense.service';
import { ExpenseCategoryService } from '../../core/services/expense-category.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { EXPENSE_TYPES, Expense, ExpenseType } from '../../core/models/expense.model';
import { ExpenseCategory } from '../../core/models/expense-category.model';
import { AcademicYear } from '../../core/models/academic-year.model';

/** Sum of one group (category / type / month) in one currency */
interface GroupTotal {
  key: string;
  label: string;
  currency: string;
  amount: number;
  count: number;
  /** Share of the currency's total (0..100) */
  share: number;
}

type QuickRange = '' | 'thisMonth' | 'lastMonth' | 'thisYear';

/** "yyyy-MM-dd" (local) */
function iso(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/**
 * Report: expenses — /reports/expenses
 * Filters by academic year, dates, category, type, currency and text; totals per currency, by category, by type and
 * by month, then the expenses; export to Excel (CSV) and print. Amounts are never added across currencies.
 */
@Component({
  selector: 'app-expenses-report',
  standalone: false,
  styleUrl: './expenses-report.component.scss',
  templateUrl: './expenses-report.component.html',
})
export class ExpensesReportComponent implements OnInit, OnDestroy {
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);
  currentLanguage = signal('en');

  private expenses = signal<Expense[]>([]);
  categories = signal<ExpenseCategory[]>([]);
  academicYears = signal<AcademicYear[]>([]);

  // Filters
  yearFilter = signal('');
  dateFrom = signal('');
  dateTo = signal('');
  quickRange = signal<QuickRange>('');
  categoryFilter = signal('');
  /** -1 = every type */
  typeFilter = signal<number>(-1);
  currencyFilter = signal('');
  searchTerm = signal('');

  readonly types = EXPENSE_TYPES;
  readonly generatedOn = new Date();

  private destroy$ = new Subject<void>();

  /** Currencies used by the expenses */
  currencies = computed(() => [...new Set(this.expenses().map(e => (e.currency || '').toUpperCase()).filter(Boolean))].sort());

  /** The expenses after every filter, newest first */
  filtered = computed(() => {
    const year = this.yearFilter();
    const from = this.dateFrom();
    const to = this.dateTo();
    const category = this.categoryFilter();
    const type = this.typeFilter();
    const currency = this.currencyFilter();
    const term = this.searchTerm().trim().toLowerCase();
    return this.expenses()
      .filter(e => {
        const d = (e.expenseDate || '').slice(0, 10);
        if (year && e.academicYearId !== year) return false;
        if (from && d < from) return false;
        if (to && d > to) return false;
        if (category && e.categoryId !== category) return false;
        if (type >= 0 && e.type !== type) return false;
        if (currency && (e.currency || '').toUpperCase() !== currency) return false;
        if (term) {
          const text = [e.title, e.description, e.reference, this.categoryName(e)].join(' ').toLowerCase();
          if (!text.includes(term)) return false;
        }
        return true;
      })
      .sort((a, b) => (b.expenseDate || '').localeCompare(a.expenseDate || ''));
  });

  /** Total, count, average and largest per currency */
  totals = computed(() => {
    const byCurrency = new Map<string, { currency: string; amount: number; count: number; max: number }>();
    for (const e of this.filtered()) {
      const c = (e.currency || '-').toUpperCase();
      const t = byCurrency.get(c) ?? { currency: c, amount: 0, count: 0, max: 0 };
      t.amount += e.amount;
      t.count++;
      t.max = Math.max(t.max, e.amount);
      byCurrency.set(c, t);
    }
    return [...byCurrency.values()]
      .map(t => ({ ...t, average: t.count ? t.amount / t.count : 0 }))
      .sort((a, b) => b.amount - a.amount);
  });

  byCategory = computed(() => this.group(e => e.categoryId || '-', e => this.categoryName(e)));

  byType = computed(() => {
    const lang = this.currentLanguage(); // labels follow the language
    void lang;
    return this.group(e => String(e.type), e => this.typeLabel(e.type));
  });

  /** Per month (newest first) */
  byMonth = computed(() =>
    this.group(e => (e.expenseDate || '').slice(0, 7), e => this.monthLabel((e.expenseDate || '').slice(0, 7)))
      .sort((a, b) => b.key.localeCompare(a.key) || a.currency.localeCompare(b.currency)));

  hasFilters = computed(() =>
    !!(this.yearFilter() || this.dateFrom() || this.dateTo() || this.categoryFilter() || this.typeFilter() >= 0
      || this.currencyFilter() || this.searchTerm()));

  constructor(
    private expenseService: ExpenseService,
    private categoryService: ExpenseCategoryService,
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
      expenses: list(this.expenseService.getAll()),
      categories: list(this.categoryService.getAll()),
      years: list(this.academicYearService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe(r => {
        this.expenses.set(r.expenses);
        this.categories.set(r.categories);
        this.academicYears.set(r.years);
        if (failed) this.errorMessage.set(this.translate.instant('reports.loadFailed'));
        this.isLoading.set(false);
      });
  }

  /** This month / last month / this year */
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

  clearFilters(): void {
    this.yearFilter.set('');
    this.dateFrom.set('');
    this.dateTo.set('');
    this.quickRange.set('');
    this.categoryFilter.set('');
    this.typeFilter.set(-1);
    this.currencyFilter.set('');
    this.searchTerm.set('');
  }

  print(): void {
    window.print();
  }

  /** The rows shown, as a CSV file that Excel opens with Arabic text (UTF-8 with BOM) */
  exportCsv(): void {
    const t = (key: string) => this.translate.instant(key);
    const header = [
      t('reports.expenses.date'), t('reports.expenses.expenseTitle'), t('reports.expenses.category'), t('reports.expenses.type'),
      t('reports.expenses.amount'), t('reports.expenses.currency'), t('reports.expenses.reference'), t('parents.academicYear'),
      t('reports.expenses.description')
    ];
    const lines = this.filtered().map(e => [
      (e.expenseDate || '').slice(0, 10), e.title, this.categoryName(e), this.typeLabel(e.type), e.amount,
      (e.currency || '').toUpperCase(), e.reference ?? '', this.yearName(e.academicYearId), e.description ?? ''
    ]);
    const cell = (v: unknown) => {
      const s = String(v ?? '');
      return /[",\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s;
    };
    const csv = '﻿' + [header, ...lines].map(row => row.map(cell).join(',')).join('\r\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = `expenses-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  categoryName(e: Expense): string {
    return e.categoryName || this.categories().find(c => c.id === e.categoryId)?.name || '-';
  }

  typeLabel(type: ExpenseType): string {
    const key = EXPENSE_TYPES.find(t => t.value === type)?.key;
    return key ? this.translate.instant(key) : '-';
  }

  yearName(id: string | null): string {
    return id ? this.academicYears().find(y => y.id === id)?.name ?? '' : '';
  }

  /** "2026-11" -> "November 2026" */
  monthLabel(ym: string): string {
    const [y, m] = ym.split('-').map(Number);
    if (!y || !m) return ym || '-';
    return new Date(y, m - 1, 1).toLocaleDateString(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-GB', { month: 'long', year: 'numeric' });
  }

  amount(value: number, currency?: string): string {
    const n = new Intl.NumberFormat(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US', { maximumFractionDigits: 2 }).format(value);
    return currency ? `${n} ${currency.toUpperCase()}` : n;
  }

  /** Sums per (group, currency), biggest first, with each group's share of its currency total */
  private group(keyOf: (e: Expense) => string, labelOf: (e: Expense) => string): GroupTotal[] {
    const groups = new Map<string, GroupTotal>();
    const currencyTotals = new Map<string, number>();
    for (const e of this.filtered()) {
      const currency = (e.currency || '-').toUpperCase();
      const key = `${keyOf(e)}|${currency}`;
      const g = groups.get(key) ?? { key: keyOf(e), label: labelOf(e), currency, amount: 0, count: 0, share: 0 };
      g.amount += e.amount;
      g.count++;
      groups.set(key, g);
      currencyTotals.set(currency, (currencyTotals.get(currency) ?? 0) + e.amount);
    }
    return [...groups.values()]
      .map(g => {
        const total = currencyTotals.get(g.currency) ?? 0;
        return { ...g, share: total > 0 ? Math.round((g.amount * 100) / total) : 0 };
      })
      .sort((a, b) => a.currency.localeCompare(b.currency) || b.amount - a.amount);
  }
}
