import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { ParentService } from '../../core/services/parent.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { GradeService } from '../../core/services/grade.service';
import { StageService } from '../../core/services/stage.service';
import { SectionService } from '../../core/services/section.service';
import { Stage } from '../../core/models/stage.model';
import { Parent } from '../../core/models/parent.model';
import { AcademicYear } from '../../core/models/academic-year.model';
import { Grade } from '../../core/models/grade.model';
import { Section } from '../../core/models/section.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, timer, forkJoin } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-parent-list',
  standalone: false,
  styleUrl: './parent-list.component.scss',
  templateUrl: './parent-list.component.html',
})
export class ParentListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  parents = signal<Parent[]>([]);
  searchTerm = signal<string>('');
  page = signal(1);
  readonly pageSize = 10;
  currentLanguage = signal<string>('en');
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);
  viewedParent = signal<Parent | null>(null);
  parentToDelete = signal<Parent | null>(null);

  // Reference data for resolving student IDs in the details view (loaded on first view)
  isLoadingLookups = signal(false);
  private lookupsLoaded = false;
  private academicYearMap = signal(new Map<string, AcademicYear>());
  private stageMap = signal(new Map<string, Stage>());
  private gradeMap = signal(new Map<string, Grade>());
  private sectionMap = signal(new Map<string, Section>());

  /**
   * Parents matching the search term (names, phone or email)
   */
  filteredParents = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) {
      return this.parents();
    }
    return this.parents().filter(p =>
      [p.fullNameAr, p.fullNameEn, p.phoneNumber, p.email]
        .some(value => (value ?? '').toLowerCase().includes(term))
    );
  });

  /**
   * Parents shown on the current page
   */
  pagedParents = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filteredParents().slice(start, start + this.pageSize);
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;

  constructor(
    private parentService: ParentService,
    private academicYearService: AcademicYearService,
    private gradeService: GradeService,
    private stageService: StageService,
    private sectionService: SectionService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe((language) => this.currentLanguage.set(language));

    this.loadParents();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all parents from API
   */
  loadParents(): void {
    this.isLoading.set(true);

    this.parentService
      .getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.parents.set(response.data);
            this.clampPage();
          } else {
            this.errorMessage.set(
              response.message || this.translate.instant('parents.loadFailed')
            );
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading parents:', error);
          this.errorMessage.set(this.translate.instant('parents.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Load academic years, stages, grades and sections once (only needed by the details view)
   */
  private ensureLookups(): void {
    if (this.lookupsLoaded || this.isLoadingLookups()) {
      return;
    }
    this.isLoadingLookups.set(true);

    forkJoin({
      academicYears: this.academicYearService.getAll(),
      stages: this.stageService.getAllStages(),
      grades: this.gradeService.getAllGrades(),
      sections: this.sectionService.getAll()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ academicYears, stages, grades, sections }) => {
          const ok = <T>(r: { status: number; data: T[] }) => (r.status === 200 && r.data ? r.data : []);
          this.academicYearMap.set(new Map(ok(academicYears).map(a => [a.id, a])));
          this.stageMap.set(new Map(ok(stages).map(s => [s.id, s])));
          this.gradeMap.set(new Map(ok(grades).map(g => [g.id, g])));
          this.sectionMap.set(new Map(ok(sections).map(s => [s.id, s])));
          this.lookupsLoaded = true;
          this.isLoadingLookups.set(false);
        },
        error: (error) => {
          console.error('Error loading reference data:', error);
          this.isLoadingLookups.set(false);
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
   * Clear search
   */
  clearSearch(): void {
    this.searchTerm.set('');
    this.page.set(1);
  }

  /**
   * Keep the current page within range after the list shrinks (e.g. after delete)
   */
  private clampPage(): void {
    const lastPage = Math.max(1, Math.ceil(this.filteredParents().length / this.pageSize));
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

  private localized(item: { nameAr: string; nameEn: string } | undefined): string {
    if (!item) {
      return '-';
    }
    return this.currentLanguage() === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr;
  }

  getAcademicYearName(id: string): string {
    return this.academicYearMap().get(id)?.name || '-';
  }

  /**
   * A student's stage is the stage of its grade
   */
  getStageNameForGrade(gradeId: string): string {
    const grade = this.gradeMap().get(gradeId);
    return this.localized(grade ? this.stageMap().get(grade.stageId) : undefined);
  }

  getGradeName(id: string): string {
    return this.localized(this.gradeMap().get(id));
  }

  getSectionName(id: string): string {
    return this.localized(this.sectionMap().get(id));
  }

  /**
   * Translate the stored gender value
   */
  getGenderLabel(gender: string | null | undefined): string {
    if (gender === 'Male' || gender === 'Female') {
      return this.translate.instant(`parents.gender${gender}`);
    }
    return gender || '-';
  }

  /**
   * Navigate to create new parent form
   */
  navigateToCreate(): void {
    this.router.navigate(['/management/parents/save']);
  }

  /**
   * Navigate to edit parent form
   */
  navigateToEdit(id: string): void {
    this.router.navigate(['/management/parents/save', id]);
  }

  /**
   * Open parent details modal (parent and students come from the list response)
   */
  openView(parent: Parent, content: any): void {
    this.ensureLookups();
    this.viewedParent.set(parent);
    this.modalService.open(content, { centered: true, size: 'xl', scrollable: true })
      .hidden.pipe(takeUntil(this.destroy$))
      .subscribe(() => this.viewedParent.set(null));
  }

  /**
   * Open delete confirmation modal
   */
  openDeleteConfirmation(parent: Parent, content: any): void {
    this.parentToDelete.set(parent);
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  /**
   * Confirm and delete parent (the backend also deletes its students)
   */
  confirmDelete(): void {
    const parent = this.parentToDelete();
    if (!parent || this.isDeleting()) {
      return;
    }

    this.isDeleting.set(true);

    this.parentService
      .delete(parent.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(this.translate.instant('parents.deleted'));
            this.deleteModalRef?.close();
            this.parentToDelete.set(null);
            this.loadParents();

            // Auto-dismiss success message after 4 seconds
            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.successMessage.set(null);
            this.errorMessage.set(
              response.message || this.translate.instant('parents.deleteFailed')
            );
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting parent:', error);
          this.successMessage.set(null);
          this.errorMessage.set(this.translate.instant('parents.deleteFailed'));
          this.deleteModalRef?.close();
          this.parentToDelete.set(null);
          this.isDeleting.set(false);
        }
      });
  }

  /**
   * Cancel delete operation
   */
  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.parentToDelete.set(null);
  }
}
