import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { GradeService } from '../../core/services/grade.service';
import { Grade } from '../../core/models/grade.model';
import { Stage } from '../../core/models/stage.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-grades',
  standalone: false,
  styleUrl: './grades.scss',
  templateUrl: './grades.html',
})
export class GradesComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  stages = signal<Stage[]>([]);
  grades = signal<Grade[]>([]);
  selectedStageId = signal<string>('');
  currentLanguage = signal<string>('en');
  isLoadingStages = signal(false);
  isLoadingGrades = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private gradeService: GradeService,
    private languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.loadActiveStages();
    // Subscribe to language changes
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe((language) => {
        this.currentLanguage.set(language);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load active stages from API
   */
  loadActiveStages(): void {
    this.isLoadingStages.set(true);
    this.errorMessage.set(null);

    this.gradeService
      .getActiveStages()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.stages.set(response.data);
            this.errorMessage.set(null);
          } else {
            this.errorMessage.set(response.message || 'Failed to load stages');
          }
          this.isLoadingStages.set(false);
        },
        error: (error) => {
          console.error('Error loading stages:', error);
          this.errorMessage.set('Failed to load stages. Please try again.');
          this.isLoadingStages.set(false);
        }
      });
  }

  /**
   * Handle stage selection change
   * @param event - Change event from dropdown
   */
  onStageChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    const stageId = target.value;
    this.selectedStageId.set(stageId);

    if (stageId) {
      this.loadGradesByStage(stageId);
    } else {
      this.grades.set([]);
    }
  }

  /**
   * Load grades for selected stage from API
   * @param stageId - The stage ID to load grades for
   */
  loadGradesByStage(stageId: string): void {
    this.isLoadingGrades.set(true);
    this.errorMessage.set(null);

    this.gradeService
      .getGradesByStage(stageId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.grades.set(response.data);
            this.errorMessage.set(null);
          } else {
            this.errorMessage.set(response.message || 'Failed to load grades');
            this.grades.set([]);
          }
          this.isLoadingGrades.set(false);
        },
        error: (error) => {
          console.error('Error loading grades:', error);
          this.errorMessage.set('Failed to load grades. Please try again.');
          this.grades.set([]);
          this.isLoadingGrades.set(false);
        }
      });
  }

  /**
   * Handle grade activation/deactivation
   * @param grade - The grade to toggle
   */
  toggleGradeStatus(grade: Grade): void {
    const isCurrentlyActive = grade.isActive;
    const action = isCurrentlyActive
      ? this.gradeService.deactivateGrade(grade.id)
      : this.gradeService.activateGrade(grade.id);

    action.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
          this.errorMessage.set(null);

          // Show success message from API response
          this.successMessage.set(response.message || 'Grade status updated successfully');
          this.loadGradesByStage(this.selectedStageId());
          // Auto-dismiss success message after 4 seconds using RxJS timer (zoneless compatible)
          timer(4000)
            .pipe(takeUntil(this.destroy$))
            .subscribe(() => {
              this.successMessage.set(null);
            });
        } else {
          // Revert the toggle if API call fails
          console.error('Failed to update grade status:', response);
          this.successMessage.set(null);
          this.errorMessage.set(
            response.message || 'Failed to update grade status'
          );
        }
      },
      error: (error) => {
        console.error('Error updating grade status:', error);
        this.successMessage.set(null);
        this.errorMessage.set('Failed to update grade status. Please try again.');
      }
    });
  }

  /**
   * Get stage name based on current selected language
   * @param stage - The stage object
   * @returns Stage name in the current language
   */
  getStageName(stage: Stage): string {
    return this.currentLanguage() === 'ar' ? stage.nameAr : stage.nameEn || stage.nameAr || 'Unknown';
  }

  /**
   * Get grade name based on current selected language
   * @param grade - The grade object
   * @returns Grade name in the current language
   */
  getGradeName(grade: Grade): string {
    return this.currentLanguage() === 'ar' ? grade.nameAr : grade.nameEn || grade.nameAr || 'Unknown';
  }
}
