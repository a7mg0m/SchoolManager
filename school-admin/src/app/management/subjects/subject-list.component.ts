import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { SubjectService } from '../../core/services/subject.service';
import { GradeService } from '../../core/services/grade.service';
import { StageService } from '../../core/services/stage.service';
import { TeacherService } from '../../core/services/teacher.service';
import { Subject as SubjectModel } from '../../core/models/subject.model';
import { Grade } from '../../core/models/grade.model';
import { Stage } from '../../core/models/stage.model';
import { Teacher } from '../../core/models/teacher.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, timer, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-subject-list',
  standalone: false,
  styleUrl: './subject-list.component.scss',
  templateUrl: './subject-list.component.html',
})
export class SubjectListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  subjects = signal<SubjectModel[]>([]);
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  teachers = signal<Teacher[]>([]);
  searchTerm = signal<string>('');
  selectedStageId = signal<string>('');
  selectedGradeId = signal<string>('');
  page = signal(1);
  readonly pageSize = 10;
  currentLanguage = signal<string>('en');
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);
  viewedSubject = signal<SubjectModel | null>(null);

  /** Stage lookup by ID */
  private stageMap = computed(() => new Map(this.stages().map(s => [s.id, s])));
  /** Grade lookup by ID */
  private gradeMap = computed(() => new Map(this.grades().map(g => [g.id, g])));
  /** Teacher lookup by ID */
  private teacherMap = computed(() => new Map(this.teachers().map(t => [t.id, t])));

  /**
   * Grades that are actually used by at least one subject
   */
  private usedGrades = computed(() => {
    const used = new Set(this.subjects().map(s => s.gradeId));
    return this.grades().filter(g => used.has(g.id));
  });

  /**
   * Stages that contain at least one subject's grade (for the stage filter)
   */
  filterStages = computed(() => {
    const used = new Set(this.usedGrades().map(g => g.stageId));
    return this.stages().filter(s => used.has(s.id));
  });

  /**
   * Grade filter options, restricted to the selected stage
   */
  filterGrades = computed(() => {
    const stageId = this.selectedStageId();
    return stageId ? this.usedGrades().filter(g => g.stageId === stageId) : this.usedGrades();
  });

  /**
   * Subjects matching the search term and stage/grade filters
   * (a subject's stage is the stage of its grade)
   */
  filteredSubjects = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const stageId = this.selectedStageId();
    const gradeId = this.selectedGradeId();
    const grades = this.gradeMap();
    return this.subjects().filter(s =>
      (!stageId || grades.get(s.gradeId)?.stageId === stageId) &&
      (!gradeId || s.gradeId === gradeId) &&
      (!term ||
        (s.nameAr ?? '').toLowerCase().includes(term) ||
        (s.nameEn ?? '').toLowerCase().includes(term))
    );
  });

  /**
   * Subjects shown on the current page
   */
  pagedSubjects = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filteredSubjects().slice(start, start + this.pageSize);
  });

  hasFilters = computed(() => !!this.searchTerm() || !!this.selectedStageId() || !!this.selectedGradeId());

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private subjectToDelete: SubjectModel | null = null;

  constructor(
    private subjectService: SubjectService,
    private gradeService: GradeService,
    private stageService: StageService,
    private teacherService: TeacherService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe((language) => this.currentLanguage.set(language));

    this.loadLookups();
    this.loadSubjects();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all stages, grades and teachers for name display (includes inactive ones)
   */
  private loadLookups(): void {
    forkJoin({
      stages: this.stageService.getAllStages(),
      grades: this.gradeService.getAllGrades(),
      teachers: this.teacherService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ stages, grades, teachers }) => {
          this.stages.set(stages.status === 200 && stages.data ? stages.data : []);
          this.grades.set(grades.status === 200 && grades.data ? grades.data : []);
          this.teachers.set(teachers.status === 200 && teachers.data ? teachers.data : []);
        },
        error: (error) => {
          console.error('Error loading grades/teachers:', error);
          this.errorMessage.set(this.translate.instant('subjects.loadLookupsFailed'));
        }
      });
  }

  /**
   * Load all subjects from API
   */
  loadSubjects(): void {
    this.isLoading.set(true);

    this.subjectService
      .getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.subjects.set(response.data);
            this.clampPage();
          } else {
            this.errorMessage.set(
              response.message || this.translate.instant('subjects.loadFailed')
            );
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading subjects:', error);
          this.errorMessage.set(this.translate.instant('subjects.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Handle search input
   */
  onSearch(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
    this.page.set(1);
  }

  /**
   * Handle stage filter change; drops a grade filter that belongs to another stage
   */
  onStageFilterChange(event: Event): void {
    const stageId = (event.target as HTMLSelectElement).value;
    this.selectedStageId.set(stageId);

    const gradeId = this.selectedGradeId();
    if (gradeId && stageId && this.gradeMap().get(gradeId)?.stageId !== stageId) {
      this.selectedGradeId.set('');
    }
    this.page.set(1);
  }

  /**
   * Handle grade filter change
   */
  onGradeFilterChange(event: Event): void {
    this.selectedGradeId.set((event.target as HTMLSelectElement).value);
    this.page.set(1);
  }

  /**
   * Clear search and filters
   */
  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedStageId.set('');
    this.selectedGradeId.set('');
    this.page.set(1);
  }

  /**
   * Keep the current page within range after the list shrinks (e.g. after delete)
   */
  private clampPage(): void {
    const lastPage = Math.max(1, Math.ceil(this.filteredSubjects().length / this.pageSize));
    if (this.page() > lastPage) {
      this.page.set(lastPage);
    }
  }

  /**
   * Row number across pages
   */
  rowNumber(index: number): number {
    return (this.page() - 1) * this.pageSize + index + 1;
  }

  /**
   * Localized name for a stage or grade option
   */
  getOptionName(item: Stage | Grade): string {
    return this.currentLanguage() === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr;
  }

  /**
   * Get localized stage name of a subject's grade
   */
  getStageNameForGrade(gradeId: string): string {
    const grade = this.gradeMap().get(gradeId);
    const stage = grade ? this.stageMap().get(grade.stageId) : undefined;
    return stage ? this.getOptionName(stage) : '-';
  }

  /**
   * Get localized grade name by ID
   */
  getGradeName(gradeId: string): string {
    const grade = this.gradeMap().get(gradeId);
    return grade ? this.getOptionName(grade) : '-';
  }

  /**
   * Get localized names of the assigned teachers
   */
  getTeacherNames(teacherIds: string[] | null | undefined): string[] {
    const isAr = this.currentLanguage() === 'ar';
    return (teacherIds ?? []).map(id => {
      const t = this.teacherMap().get(id);
      if (!t) {
        return this.translate.instant('subjects.unknownTeacher');
      }
      return isAr ? t.fullNameAr || t.fullNameEn : t.fullNameEn || t.fullNameAr;
    });
  }

  /**
   * Navigate to create new subject form
   */
  navigateToCreate(): void {
    this.router.navigate(['/management/subjects/save']);
  }

  /**
   * Navigate to edit subject form
   * @param id - Subject ID
   */
  navigateToEdit(id: string): void {
    this.router.navigate(['/management/subjects/save', id]);
  }

  /**
   * Open subject details modal (uses data already loaded in the list)
   */
  openView(subject: SubjectModel, content: any): void {
    this.viewedSubject.set(subject);
    this.modalService.open(content, { centered: true })
      .hidden.pipe(takeUntil(this.destroy$))
      .subscribe(() => this.viewedSubject.set(null));
  }

  /**
   * Open delete confirmation modal
   */
  openDeleteConfirmation(subject: SubjectModel, content: any): void {
    this.subjectToDelete = subject;
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  /**
   * Confirm and delete subject
   */
  confirmDelete(): void {
    if (!this.subjectToDelete || this.isDeleting()) {
      return;
    }

    this.isDeleting.set(true);
    const id = this.subjectToDelete.id;

    this.subjectService
      .delete(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(this.translate.instant('subjects.deleted'));
            this.deleteModalRef?.close();
            this.subjectToDelete = null;
            this.loadSubjects();

            // Auto-dismiss success message after 4 seconds
            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.successMessage.set(null);
            this.errorMessage.set(
              response.message || this.translate.instant('subjects.deleteFailed')
            );
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting subject:', error);
          this.successMessage.set(null);
          this.errorMessage.set(this.translate.instant('subjects.deleteFailed'));
          this.deleteModalRef?.close();
          this.subjectToDelete = null;
          this.isDeleting.set(false);
        }
      });
  }

  /**
   * Cancel delete operation
   */
  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.subjectToDelete = null;
  }
}
