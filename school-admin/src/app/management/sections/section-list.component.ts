import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { SectionService } from '../../core/services/section.service';
import { GradeService } from '../../core/services/grade.service';
import { Section } from '../../core/models/section.model';
import { Stage } from '../../core/models/stage.model';
import { Grade } from '../../core/models/grade.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, timer, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-section-list',
  standalone: false,
  styleUrl: './section-list.component.scss',
  templateUrl: './section-list.component.html',
})
export class SectionListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  sections = signal<Section[]>([]);
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  selectedStageId = signal<string>('');
  selectedGradeId = signal<string>('');
  currentLanguage = signal<string>('en');
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);

  /**
   * Grades available in the filter (restricted to the selected stage)
   */
  filterGrades = computed(() => {
    const stageId = this.selectedStageId();
    return stageId ? this.grades().filter(g => g.stageId === stageId) : this.grades();
  });

  /**
   * Sections matching the selected stage and grade filters
   */
  filteredSections = computed(() => {
    const stageId = this.selectedStageId();
    const gradeId = this.selectedGradeId();
    return this.sections().filter(s =>
      (!stageId || s.stageId === stageId) && (!gradeId || s.gradeId === gradeId)
    );
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private sectionToDelete: Section | null = null;

  constructor(
    private sectionService: SectionService,
    private gradeService: GradeService,
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
    this.loadSections();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load active stages and grades for filters and name display
   */
  private loadLookups(): void {
    forkJoin({
      stages: this.gradeService.getActiveStages(),
      grades: this.gradeService.getActiveGrades()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ stages, grades }) => {
          this.stages.set(stages.status === 200 && stages.data ? stages.data : []);
          this.grades.set(grades.status === 200 && grades.data ? grades.data : []);
        },
        error: (error) => {
          console.error('Error loading stages/grades:', error);
          this.errorMessage.set(this.translate.instant('sections.loadLookupsFailed'));
        }
      });
  }

  /**
   * Load all sections from API
   */
  loadSections(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.sectionService
      .getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.sections.set(response.data);
            this.errorMessage.set(null);
          } else {
            this.errorMessage.set(
              response.message || this.translate.instant('sections.loadFailed')
            );
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading sections:', error);
          this.errorMessage.set(this.translate.instant('sections.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Handle stage filter change
   */
  onStageFilterChange(event: Event): void {
    const stageId = (event.target as HTMLSelectElement).value;
    this.selectedStageId.set(stageId);

    // Clear grade filter if it no longer belongs to the selected stage
    const gradeId = this.selectedGradeId();
    if (gradeId && stageId && !this.grades().some(g => g.id === gradeId && g.stageId === stageId)) {
      this.selectedGradeId.set('');
    }
  }

  /**
   * Handle grade filter change
   */
  onGradeFilterChange(event: Event): void {
    this.selectedGradeId.set((event.target as HTMLSelectElement).value);
  }

  /**
   * Clear all filters
   */
  clearFilters(): void {
    this.selectedStageId.set('');
    this.selectedGradeId.set('');
  }

  /**
   * Get localized stage name by ID
   */
  getStageName(stageId: string): string {
    const stage = this.stages().find(s => s.id === stageId);
    if (!stage) {
      return '-';
    }
    return this.currentLanguage() === 'ar' ? stage.nameAr : stage.nameEn || stage.nameAr;
  }

  /**
   * Get localized grade name by ID
   */
  getGradeName(gradeId: string): string {
    const grade = this.grades().find(g => g.id === gradeId);
    if (!grade) {
      return '-';
    }
    return this.currentLanguage() === 'ar' ? grade.nameAr : grade.nameEn || grade.nameAr;
  }

  /**
   * Get localized name for a dropdown option
   */
  getOptionName(item: Stage | Grade): string {
    return this.currentLanguage() === 'ar' ? item.nameAr : item.nameEn || item.nameAr;
  }

  /**
   * Navigate to create new section form
   */
  navigateToCreate(): void {
    this.router.navigate(['/management/sections/save']);
  }

  /**
   * Navigate to edit section form
   * @param id - Section ID
   */
  navigateToEdit(id: string): void {
    this.router.navigate(['/management/sections/save', id]);
  }

  /**
   * Open delete confirmation modal
   * @param section - The section to delete
   * @param content - Modal template reference
   */
  openDeleteConfirmation(section: Section, content: any): void {
    this.sectionToDelete = section;
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  /**
   * Confirm and delete section
   */
  confirmDelete(): void {
    if (!this.sectionToDelete || this.isDeleting()) {
      return;
    }

    this.isDeleting.set(true);
    const id = this.sectionToDelete.id;

    this.sectionService
      .delete(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(this.translate.instant('sections.deleted'));
            this.deleteModalRef?.close();
            this.sectionToDelete = null;
            this.loadSections();

            // Auto-dismiss success message after 4 seconds
            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => {
                this.successMessage.set(null);
              });
          } else {
            this.successMessage.set(null);
            this.errorMessage.set(
              response.message || this.translate.instant('sections.deleteFailed')
            );
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting section:', error);
          this.successMessage.set(null);
          this.errorMessage.set(this.translate.instant('sections.deleteFailed'));
          this.deleteModalRef?.close();
          this.sectionToDelete = null;
          this.isDeleting.set(false);
        }
      });
  }

  /**
   * Cancel delete operation
   */
  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.sectionToDelete = null;
  }
}
