import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
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
import { StudentFee, FeeStatus, effectiveFeeStatus } from '../../core/models/student-fee.model';
import { AcademicYear } from '../../core/models/academic-year.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';

/** Overall fee position of one student, used for the list badge and filter */
export type StudentFeeState = 'noFees' | 'paid' | 'outstanding' | 'overdue';

export interface StudentFeeOverview {
  state: StudentFeeState;
  /** Remaining balance per currency */
  balances: { currency: string; amount: number }[];
}

export const FEE_STATE_BADGES: Record<StudentFeeState, string> = {
  noFees: 'bg-light text-muted border',
  paid: 'bg-success',
  outstanding: 'bg-warning text-dark',
  overdue: 'bg-danger'
};

@Component({
  selector: 'app-student-list',
  standalone: false,
  styleUrl: './student-list.component.scss',
  templateUrl: './student-list.component.html',
})
export class StudentListComponent implements OnInit, OnDestroy {
  students = signal<Student[]>([]);
  currentLanguage = signal<string>('en');
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  /** The Financial service is optional for this page; the list still works without it */
  feesUnavailable = signal(false);

  // Lookups
  private parentMap = signal(new Map<string, Parent>());
  private academicYearMap = signal(new Map<string, AcademicYear>());
  private stageMap = signal(new Map<string, Stage>());
  private gradeMap = signal(new Map<string, Grade>());
  private sectionMap = signal(new Map<string, Section>());
  private feesByStudent = signal(new Map<string, StudentFee[]>());

  stages = computed(() => [...this.stageMap().values()]);

  // Filters
  searchTerm = signal('');
  stageFilter = signal('');
  gradeFilter = signal('');
  sectionFilter = signal('');
  feeStateFilter = signal<StudentFeeState | ''>('');
  page = signal(1);
  readonly pageSize = 10;

  readonly feeStates: StudentFeeState[] = ['noFees', 'paid', 'outstanding', 'overdue'];
  readonly feeStateBadges = FEE_STATE_BADGES;

  filterGrades = computed(() => {
    const stageId = this.stageFilter();
    const grades = [...this.gradeMap().values()];
    return stageId ? grades.filter(g => g.stageId === stageId) : grades;
  });

  filterSections = computed(() => {
    const gradeId = this.gradeFilter();
    return gradeId ? [...this.sectionMap().values()].filter(s => s.gradeId === gradeId) : [];
  });

  /** Fee overview per student, derived once from the fee list */
  private feeOverview = computed(() => {
    const map = new Map<string, StudentFeeOverview>();
    for (const student of this.students()) {
      map.set(student.id, this.buildOverview(this.feesByStudent().get(student.id) ?? []));
    }
    return map;
  });

  filteredStudents = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const stageId = this.stageFilter();
    const gradeId = this.gradeFilter();
    const sectionId = this.sectionFilter();
    const feeState = this.feeStateFilter();
    const overview = this.feeOverview();
    const parents = this.parentMap();

    return this.students().filter(s => {
      if (stageId && this.stageIdOf(s) !== stageId) return false;
      if (gradeId && s.gradeId !== gradeId) return false;
      if (sectionId && s.sectionId !== sectionId) return false;
      if (feeState && overview.get(s.id)?.state !== feeState) return false;
      if (term) {
        const parent = parents.get(s.parentId);
        const haystack = [s.fullNameAr, s.fullNameEn, s.nationalId, s.phoneNumber, s.email, parent?.fullNameAr, parent?.fullNameEn]
          .map(v => (v ?? '').toLowerCase())
          .join(' ');
        if (!haystack.includes(term)) return false;
      }
      return true;
    });
  });

  pagedStudents = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filteredStudents().slice(start, start + this.pageSize);
  });

  hasActiveFilters = computed(() =>
    !!(this.searchTerm() || this.stageFilter() || this.gradeFilter() || this.sectionFilter() || this.feeStateFilter())
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
    private router: Router
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.feesUnavailable.set(false);

    // Lookups and fees are best-effort: a failure there shouldn't hide the student list
    const safe = <T>(obs: Observable<T>) => obs.pipe(catchError(() => of(null as T | null)));

    forkJoin({
      students: this.studentService.getAll(),
      parents: safe(this.parentService.getAll()),
      fees: safe(this.studentFeeService.getAll()),
      academicYears: safe(this.academicYearService.getAll()),
      stages: safe(this.stageService.getAllStages()),
      grades: safe(this.gradeService.getAllGrades()),
      sections: safe(this.sectionService.getAll())
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ students, parents, fees, academicYears, stages, grades, sections }) => {
          const ok = <T>(r: { status: number; data: T[] } | null) => (r && r.status === 200 && r.data ? r.data : []);

          this.parentMap.set(new Map(ok(parents).map(p => [p.id, p])));
          this.academicYearMap.set(new Map(ok(academicYears).map(a => [a.id, a])));
          this.stageMap.set(new Map(ok(stages).map(s => [s.id, s])));
          this.gradeMap.set(new Map(ok(grades).map(g => [g.id, g])));
          this.sectionMap.set(new Map(ok(sections).map(s => [s.id, s])));

          if (fees?.status === 200) {
            const byStudent = new Map<string, StudentFee[]>();
            for (const fee of fees.data || []) {
              const list = byStudent.get(fee.studentId) ?? [];
              list.push(fee);
              byStudent.set(fee.studentId, list);
            }
            this.feesByStudent.set(byStudent);
          } else {
            this.feesUnavailable.set(true);
          }

          if (students.status === 200) {
            this.students.set(this.sortByName(students.data || []));
            this.clampPage();
          } else {
            this.errorMessage.set(students.message || this.translate.instant('students.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading students:', error);
          this.errorMessage.set(this.translate.instant('students.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Filters & navigation
  // ---------------------------------------------------------------------------

  setFilter(filter: 'search' | 'stage' | 'grade' | 'section' | 'feeState', event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLSelectElement).value;
    switch (filter) {
      case 'search':
        this.searchTerm.set(value);
        break;
      case 'stage':
        this.stageFilter.set(value);
        if (this.gradeFilter() && !this.filterGrades().some(g => g.id === this.gradeFilter())) {
          this.gradeFilter.set('');
          this.sectionFilter.set('');
        }
        break;
      case 'grade':
        this.gradeFilter.set(value);
        this.sectionFilter.set('');
        break;
      case 'section':
        this.sectionFilter.set(value);
        break;
      case 'feeState':
        this.feeStateFilter.set(value as StudentFeeState | '');
        break;
    }
    this.page.set(1);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.stageFilter.set('');
    this.gradeFilter.set('');
    this.sectionFilter.set('');
    this.feeStateFilter.set('');
    this.page.set(1);
  }

  private clampPage(): void {
    const lastPage = Math.max(1, Math.ceil(this.filteredStudents().length / this.pageSize));
    if (this.page() > lastPage) this.page.set(lastPage);
  }

  rowNumber(index: number): number {
    return (this.page() - 1) * this.pageSize + index + 1;
  }

  viewProfile(student: Student): void {
    this.router.navigate(['/management/students', student.id]);
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  studentName(s: Student): string {
    return this.currentLanguage() === 'ar' ? (s.fullNameAr || s.fullNameEn) : (s.fullNameEn || s.fullNameAr);
  }

  /** Name in the other language, shown under the main name */
  studentAltName(s: Student): string {
    const alt = this.currentLanguage() === 'ar' ? s.fullNameEn : s.fullNameAr;
    return alt && alt !== this.studentName(s) ? alt : '';
  }

  initials(s: Student): string {
    const name = (s.fullNameEn || s.fullNameAr || '').trim();
    const parts = name.split(/\s+/).filter(Boolean);
    return ((parts[0]?.[0] ?? '') + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase() || '?';
  }

  parentName(s: Student): string {
    const p = this.parentMap().get(s.parentId);
    if (!p) return '-';
    return this.currentLanguage() === 'ar' ? (p.fullNameAr || p.fullNameEn) : (p.fullNameEn || p.fullNameAr);
  }

  private localized(item: { nameAr: string; nameEn: string } | undefined): string {
    if (!item) return '-';
    return this.currentLanguage() === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr;
  }

  /** Student DTO carries stageId; fall back to the grade's stage for older records */
  private stageIdOf(s: Student): string {
    return s.stageId || this.gradeMap().get(s.gradeId)?.stageId || '';
  }

  getStageName(s: Student): string {
    return this.localized(this.stageMap().get(this.stageIdOf(s)));
  }

  getGradeName(id: string): string {
    return this.localized(this.gradeMap().get(id));
  }

  getSectionName(id: string): string {
    return this.localized(this.sectionMap().get(id));
  }

  getAcademicYearName(id: string): string {
    return this.academicYearMap().get(id)?.name || '-';
  }

  getName(item: { nameAr: string; nameEn: string }): string {
    return this.localized(item);
  }

  feeOverviewOf(s: Student): StudentFeeOverview {
    return this.feeOverview().get(s.id) ?? { state: 'noFees', balances: [] };
  }

  formatAmount(amount: number, currency?: string): string {
    const formatted = (amount ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    return currency ? `${formatted} ${currency}` : formatted;
  }

  private buildOverview(fees: StudentFee[]): StudentFeeOverview {
    if (fees.length === 0) return { state: 'noFees', balances: [] };

    const balances = new Map<string, number>();
    let overdue = false;
    for (const fee of fees) {
      if (fee.balance > 0) {
        balances.set(fee.currency, (balances.get(fee.currency) ?? 0) + fee.balance);
      }
      if (effectiveFeeStatus(fee) === FeeStatus.Overdue) overdue = true;
    }

    const state: StudentFeeState = overdue ? 'overdue' : balances.size > 0 ? 'outstanding' : 'paid';
    return { state, balances: [...balances].map(([currency, amount]) => ({ currency, amount })) };
  }

  private sortByName(list: Student[]): Student[] {
    return [...list].sort((a, b) =>
      (a.fullNameEn || a.fullNameAr || '').localeCompare(b.fullNameEn || b.fullNameAr || '')
    );
  }
}
