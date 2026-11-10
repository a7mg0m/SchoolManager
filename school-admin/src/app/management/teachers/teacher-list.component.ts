import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { TranslateService } from '@ngx-translate/core';
import { TeacherService } from '../../core/services/teacher.service';
import { Teacher } from '../../core/models/teacher.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-teacher-list',
  standalone: false,
  styleUrl: './teacher-list.component.scss',
  templateUrl: './teacher-list.component.html',
})
export class TeacherListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  teachers = signal<Teacher[]>([]);
  searchTerm = signal<string>('');
  page = signal(1);
  readonly pageSize = 10;
  currentLanguage = signal<string>('en');
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);
  viewedTeacher = signal<Teacher | null>(null);

  /**
   * Teachers matching the search term (name, national ID, phone or email)
   */
  filteredTeachers = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) {
      return this.teachers();
    }
    return this.teachers().filter(t =>
      [t.fullNameAr, t.fullNameEn, t.nationalId, t.phoneNumber, t.email]
        .some(value => (value ?? '').toLowerCase().includes(term))
    );
  });

  /**
   * Teachers shown on the current page
   */
  pagedTeachers = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filteredTeachers().slice(start, start + this.pageSize);
  });

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private teacherToDelete: Teacher | null = null;

  constructor(
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

    this.loadTeachers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all teachers from API
   */
  loadTeachers(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.teacherService
      .getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.teachers.set(response.data);
            this.clampPage();
          } else {
            this.errorMessage.set(
              response.message || this.translate.instant('teachers.loadFailed')
            );
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading teachers:', error);
          this.errorMessage.set(this.translate.instant('teachers.loadFailed'));
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
    const lastPage = Math.max(1, Math.ceil(this.filteredTeachers().length / this.pageSize));
    if (this.page() > lastPage) {
      this.page.set(lastPage);
    }
  }

  /**
   * Get the teacher's name in the current language
   */
  getTeacherName(teacher: Teacher): string {
    return this.currentLanguage() === 'ar'
      ? teacher.fullNameAr || teacher.fullNameEn
      : teacher.fullNameEn || teacher.fullNameAr;
  }

  /**
   * Translate the stored gender value
   */
  getGenderLabel(gender: string | null | undefined): string {
    if (gender === 'Male' || gender === 'Female') {
      return this.translate.instant(`teachers.gender${gender}`);
    }
    return gender || '-';
  }

  /**
   * Navigate to create new teacher form
   */
  navigateToCreate(): void {
    this.router.navigate(['/management/teachers/save']);
  }

  /**
   * Navigate to edit teacher form
   * @param id - Teacher ID
   */
  navigateToEdit(id: string): void {
    this.router.navigate(['/management/teachers/save', id]);
  }

  /**
   * Open teacher details modal (uses data already loaded in the list)
   * @param teacher - The teacher to view
   * @param content - Modal template reference
   */
  openView(teacher: Teacher, content: any): void {
    this.viewedTeacher.set(teacher);
    this.modalService.open(content, { centered: true, size: 'lg' })
      .hidden.pipe(takeUntil(this.destroy$))
      .subscribe(() => this.viewedTeacher.set(null));
  }

  /**
   * Open delete confirmation modal
   * @param teacher - The teacher to delete
   * @param content - Modal template reference
   */
  openDeleteConfirmation(teacher: Teacher, content: any): void {
    this.teacherToDelete = teacher;
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  /**
   * Confirm and delete teacher
   */
  confirmDelete(): void {
    if (!this.teacherToDelete || this.isDeleting()) {
      return;
    }

    this.isDeleting.set(true);
    const id = this.teacherToDelete.id;

    this.teacherService
      .delete(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(this.translate.instant('teachers.deleted'));
            this.deleteModalRef?.close();
            this.teacherToDelete = null;
            this.loadTeachers();

            // Auto-dismiss success message after 4 seconds
            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => this.successMessage.set(null));
          } else {
            this.successMessage.set(null);
            this.errorMessage.set(
              response.message || this.translate.instant('teachers.deleteFailed')
            );
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting teacher:', error);
          this.successMessage.set(null);
          this.errorMessage.set(this.translate.instant('teachers.deleteFailed'));
          this.deleteModalRef?.close();
          this.teacherToDelete = null;
          this.isDeleting.set(false);
        }
      });
  }

  /**
   * Cancel delete operation
   */
  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.teacherToDelete = null;
  }
}
