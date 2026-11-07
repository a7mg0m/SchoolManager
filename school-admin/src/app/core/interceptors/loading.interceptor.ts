import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { LoadingService } from '../services/loading.service';

/**
 * Loading Interceptor
 * Manages centralized loading state for HTTP requests
 * 
 * Responsibilities:
 * - Show loading indicator when HTTP requests start
 * - Hide loading indicator when requests complete or fail
 * - Support multiple concurrent requests
 * - Prevent loading indicator hiding while other requests are still running
 */
@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  
  // Endpoints that should bypass the loading indicator
  private bypassLoadingEndpoints = [
    // Add endpoints here that should not show loading indicator
    // Example: '/api/health', '/api/ping'
  ];

  constructor(private loadingService: LoadingService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Check if this endpoint should bypass loading indicator
    const shouldShowLoading = !this.shouldBypassLoading(request.url);

    if (shouldShowLoading) {
      this.loadingService.showLoading();
    }

    return next.handle(request).pipe(
      finalize(() => {
        // Hide loading when request completes (success or error)
        if (shouldShowLoading) {
          this.loadingService.hideLoading();
        }
      })
    );
  }

  /**
   * Check if the request URL should bypass the loading indicator
   */
  private shouldBypassLoading(url: string): boolean {
    return this.bypassLoadingEndpoints.some(endpoint => url.includes(endpoint));
  }
}
