import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { Subject, timer, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { StudentFeeService } from '../../core/services/student-fee.service';
import { StudentService } from '../../core/services/student.service';
import { GradeService } from '../../core/services/grade.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import {
  StudentFee,
  FeeStatus,
  FEE_STATUSES,
  effectiveFeeStatus,
  isFeePastDue,
  feeStatusInfo,
  canWaiveFee,
  canRestoreWaiver,
  MAX_WAIVE_REASON_LENGTH
} from '../../core/models/student-fee.model';
import { TranslateService } from '@ngx-translate/core';
import { Student } from '../../core/models/student.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';

interface CurrencySummary {
  currency: string;
  net: number;
  paid: number;
  balance: number;
  waived: number;
}

@Component({
  selector: 'app-student-fee-list',
  standalone: false,
  styleUrl: './student-fee-list.component.scss',
  templateUrl: './student-fee-list.component.html',
})
export class StudentFeeListComponent implements OnInit, OnDestroy {
  studentFees = signal<StudentFee[]>([]);
  students = signal<Student[]>([]);
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  currentLanguage = signal<string>('en');
  isLoading = signal(false);
  isPaying = signal(false);
  isDeleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  /** Set when viewing a single student's fees (/financial/student-fees/student/:studentId) */
  studentId = signal<string | null>(null);

  feeToPay = signal<StudentFee | null>(null);
  feeToDelete = signal<StudentFee | null>(null);
  paymentForm!: FormGroup;

  // Waive / restore
  feeToWaive = signal<StudentFee | null>(null);
  feeToRestore = signal<StudentFee | null>(null);
  isWaiving = signal(false);
  isRestoring = signal(false);
  waiveForm!: FormGroup;
  readonly maxWaiveReasonLength = MAX_WAIVE_REASON_LENGTH;

  /** Any modal action in flight – disables the row buttons */
  isBusy = computed(() => this.isPaying() || this.isDeleting() || this.isWaiving() || this.isRestoring());

  // Filters
  searchTerm = signal('');
  stageFilter = signal('');
  gradeFilter = signal('');
  statusFilter = signal('');
  outstandingOnly = signal(false);

  readonly statuses = FEE_STATUSES;

  private studentById = computed(() => new Map(this.students().map(s => [s.id, s])));

  filterGrades = computed(() => {
    const stageId = this.stageFilter();
    return stageId ? this.grades().filter(g => g.stageId === stageId) : this.grades();
  });

  filteredFees = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const stageId = this.stageFilter();
    const gradeId = this.gradeFilter();
    const status = this.statusFilter();
    const outstanding = this.outstandingOnly();
    const students = this.studentById();

    return this.studentFees()
      .filter(f => {
        const student = students.get(f.studentId);
        if (stageId && student?.stageId !== stageId) return false;
        if (gradeId && student?.gradeId !== gradeId) return false;
        if (status !== '' && this.effectiveStatus(f) !== Number(status)) return false;
        if (outstanding && !(f.balance > 0)) return false;
        if (term) {
          const name = `${student?.fullNameAr || ''} ${student?.fullNameEn || ''} ${student?.nationalId || ''}`.toLowerCase();
          if (!name.includes(term)) return false;
        }
        return true;
      })
      .sort((a, b) => a.dueDate.localeCompare(b.dueDate));
  });

  /** Net / paid / balance totals of the filtered rows, per currency */
  summary = computed<CurrencySummary[]>(() => {
    const map = new Map<string, CurrencySummary>();
    for (const f of this.filteredFees()) {
      const s = map.get(f.currency) ?? { currency: f.currency, net: 0, paid: 0, balance: 0, waived: 0 };
      s.net += f.netAmount || 0;
      s.paid += f.paidAmount || 0;
      s.balance += f.balance || 0;
      s.waived += f.waivedAmount || 0;
      map.set(f.currency, s);
    }
    return [...map.values()];
  });

  overdueCount = computed(() =>
    this.filteredFees().filter(f => this.effectiveStatus(f) === FeeStatus.Overdue).length
  );

  hasActiveFilters = computed(() =>
    !!(this.searchTerm() || this.stageFilter() || this.gradeFilter() || this.statusFilter() !== '' || this.outstandingOnly())
  );

  private destroy$ = new Subject<void>();
  private modalRef: NgbModalRef | null = null;

  constructor(
    private studentFeeService: StudentFeeService,
    private studentService: StudentService,
    private gradeService: GradeService,
    private languageService: LanguageService,
    private formBuilder: FormBuilder,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));

    // Same component serves the full list and a single student's view
    this.activatedRoute.paramMap
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        this.studentId.set(params.get('studentId'));
        this.loadData();
      });
  }

  ngOnDestroy(): void {
    this.modalRef?.close();
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const studentId = this.studentId();
    const fees$ = studentId
      ? this.studentFeeService.getByStudent(studentId)
      : this.studentFeeService.getAll();

    forkJoin({
      fees: fees$,
      students: this.studentService.getAll(),
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getAllGrades()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ fees, students, stages, grades }) => {
          this.students.set(students.data || []);
          this.stages.set(stages.data || []);
          this.grades.set(grades.data || []);
          if (fees.status === 200) {
            this.studentFees.set(fees.data || []);
          } else {
            this.errorMessage.set(fees.message || 'Failed to load student fees');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading student fees:', error);
          this.errorMessage.set('Failed to load student fees. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Filters & navigation
  // ---------------------------------------------------------------------------

  setFilter(filter: 'search' | 'stage' | 'grade' | 'status', event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLSelectElement).value;
    switch (filter) {
      case 'search': this.searchTerm.set(value); break;
      case 'stage':
        this.stageFilter.set(value);
        if (this.gradeFilter() && !this.filterGrades().some(g => g.id === this.gradeFilter())) {
          this.gradeFilter.set('');
        }
        break;
      case 'grade': this.gradeFilter.set(value); break;
      case 'status': this.statusFilter.set(value); break;
    }
  }

  toggleOutstanding(event: Event): void {
    this.outstandingOnly.set((event.target as HTMLInputElement).checked);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.stageFilter.set('');
    this.gradeFilter.set('');
    this.statusFilter.set('');
    this.outstandingOnly.set(false);
  }

  viewStudent(studentId: string): void {
    this.router.navigate(['/financial/student-fees/student', studentId]);
  }

  /** Opens the assign page; in a student's view the student is pre-selected */
  navigateToAssign(): void {
    const studentId = this.studentId();
    this.router.navigate(['/financial/student-fees/assign'], studentId ? { queryParams: { studentId } } : {});
  }

  backToAll(): void {
    this.router.navigate(['/financial/student-fees']);
  }

  // ---------------------------------------------------------------------------
  // Apply payment
  // ---------------------------------------------------------------------------

  openPayment(fee: StudentFee, content: any): void {
    this.feeToPay.set(fee);
    const max = Math.max(fee.balance || 0, 0);
    this.paymentForm = this.formBuilder.group({
      amount: [max || null, [Validators.required, Validators.min(0.01), this.maxAmount(max)]]
    });
    this.modalRef = this.modalService.open(content, { centered: true, backdrop: 'static' });
  }

  /** The API doesn't stop overpayment, so the UI caps the amount at the balance */
  private maxAmount(max: number) {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = Number(control.value);
      return value > max + 0.0001 ? { exceedsBalance: true } : null;
    };
  }

  payFullBalance(): void {
    const fee = this.feeToPay();
    if (fee) this.paymentForm.patchValue({ amount: fee.balance });
  }

  confirmPayment(): void {
    const fee = this.feeToPay();
    if (!fee || this.paymentForm.invalid) {
      this.paymentForm?.markAllAsTouched();
      return;
    }

    this.isPaying.set(true);
    const amount = Number(this.paymentForm.value.amount);

    this.studentFeeService
      .applyPayment(fee.id, { amount, currency: fee.currency })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.modalRef?.close();
            this.feeToPay.set(null);
            this.showSuccess(response.message || 'Payment applied successfully');
            this.refreshFee(fee.id);
          } else {
            this.errorMessage.set(response.message || 'Failed to apply payment');
          }
          this.isPaying.set(false);
        },
        error: (error) => {
          console.error('Error applying payment:', error);
          this.errorMessage.set('Failed to apply payment. Please try again.');
          this.isPaying.set(false);
        }
      });
  }

  /** Re-fetch one row after a payment instead of reloading everything */
  private refreshFee(id: string): void {
    this.studentFeeService
      .getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            const updated = response.data;
            this.studentFees.update(list => list.map(f => (f.id === id ? updated : f)));
          } else {
            this.loadData();
          }
        },
        error: () => this.loadData()
      });
  }

  // ---------------------------------------------------------------------------
  // Waive / restore waiver
  // ---------------------------------------------------------------------------

  isWaived(fee: StudentFee): boolean {
    return fee.status === FeeStatus.Waived;
  }

  canWaive(fee: StudentFee): boolean {
    return canWaiveFee(fee);
  }

  canRestore(fee: StudentFee): boolean {
    return canRestoreWaiver(fee);
  }

  openWaive(fee: StudentFee, content: any): void {
    this.feeToWaive.set(fee);
    this.waiveForm = this.formBuilder.group({
      reason: ['', [Validators.required, Validators.maxLength(MAX_WAIVE_REASON_LENGTH), this.notBlank]]
    });
    this.modalRef = this.modalService.open(content, { centered: true, backdrop: 'static' });
  }

  private notBlank(control: AbstractControl): ValidationErrors | null {
    return typeof control.value === 'string' && control.value.length > 0 && !control.value.trim()
      ? { required: true }
      : null;
  }

  confirmWaive(): void {
    const fee = this.feeToWaive();
    if (!fee || this.waiveForm.invalid) {
      this.waiveForm?.markAllAsTouched();
      return;
    }

    this.isWaiving.set(true);
    const reason = String(this.waiveForm.value.reason).trim();

    this.studentFeeService
      .waive(fee.id, { reason })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.modalRef?.close();
            this.feeToWaive.set(null);
            this.showSuccess(this.translate.instant('studentFees.waive.success'));
            this.refreshFee(fee.id);
          } else {
            this.errorMessage.set(response.message || this.translate.instant('studentFees.waive.failed'));
          }
          this.isWaiving.set(false);
        },
        error: (error) => {
          console.error('Error waiving student fee:', error);
          this.errorMessage.set(this.translate.instant('studentFees.waive.failed'));
          this.isWaiving.set(false);
        }
      });
  }

  openRestore(fee: StudentFee, content: any): void {
    this.feeToRestore.set(fee);
    this.modalRef = this.modalService.open(content, { centered: true, backdrop: 'static' });
  }

  confirmRestore(): void {
    const fee = this.feeToRestore();
    if (!fee) return;

    this.isRestoring.set(true);

    this.studentFeeService
      .restoreWaiver(fee.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.modalRef?.close();
            this.feeToRestore.set(null);
            this.showSuccess(this.translate.instant('studentFees.waive.restored'));
            this.refreshFee(fee.id);
          } else {
            this.errorMessage.set(response.message || this.translate.instant('studentFees.waive.restoreFailed'));
          }
          this.isRestoring.set(false);
        },
        error: (error) => {
          console.error('Error restoring student fee waiver:', error);
          this.errorMessage.set(this.translate.instant('studentFees.waive.restoreFailed'));
          this.isRestoring.set(false);
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Delete
  // ---------------------------------------------------------------------------

  openDeleteConfirmation(fee: StudentFee, content: any): void {
    this.feeToDelete.set(fee);
    this.modalRef = this.modalService.open(content, { centered: true, backdrop: 'static', keyboard: false });
  }

  confirmDelete(): void {
    const fee = this.feeToDelete();
    if (!fee) return;

    this.isDeleting.set(true);

    this.studentFeeService
      .delete(fee.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.modalRef?.close();
            this.feeToDelete.set(null);
            this.studentFees.update(list => list.filter(f => f.id !== fee.id));
            this.showSuccess(response.message || 'Student fee deleted successfully');
          } else {
            this.errorMessage.set(response.message || 'Failed to delete student fee');
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting student fee:', error);
          this.errorMessage.set('Failed to delete student fee. It may have payments recorded against it.');
          this.modalRef?.close();
          this.feeToDelete.set(null);
          this.isDeleting.set(false);
        }
      });
  }

  closeModal(): void {
    this.modalRef?.close();
    this.feeToPay.set(null);
    this.feeToDelete.set(null);
    this.feeToWaive.set(null);
    this.feeToRestore.set(null);
  }

  private showSuccess(message: string): void {
    this.errorMessage.set(null);
    this.successMessage.set(message);
    timer(4000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.successMessage.set(null));
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  effectiveStatus(fee: StudentFee): FeeStatus {
    return effectiveFeeStatus(fee);
  }

  isPastDue(fee: StudentFee): boolean {
    return isFeePastDue(fee);
  }

  statusInfo(fee: StudentFee) {
    return feeStatusInfo(fee);
  }

  paidPercent(fee: StudentFee): number {
    if (!fee.netAmount || fee.netAmount <= 0) return fee.balance > 0 ? 0 : 100;
    return Math.min(100, Math.round((fee.paidAmount / fee.netAmount) * 100));
  }

  studentName(studentId: string): string {
    const s = this.studentById().get(studentId);
    if (!s) return '—';
    return this.currentLanguage() === 'ar' ? (s.fullNameAr || s.fullNameEn) : (s.fullNameEn || s.fullNameAr);
  }

  studentGrade(studentId: string): string {
    const s = this.studentById().get(studentId);
    const grade = s ? this.grades().find(g => g.id === s.gradeId) : undefined;
    return grade ? this.getName(grade) : '';
  }

  getName(item: { nameAr?: string; nameEn?: string }): string {
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  formatDate(value: string): string {
    const date = (value || '').substring(0, 10);
    if (!date) return '';
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
