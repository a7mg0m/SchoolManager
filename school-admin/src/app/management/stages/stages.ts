import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { StageService } from '../../core/services/stage.service';
import { Stage } from '../../core/models/stage.model';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-stages',
  standalone: false,
  styleUrl: './stages.scss',
  templateUrl: './stages.html',
})
export class StagesComponent implements OnInit, OnDestroy {
  // Signals for reactive state management (zoneless compatible)
  stages = signal<Stage[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(private stageService: StageService) {}

  ngOnInit(): void {
    this.loadStages();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load all stages from API
   */
  loadStages(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.stageService
      .getAllStages()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.stages.set(response.data);
            this.errorMessage.set(null);
          } else {
            this.errorMessage.set(response.message || 'Failed to load stages');
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading stages:', error);
          this.errorMessage.set('Failed to load stages. Please try again.');
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Handle stage activation/deactivation
   * @param stage - The stage to toggle
   */
  toggleStageStatus(stage: Stage): void {
    const isCurrentlyActive = stage.isActive;
    const action = isCurrentlyActive
      ? this.stageService.deactivateStage(stage.id)
      : this.stageService.activateStage(stage.id);

    action.pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        if (response.status === 200) {
       
          this.errorMessage.set(null);
          
          // Show success message from API response
          this.successMessage.set(response.message || 'Stage status updated successfully');
          this.loadStages();
          // Auto-dismiss success message after 4 seconds using RxJS timer (zoneless compatible)
          timer(4000)
            .pipe(takeUntil(this.destroy$))
            .subscribe(() => {
              this.successMessage.set(null);
              
            });
        } else {
          // Revert the toggle if API call fails
          console.error('Failed to update stage status:', response);
          this.successMessage.set(null);
          this.errorMessage.set(
            response.message || 'Failed to update stage status'
          );
        }
      },
      error: (error) => {
        console.error('Error updating stage status:', error);
        this.successMessage.set(null);
        this.errorMessage.set('Failed to update stage status. Please try again.');
      }
    });
  }

  /**
   * Get translated name based on locale
   * @param stage - The stage
   * @returns The appropriate name
   */
  getStageName(stage: Stage): string {
    // You can enhance this to use i18n service to detect current language
    const currentLanguage = localStorage.getItem('language') || 'en';
    return currentLanguage === 'ar' ? stage.nameAr : stage.nameEn;
  }
}
