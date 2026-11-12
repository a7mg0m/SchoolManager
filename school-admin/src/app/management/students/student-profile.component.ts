import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, forkJoin, of } from 'rxjs';
import { catchError, map, switchMap, takeUntil, tap } from 'rxjs/operators';
import { StudentService } from '../../core/services/student.service';
import { ParentService } from '../../core/services/parent.service';
import { StudentFeeService } from '../../core/services/student-fee.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { StageService } from '../../core/services/stage.service';
import { GradeService } from '../../core/services/grade.service';
import { SectionService } from '../../core/services/section.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Student } from '../../core/models/student.model';
import { Parent } from '../../core/models/parent.model';
import {
  StudentFee,
  FeeStatus,
  FEE_STATUSES,
  effectiveFeeStatus,
  feeStatusInfo,
  isFeePastDue
} from '../../core/models/student-fee.model';
import { AcademicYear } from '../../core/models/academic-year.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';

interface CurrencySummary {
  currency: string;
  total: number;
  discount: number;
  net: number;
  paid: number;
  balance: number;
  waived: number;
}

@Component({
  selector: 'app-student-profile',
  standalone: false,
  styleUrl: './student-profile.component.scss',
  templateUrl: './student-profile.component.html',
})
export class StudentProfileComponent implements OnInit, OnDestroy {
  student = signal<Student | null>(null);
  parent = signal<Parent | null>(null);
  fees = signal<StudentFee[]>([]);
  currentLanguage = signal<string>('en');

  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  notFound = signal(false);
  feesUnavailable = signal(false);

  private academicYearMap = signal(new Map<string, AcademicYear>());
  private stageMap = signal(new Map<string, Stage>());
  private gradeMap = signal(new Map<string, Grade>());
  private sectionMap = signal(new Map<string, Section>());

  /** '' = all statuses */
  statusFilter = signal<string>('');
  readonly statuses = FEE_STATUSES;

  sortedFees = computed(() => [...this.fees()].sort((a, b) => a.dueDate.localeCompare(b.dueDate)));

  visibleFees = computed(() => {
    const status = this.statusFilter();
    return status === '' ? this.sortedFees() : this.sortedFees().filter(f => effectiveFeeStatus(f) === Number(status));
  });

  /** Count per effective status, for the filter chips */
  statusCounts = computed(() => {
    const counts = new Map<FeeStatus, number>();
    for (const f of this.fees()) {
      const s = effectiveFeeStatus(f);
      counts.set(s, (counts.get(s) ?? 0) + 1);
    }
    return counts;
  });

  summary = computed<CurrencySummary[]>(() => {
    const map = new Map<string, CurrencySummary>();
    for (const f of this.fees()) {
      const s = map.get(f.currency) ?? { currency: f.currency, total: 0, discount: 0, net: 0, paid: 0, balance: 0, waived: 0 };
      s.waived += f.waivedAmount || 0;
      s.total += f.totalAmount || 0;
      s.discount += f.discountAmount || 0;
      s.net += f.netAmount || 0;
      s.paid += f.paidAmount || 0;
      s.balance += f.balance || 0;
      map.set(f.currency, s);
    }
    return [...map.values()];
  });

  overdueCount = computed(() => this.statusCounts().get(FeeStatus.Overdue) ?? 0);

  /** Earliest unpaid fee, shown as "next due" */
  nextDue = computed(() =>
    this.sortedFees().find(f => f.balance > 0 && effectiveFeeStatus(f) !== FeeStatus.Waived) ?? null
  );

  private destroy$ = new Subject<void>();

  constructor(
    private studentService: StudentService,
    private parentService: ParentService,
    private studentFeeService: StudentFeeService,
    private academicYearService: AcademicYearService,
    private stageService: StageService,
    private gradeService: GradeService,
    private sectionService: SectionService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.activatedRoute.paramMap
      .pipe(
        map(params => params.get('id') ?? ''),
        tap(() => this.resetState()),
        switchMap(id => this.load(id)),
        takeUntil(this.destroy$)
      )
      .subscribe();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  reload(): void {
    const id = this.student()?.id ?? this.activatedRoute.snapshot.paramMap.get('id') ?? '';
    this.resetState();
    this.load(id).pipe(takeUntil(this.destroy$)).subscribe();
  }

  private resetState(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.notFound.set(false);
    this.feesUnavailable.set(false);
    this.statusFilter.set('');
  }

  /** Student first (needed for parentId), then parent, fees and lookups in parallel */
  private load(id: string): Observable<unknown> {
    const safe = <T>(obs: Observable<T>) => obs.pipe(catchError(() => of(null as T | null)));
    const ok = <T>(r: { status: number; data: T[] } | null) => (r && r.status === 200 && r.data ? r.data : []);

    return this.studentService.getById(id).pipe(
      switchMap(response => {
        if (response.status !== 200 || !response.data) {
          this.notFound.set(true);
          return of(null);
        }
        const student = response.data;
        this.student.set(student);

        return forkJoin({
          parent: student.parentId ? safe(this.parentService.getById(student.parentId)) : of(null),
          fees: safe(this.studentFeeService.getByStudent(student.id)),
          academicYears: safe(this.academicYearService.getAll()),
          stages: safe(this.stageService.getAllStages()),
          grades: safe(this.gradeService.getAllGrades()),
          sections: safe(this.sectionService.getAll())
        }).pipe(
          tap(({ parent, fees, academicYears, stages, grades, sections }) => {
            this.parent.set(parent?.status === 200 ? parent.data : null);
            this.academicYearMap.set(new Map(ok(academicYears).map(a => [a.id, a])));
            this.stageMap.set(new Map(ok(stages).map(s => [s.id, s])));
            this.gradeMap.set(new Map(ok(grades).map(g => [g.id, g])));
            this.sectionMap.set(new Map(ok(sections).map(s => [s.id, s])));
            if (fees?.status === 200) {
              this.fees.set(fees.data || []);
            } else {
              this.fees.set([]);
              this.feesUnavailable.set(true);
            }
          })
        );
      }),
      tap(() => this.isLoading.set(false)),
      catchError(error => {
        console.error('Error loading student profile:', error);
        if (error?.status === 404) {
          this.notFound.set(true);
        } else {
          this.errorMessage.set(this.translate.instant('students.loadProfileFailed'));
        }
        this.isLoading.set(false);
        return of(null);
      })
    );
  }

  // ---------------------------------------------------------------------------
  // Navigation
  // ---------------------------------------------------------------------------

  backToList(): void {
    this.router.navigate(['/management/students']);
  }

  /** Students are edited through their parent */
  editViaParent(): void {
    const parentId = this.student()?.parentId;
    if (parentId) this.router.navigate(['/management/parents/save', parentId]);
  }

  manageFees(): void {
    const id = this.student()?.id;
    if (id) this.router.navigate(['/financial/student-fees/student', id]);
  }

  assignFees(): void {
    const id = this.student()?.id;
    if (id) this.router.navigate(['/financial/student-fees/assign'], { queryParams: { studentId: id } });
  }

  setStatusFilter(value: string): void {
    this.statusFilter.set(this.statusFilter() === value ? '' : value);
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  displayName(item: { fullNameAr: string; fullNameEn: string } | null): string {
    if (!item) return '-';
    return this.currentLanguage() === 'ar' ? (item.fullNameAr || item.fullNameEn) : (item.fullNameEn || item.fullNameAr);
  }

  altName(item: { fullNameAr: string; fullNameEn: string } | null): string {
    if (!item) return '';
    const alt = this.currentLanguage() === 'ar' ? item.fullNameEn : item.fullNameAr;
    return alt && alt !== this.displayName(item) ? alt : '';
  }

  initials(s: Student): string {
    const parts = (s.fullNameEn || s.fullNameAr || '').trim().split(/\s+/).filter(Boolean);
    return ((parts[0]?.[0] ?? '') + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase() || '?';
  }

  private localized(item: { nameAr: string; nameEn: string } | undefined): string {
    if (!item) return '-';
    return this.currentLanguage() === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr;
  }

  stageName(s: Student): string {
    const stageId = s.stageId || this.gradeMap().get(s.gradeId)?.stageId;
    return this.localized(stageId ? this.stageMap().get(stageId) : undefined);
  }

  gradeName(id: string): string {
    return this.localized(this.gradeMap().get(id));
  }

  sectionName(id: string): string {
    return this.localized(this.sectionMap().get(id));
  }

  academicYearName(id: string): string {
    return this.academicYearMap().get(id)?.name || '-';
  }

  genderLabel(gender: string | null | undefined): string {
    if (gender === 'Male' || gender === 'Female') {
      return this.translate.instant(`parents.gender${gender}`);
    }
    return gender || '-';
  }

  /** Whole years since date of birth */
  age(dateOfBirth: string): number | null {
    const dob = (dateOfBirth || '').substring(0, 10);
    if (!dob) return null;
    const birth = new Date(dob + 'T00:00:00');
    if (isNaN(birth.getTime())) return null;
    const now = new Date();
    let years = now.getFullYear() - birth.getFullYear();
    const beforeBirthday =
      now.getMonth() < birth.getMonth() || (now.getMonth() === birth.getMonth() && now.getDate() < birth.getDate());
    if (beforeBirthday) years--;
    return years >= 0 ? years : null;
  }

  statusInfo(fee: StudentFee) {
    return feeStatusInfo(fee);
  }

  isPastDue(fee: StudentFee): boolean {
    return isFeePastDue(fee) && fee.balance > 0;
  }

  statusCount(value: FeeStatus): number {
    return this.statusCounts().get(value) ?? 0;
  }

  paidPercent(net: number, paid: number, balance: number): number {
    if (!net || net <= 0) return balance > 0 ? 0 : 100;
    return Math.min(100, Math.round((paid / net) * 100));
  }

  formatDate(value: string): string {
    const date = (value || '').substring(0, 10);
    if (!date) return '-';
    try {
      return new Date(date + 'T00:00:00').toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
    } catch {
      return date;
    }
  }

  formatAmount(amount: number, currency?: string): string {
    const formatted = (amount ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    return currency ? `${formatted} ${currency}` : formatted;
  }
}
