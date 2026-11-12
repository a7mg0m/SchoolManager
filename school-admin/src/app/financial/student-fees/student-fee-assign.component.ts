import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { StudentFeeService } from '../../core/services/student-fee.service';
import { StudentService } from '../../core/services/student.service';
import { FeeService } from '../../core/services/fee.service';
import { GradeService } from '../../core/services/grade.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Student } from '../../core/models/student.model';
import { Fee, FEE_TYPES } from '../../core/models/fee.model';
import { StudentFee } from '../../core/models/student-fee.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';

@Component({
  selector: 'app-student-fee-assign',
  standalone: false,
  styleUrl: './student-fee-assign.component.scss',
  templateUrl: './student-fee-assign.component.html',
})
export class StudentFeeAssignComponent implements OnInit, OnDestroy {
  // Lookups
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  students = signal<Student[]>([]);
  fees = signal<Fee[]>([]);
  existingStudentFees = signal<StudentFee[]>([]);
  currentLanguage = signal<string>('en');

  // State
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // Selections
  stageId = signal('');
  gradeId = signal('');
  studentSearch = signal('');
  selectedStudentIds = signal<Set<string>>(new Set());
  selectedFeeIds = signal<Set<string>>(new Set());
  discount = signal<number>(0);
  dueDate = signal<string>('');
  submitted = signal(false);

  readonly feeTypes = FEE_TYPES;

  stageGrades = computed(() =>
    this.stageId() ? this.grades().filter(g => g.stageId === this.stageId() && g.isActive) : []
  );

  /** Active students of the selected grade */
  gradeStudents = computed(() =>
    this.students()
      .filter(s => s.gradeId === this.gradeId() && s.isActive !== false)
      .sort((a, b) => this.studentName(a).localeCompare(this.studentName(b)))
  );

  visibleStudents = computed(() => {
    const term = this.studentSearch().trim().toLowerCase();
    if (!term) return this.gradeStudents();
    return this.gradeStudents().filter(s =>
      `${s.fullNameAr} ${s.fullNameEn} ${s.nationalId}`.toLowerCase().includes(term)
    );
  });

  /** Fees defined for the selected grade, mandatory first */
  gradeFees = computed(() =>
    this.fees()
      .filter(f => f.gradeId === this.gradeId())
      .sort((a, b) => Number(b.isMandatory) - Number(a.isMandatory) || a.name.localeCompare(b.name))
  );

  selectedFees = computed(() => this.gradeFees().filter(f => this.selectedFeeIds().has(f.id)));

  /** Currencies of the selected fees – the API requires exactly one */
  selectedCurrencies = computed(() => [...new Set(this.selectedFees().map(f => f.currency.toUpperCase()))]);
  currency = computed(() => this.selectedCurrencies()[0] ?? '');
  hasCurrencyMismatch = computed(() => this.selectedCurrencies().length > 1);

  totalPerStudent = computed(() => this.selectedFees().reduce((sum, f) => sum + f.amount, 0));
  netPerStudent = computed(() => Math.max(this.totalPerStudent() - (this.discount() || 0), 0));
  grandTotal = computed(() => this.netPerStudent() * this.selectedStudentIds().size);

  /** How many student-fee records each student already has (to warn about duplicates) */
  existingCountByStudent = computed(() => {
    const map = new Map<string, number>();
    for (const sf of this.existingStudentFees()) {
      map.set(sf.studentId, (map.get(sf.studentId) || 0) + 1);
    }
    return map;
  });

  allVisibleSelected = computed(() => {
    const visible = this.visibleStudents();
    return visible.length > 0 && visible.every(s => this.selectedStudentIds().has(s.id));
  });

  // Validation
  discountError = computed<string | null>(() => {
    const d = this.discount();
    if (d === null || d === undefined || isNaN(d)) return null;
    if (d < 0) return 'studentFees.assign.discountNegative';
    if (d > this.totalPerStudent() && this.totalPerStudent() > 0) return 'studentFees.assign.discountTooHigh';
    return null;
  });

  canSubmit = computed(() =>
    !!this.gradeId() &&
    this.selectedStudentIds().size > 0 &&
    this.selectedFees().length > 0 &&
    !this.hasCurrencyMismatch() &&
    this.totalPerStudent() > 0 &&
    !this.discountError() &&
    !!this.dueDate() &&
    !this.isSaving()
  );

  private destroy$ = new Subject<void>();
  /** Student pre-selected from the query string (?studentId=...) */
  private presetStudentId: string | null = null;

  constructor(
    private studentFeeService: StudentFeeService,
    private studentService: StudentService,
    private feeService: FeeService,
    private gradeService: GradeService,
    private languageService: LanguageService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    this.presetStudentId = this.activatedRoute.snapshot.queryParamMap.get('studentId');
    this.dueDate.set(this.defaultDueDate());
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
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getAllGrades(),
      students: this.studentService.getAll(),
      fees: this.feeService.getAll(),
      studentFees: this.studentFeeService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.stages.set(res.stages.data || []);
          this.grades.set(res.grades.data || []);
          this.students.set(res.students.data || []);
          this.fees.set(res.fees.data || []);
          this.existingStudentFees.set(res.studentFees.data || []);
          this.applyPresetStudent();
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading assign data:', error);
          this.errorMessage.set('Failed to load students and fees. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  /** Coming from a student's fee page: select their stage, grade and the student */
  private applyPresetStudent(): void {
    const student = this.presetStudentId ? this.students().find(s => s.id === this.presetStudentId) : undefined;
    if (!student) return;
    const grade = this.grades().find(g => g.id === student.gradeId);
    this.stageId.set(student.stageId || grade?.stageId || '');
    this.selectGrade(student.gradeId);
    this.selectedStudentIds.set(new Set([student.id]));
  }

  // ---------------------------------------------------------------------------
  // Selection handlers
  // ---------------------------------------------------------------------------

  onStageChange(event: Event): void {
    this.stageId.set((event.target as HTMLSelectElement).value);
    this.selectGrade('');
  }

  onGradeChange(event: Event): void {
    this.selectGrade((event.target as HTMLSelectElement).value);
  }

  /** Changing grade resets students and pre-selects the grade's mandatory fees */
  private selectGrade(gradeId: string): void {
    this.gradeId.set(gradeId);
    this.studentSearch.set('');
    this.selectedStudentIds.set(new Set());
    this.selectedFeeIds.set(new Set(this.gradeFees().filter(f => f.isMandatory).map(f => f.id)));
    this.discount.set(0);
  }

  onStudentSearch(event: Event): void {
    this.studentSearch.set((event.target as HTMLInputElement).value);
  }

  toggleStudent(id: string): void {
    this.selectedStudentIds.update(set => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  toggleAllVisible(): void {
    const visibleIds = this.visibleStudents().map(s => s.id);
    const selectAll = !this.allVisibleSelected();
    this.selectedStudentIds.update(set => {
      const next = new Set(set);
      visibleIds.forEach(id => (selectAll ? next.add(id) : next.delete(id)));
      return next;
    });
  }

  toggleFee(id: string): void {
    this.selectedFeeIds.update(set => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  onDiscountChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.discount.set(value === '' ? 0 : Number(value));
  }

  onDueDateChange(event: Event): void {
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

    this.studentFeeService
      .assign({
        studentIds: [...this.selectedStudentIds()],
        feeIds: this.selectedFees().map(f => f.id),
        discountAmount: this.discount() || 0,
        dueDate: `${this.dueDate()}T00:00:00`
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.successMessage.set(response.message || 'Fees assigned successfully');
            this.isSaving.set(false);
            setTimeout(() => this.navigateBack(), 1000);
          } else {
            // Domain validation errors come back as status 400 with a message
            this.errorMessage.set(response.message || 'Failed to assign fees');
            this.isSaving.set(false);
          }
        },
        error: (error) => {
          console.error('Error assigning fees:', error);
          this.errorMessage.set('Failed to assign fees. Please try again.');
          this.isSaving.set(false);
        }
      });
  }

  navigateBack(): void {
    if (this.presetStudentId) {
      this.router.navigate(['/financial/student-fees/student', this.presetStudentId]);
    } else {
      this.router.navigate(['/financial/student-fees']);
    }
  }

  navigateToFees(): void {
    this.router.navigate(['/financial/fees/save'], { queryParams: { gradeId: this.gradeId() } });
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  getName(item: { nameAr?: string; nameEn?: string }): string {
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  studentName(s: Student): string {
    return this.currentLanguage() === 'ar' ? (s.fullNameAr || s.fullNameEn) : (s.fullNameEn || s.fullNameAr);
  }

  typeKey(fee: Fee): string {
    return this.feeTypes.find(t => t.value === fee.type)?.key ?? 'fees.types.other';
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
}
