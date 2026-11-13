import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { Subject, timer, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ExamService } from '../../core/services/exam.service';
import { GradeService } from '../../core/services/grade.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { SectionService } from '../../core/services/section.service';
import { SubjectService } from '../../core/services/subject.service';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Exam, ExamType, EXAM_TYPES } from '../../core/models/exam.model';
import { Grade } from '../../core/models/grade.model';
import { Stage } from '../../core/models/stage.model';

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';
import { AcademicYear, AcademicYearSemester } from '../../core/models/academic-year.model';
import { Section } from '../../core/models/section.model';
import { Subject as SchoolSubject } from '../../core/models/subject.model';

@Component({
  selector: 'app-exam-list',
  standalone: false,
  styleUrl: './exam-list.component.scss',
  templateUrl: './exam-list.component.html',
})
export class ExamListComponent implements OnInit, OnDestroy {
  exams = signal<Exam[]>([]);
  isLoading = signal(false);
  isDeleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  currentLanguage = signal<string>('en');

  // Filters
  filterStageId = signal<string>('');
  filterGradeId = signal<string>('');
  filterExamType = signal<string>('');
  filterAcademicYearId = signal<string>('');

  // Lookups
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  private stageMap = new Map<string, Stage>();

  /** Grades for the grade filter, narrowed by the stage filter */
  filterGrades = computed(() => {
    const stageId = this.filterStageId();
    return stageId ? this.grades().filter(g => g.stageId === stageId) : this.grades();
  });
  academicYears = signal<AcademicYear[]>([]);
  private gradeMap = new Map<string, Grade>();
  private sectionMap = new Map<string, Section>();
  private subjectMap = new Map<string, SchoolSubject>();
  private academicYearMap = new Map<string, AcademicYear>();
  private semesterMap = new Map<string, AcademicYearSemester>();

  readonly examTypes = EXAM_TYPES;

  /** Exams after applying filters, newest date first */
  filteredExams = computed(() => {
    const stageId = this.filterStageId();
    const gradeId = this.filterGradeId();
    const examType = this.filterExamType();
    const yearId = this.filterAcademicYearId();
    return this.exams()
      .filter(e => !stageId || this.examStageId(e) === stageId)
      .filter(e => !gradeId || e.gradeId === gradeId)
      .filter(e => !examType || e.examType === Number(examType))
      .filter(e => !yearId || e.academicYearId === yearId)
      .sort((a, b) =>
        (b.examDate || '').localeCompare(a.examDate || '') ||
        (a.startTime || '').localeCompare(b.startTime || ''));
  });

  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private examToDelete: Exam | null = null;

  constructor(
    private examService: ExamService,
    private gradeService: GradeService,
    private academicYearService: AcademicYearService,
    private sectionService: SectionService,
    private subjectService: SubjectService,
    private languageService: LanguageService,
    private router: Router,
    private modalService: NgbModal
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

    forkJoin({
      exams: this.examService.getAll(),
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getAllGrades(),
      academicYears: this.academicYearService.getAll(),
      sections: this.sectionService.getAll(),
      subjects: this.subjectService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.stages.set(res.stages.data || []);
          this.stageMap = this.toMap(res.stages.data);
          this.grades.set(res.grades.data || []);
          this.academicYears.set(res.academicYears.data || []);
          this.gradeMap = this.toMap(res.grades.data);
          this.sectionMap = this.toMap(res.sections.data);
          this.subjectMap = this.toMap(res.subjects.data);
          this.academicYearMap = this.toMap(res.academicYears.data);
          this.semesterMap = new Map(
            (res.academicYears.data || [])
              .flatMap(y => y.semesters || [])
              .filter(s => !!s.id)
              .map(s => [s.id!, s])
          );

          if (res.exams.status === 200) {
            this.exams.set(res.exams.data || []);
          } else {
            this.errorMessage.set(res.exams.message || 'Failed to load exams');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading exams:', error);
          this.errorMessage.set('Failed to load exams. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  private toMap<T extends { id: string }>(items: T[] | null | undefined): Map<string, T> {
    return new Map((items || []).map(i => [i.id, i]));
  }

  onStageFilterChange(stageId: string): void {
    this.filterStageId.set(stageId);
    // Drop a grade filter that doesn't belong to the chosen stage
    if (this.filterGradeId() && !this.filterGrades().some(g => g.id === this.filterGradeId())) {
      this.filterGradeId.set('');
    }
  }

  clearFilters(): void {
    this.filterStageId.set('');
    this.filterGradeId.set('');
    this.filterExamType.set('');
    this.filterAcademicYearId.set('');
  }

  // ---------------------------------------------------------------------------
  // Navigation
  // ---------------------------------------------------------------------------

  navigateToCreate(): void {
    this.router.navigate(['/exam/exams/save']);
  }

  navigateToEdit(id: string): void {
    this.router.navigate(['/exam/exams/save', id]);
  }

  navigateToResults(id: string): void {
    this.router.navigate(['/exam/exams', id, 'results']);
  }

  // ---------------------------------------------------------------------------
  // Delete
  // ---------------------------------------------------------------------------

  openDeleteConfirmation(exam: Exam, content: any): void {
    this.examToDelete = exam;
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  confirmDelete(): void {
    if (!this.examToDelete) return;

    this.isDeleting.set(true);

    this.examService
      .delete(this.examToDelete.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(response.message || 'Exam deleted successfully');
            this.deleteModalRef?.close();
            this.examToDelete = null;
            this.loadData();

            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.errorMessage.set(response.message || 'Failed to delete exam');
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting exam:', error);
          this.errorMessage.set('Failed to delete exam. Please try again.');
          this.isDeleting.set(false);
        }
      });
  }

  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.examToDelete = null;
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  getName(item: { nameAr?: string; nameEn?: string } | undefined | null): string {
    if (!item) return '—';
    return this.currentLanguage() === 'ar' ? (item.nameAr || item.nameEn) : (item.nameEn || item.nameAr);
  }

  /** Exams saved before stage existed have an empty stageId: derive it from the grade */
  examStageId(exam: Exam): string {
    return (exam.stageId && exam.stageId !== EMPTY_GUID)
      ? exam.stageId
      : this.gradeMap.get(exam.gradeId)?.stageId ?? '';
  }

  stageName(exam: Exam): string { return this.getName(this.stageMap.get(this.examStageId(exam))); }
  gradeName(id: string): string { return this.getName(this.gradeMap.get(id)); }
  sectionName(id: string | null): string { return id ? this.getName(this.sectionMap.get(id)) : ''; }
  subjectName(id: string): string { return this.getName(this.subjectMap.get(id)); }
  academicYearName(id: string): string { return this.academicYearMap.get(id)?.name ?? '—'; }
  semesterName(id: string): string { return this.semesterMap.get(id)?.name ?? '—'; }

  examTypeKey(type: ExamType): string {
    return this.examTypes.find(t => t.value === type)?.key ?? '—';
  }

  examTypeBadge(type: ExamType): string {
    return this.examTypes.find(t => t.value === type)?.badge ?? 'bg-secondary';
  }

  formatTime(value: string): string {
    return value ? value.substring(0, 5) : '';
  }
}
