import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { AcademicYear } from '../../core/models/academic-year.model';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-academic-year-list',
  standalone: false,
  styleUrl: './academic-year-list.component.scss',
  templateUrl: './academic-year-list.component.html',
})
export class AcademicYearListComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  academicYears = signal<AcademicYear[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isDeleting = signal(false);

  // Subject for cleanup
  private destroy$ = new Subject<void>();
  private deleteModalRef: NgbModalRef | null = null;
  private academicYearToDelete: AcademicYear | null = null;

  constructor(
    private academicYearService: AcademicYearService,
    private router: Router,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.loadAcademicYears();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all academic years from API
   */
  loadAcademicYears(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.academicYearService
      .getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.academicYears.set(response.data);
            this.errorMessage.set(null);
          } else {
            this.errorMessage.set(
              response.message || 'Failed to load academic years'
            );
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading academic years:', error);
          this.errorMessage.set(
            'Failed to load academic years. Please try again.'
          );
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Navigate to create new academic year form
   */
  navigateToCreate(): void {
    this.router.navigate(['/management/academic-years/save']);
  }

  /**
   * Navigate to edit academic year form
   * @param id - Academic year ID
   */
  navigateToEdit(id: string): void {
    this.router.navigate(['/management/academic-years/save', id]);
  }

  /**
   * Open delete confirmation modal
   * @param academicYear - The academic year to delete
   * @param content - Modal template reference
   */
  openDeleteConfirmation(academicYear: AcademicYear, content: any): void {
    this.academicYearToDelete = academicYear;
    this.deleteModalRef = this.modalService.open(content, {
      centered: true,
      backdrop: 'static',
      keyboard: false
    });
  }

  /**
   * Confirm and delete academic year
   */
  confirmDelete(): void {
    if (!this.academicYearToDelete) {
      return;
    }

    this.isDeleting.set(true);
    const id = this.academicYearToDelete.id;

    this.academicYearService
      .delete(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.errorMessage.set(null);
            this.successMessage.set(
              response.message || 'Academic year deleted successfully'
            );
            this.deleteModalRef?.close();
            this.academicYearToDelete = null;
            this.loadAcademicYears();

            // Auto-dismiss success message after 4 seconds
            timer(4000)
              .pipe(takeUntil(this.destroy$))
              .subscribe(() => {
                this.successMessage.set(null);
              });
          } else {
            this.successMessage.set(null);
            this.errorMessage.set(
              response.message || 'Failed to delete academic year'
            );
          }
          this.isDeleting.set(false);
        },
        error: (error) => {
          console.error('Error deleting academic year:', error);
          this.successMessage.set(null);
          this.errorMessage.set(
            'Failed to delete academic year. Please try again.'
          );
          this.isDeleting.set(false);
        }
      });
  }

  /**
   * Cancel delete operation
   */
  cancelDelete(): void {
    this.deleteModalRef?.close();
    this.academicYearToDelete = null;
  }

  /**
   * Format date for display
   * @param dateStr - Date string in format 'yyyy-MM-dd'
   * @returns Formatted date string
   */
  formatDate(dateStr: string): string {
    try {
      const date = new Date(dateStr + 'T00:00:00');
      return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      });
    } catch {
      return dateStr;
    }
  }
}
