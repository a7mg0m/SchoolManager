import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { ClinicVisitService } from '../../core/services/clinic-visit.service';
import { StudentService } from '../../core/services/student.service';
import { ClinicVisit, VISIT_OUTCOMES } from '../../core/models/clinic-visit.model';
import { Student } from '../../core/models/student.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, forkJoin, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-clinic-visit-list',
  standalone: false,
  styleUrl: './clinic-visit-list.component.scss',
  templateUrl: './clinic-visit-list.component.html',
})
export class ClinicVisitListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  visits = signal<ClinicVisit[]>([]);
  students = signal<Student[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);
  currentLanguage = signal<string>('en');
  searchTerm = signal('');
  outcomeFilter = signal('');

  readonly outcomes = VISIT_OUTCOMES;

  /** Newest first, filtered by student name / complaint / diagnosis and outcome */
  filtered = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const outcome = this.outcomeFilter();
    return this.visits()
      .filter(v => !outcome || v.outcome === Number(outcome))
      .filter(v => !term || `${this.studentName(v.studentId)} ${v.doctorName} ${v.complaint} ${v.diagnosis || ''}`.toLowerCase().includes(term))
      .sort((a, b) => b.visitDate.localeCompare(a.visitDate));
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private visitToDelete: ClinicVisit | null = null;

  constructor(
    private visitService: ClinicVisitService,
    private studentService: StudentService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe(language => this.currentLanguage.set(language));
    this.loadVisits();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all visits, with the students for their names
   */
  loadVisits(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      visits: this.visitService.getAll(),
      students: this.studentService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ visits, students }) => {
          this.students.set(students.data || []);
          if (visits.status === 200 && visits.data) {
            this.visits.set(visits.data);
          } else {
            this.errorMessage.set(visits.message || this.translate.instant('clinic.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading clinic visits:', error);
          this.errorMessage.set(this.translate.instant('clinic.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  setSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  setOutcome(event: Event): void {
    this.outcomeFilter.set((event.target as HTMLSelectElement).value);
  }

  navigateToCreate(): void {
    this.router.navigate(['/clinic/visits/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/clinic/visits/save', id]);
  }

  navigateToStudent(studentId: string): void {
    this.router.navigate(['/clinic/students', studentId]);
  }

  openDeleteConfirmation(visit: ClinicVisit, content: any): void {
    this.visitToDelete = visit;
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  confirmDelete(): void {
    if (!this.visitToDelete) {
      return;
    }

    this.isDeleting.set(true);
    const id = this.visitToDelete.id;

    this.visitService
      .delete(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(this.translate.instant('clinic.deleted'));
            this.deleteModalRef?.close();
            this.visitToDelete = null;
            this.visits.update(list => list.filter(v => v.id !== id));

            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.successMessage.set(null);
            this.errorMessage.set(response.message || this.translate.instant('clinic.deleteFailed'));
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting clinic visit:', error);
          this.successMessage.set(null);
          this.errorMessage.set(this.translate.instant('clinic.deleteFailed'));
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.visitToDelete = null;
  }

  studentName(id: string): string {
    const student = this.students().find(s => s.id === id);
    if (!student) return '-';
    return this.currentLanguage() === 'ar' ? student.fullNameAr : student.fullNameEn;
  }

  outcomeInfo(visit: ClinicVisit) {
    return this.outcomes.find(o => o.value === visit.outcome) ?? this.outcomes[0];
  }

  formatDateTime(value: string): string {
    const date = new Date(value);
    return isNaN(date.getTime())
      ? value
      : date.toLocaleString(this.currentLanguage() === 'ar' ? 'ar-EG' : 'en-US', { dateStyle: 'medium', timeStyle: 'short' });
  }
}
