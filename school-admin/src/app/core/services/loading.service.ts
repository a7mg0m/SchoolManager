import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

/**
 * Loading Service
 * Manages centralized loading state for HTTP requests
 * Tracks active request count to prevent premature loading indicator hiding
 */
@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private activeRequests = 0;
  private loadingSubject = new BehaviorSubject<boolean>(false);
  public loading$: Observable<boolean> = this.loadingSubject.asObservable();

  constructor() {}

  /**
   * Show loading indicator
   * Increments active request counter
   */
  showLoading(): void {
    this.activeRequests++;
    this.updateLoadingState();
  }

  /**
   * Hide loading indicator
   * Decrements active request counter
   * Only hides when all requests are complete
   */
  hideLoading(): void {
    this.activeRequests = Math.max(0, this.activeRequests - 1);
    this.updateLoadingState();
  }

  /**
   * Reset loading state
   * Used in error scenarios to ensure clean state
   */
  reset(): void {
    this.activeRequests = 0;
    this.updateLoadingState();
  }

  /**
   * Get current loading state
   */
  isLoading(): boolean {
    return this.loadingSubject.value;
  }

  /**
   * Update loading state based on active request counter
   */
  private updateLoadingState(): void {
    this.loadingSubject.next(this.activeRequests > 0);
  }
}
