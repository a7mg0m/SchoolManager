import { Injectable, Injector } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

/**
 * Error Handling Interceptor
 * Centralized error handling for all HTTP requests
 * 
 * Responsibilities:
 * - Intercept HTTP errors
 * - Display user-friendly error messages
 * - Handle specific error status codes
 * - Redirect to login on 401 Unauthorized
 * - Prevent infinite redirect loops
 */
@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  
  private isRedirectingToLogin = false;

  constructor(private injector: Injector) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        return this.handleError(error);
      })
    );
  }

  /**
   * Handle HTTP errors based on status code
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = this.getErrorMessage(error);

    console.error('HTTP Error:', {
      status: error.status,
      message: errorMessage,
      url: error.url
    });

    switch (error.status) {
      case 400:
        this.handleBadRequest(error);
        break;

      case 401:
        this.handleUnauthorized(error);
        break;

      case 403:
        this.handleForbidden(error);
        break;

      case 404:
        this.handleNotFound(error);
        break;

      case 409:
        this.handleConflict(error);
        break;

      case 422:
        this.handleValidationError(error);
        break;

      case 500:
      case 502:
      case 503:
      case 504:
        this.handleServerError(error);
        break;

      case 0:
        this.handleNetworkError(error);
        break;

      default:
        this.handleGenericError(error);
    }

    return throwError(() => error);
  }

  /**
   * Handle 400 Bad Request
   */
  private handleBadRequest(error: HttpErrorResponse): void {
    const message = error.error?.message || 'Invalid request. Please check your input.';
    this.showNotification(message, 'error');
  }

  /**
   * Handle 401 Unauthorized
   * Logout user and redirect to login page
   * Prevents redirect loops by tracking redirect state
   */
  private handleUnauthorized(error: HttpErrorResponse): void {
    // Prevent infinite redirect loops
    if (this.isRedirectingToLogin) {
      return;
    }

    this.isRedirectingToLogin = true;

    // Get services from injector (avoids circular dependency)
    const authService = this.injector.get(AuthService);
    const router = this.injector.get(Router);

    // Logout user
    authService.logout();

    // Show message
    this.showNotification(
      'Your session has expired. Please log in again.',
      'warning'
    );

    // Redirect to login
    router.navigate(['/login']).then(() => {
      // Reset redirect flag after navigation completes
      this.isRedirectingToLogin = false;
    });
  }

  /**
   * Handle 403 Forbidden
   */
  private handleForbidden(error: HttpErrorResponse): void {
    const message = 'You do not have permission to access this resource.';
    this.showNotification(message, 'error');
  }

  /**
   * Handle 404 Not Found
   */
  private handleNotFound(error: HttpErrorResponse): void {
    const message = 'The requested resource was not found.';
    this.showNotification(message, 'error');
  }

  /**
   * Handle 409 Conflict
   */
  private handleConflict(error: HttpErrorResponse): void {
    const message = error.error?.message || 'The request conflicts with existing data.';
    this.showNotification(message, 'error');
  }

  /**
   * Handle 422 Validation Error
   */
  private handleValidationError(error: HttpErrorResponse): void {
    if (error.error?.errors && typeof error.error.errors === 'object') {
      // Handle validation errors from API
      const fieldErrors = Object.entries(error.error.errors)
        .map(([field, messages]) => {
          if (Array.isArray(messages)) {
            return `${field}: ${messages.join(', ')}`;
          }
          return `${field}: ${messages}`;
        })
        .join('\n');

      this.showNotification(fieldErrors, 'error');
    } else {
      const message = error.error?.message || 'Validation failed. Please check your input.';
      this.showNotification(message, 'error');
    }
  }

  /**
   * Handle 5xx Server Errors
   */
  private handleServerError(error: HttpErrorResponse): void {
    const message = 'Server error. Please try again later.';
    this.showNotification(message, 'error');
  }

  /**
   * Handle Network Errors (0 status code)
   */
  private handleNetworkError(error: HttpErrorResponse): void {
    const message = 'Network error. Please check your connection.';
    this.showNotification(message, 'error');
  }

  /**
   * Handle generic/unknown errors
   */
  private handleGenericError(error: HttpErrorResponse): void {
    const message = 'An unexpected error occurred. Please try again.';
    this.showNotification(message, 'error');
  }

  /**
   * Extract error message from error response
   */
  private getErrorMessage(error: HttpErrorResponse): string {
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      return error.error.message;
    } else {
      // Server-side error
      return error.error?.message || error.message || 'An error occurred';
    }
  }

  /**
   * Show user-friendly notification
   * Currently logs to console; integrate with your notification/toast service
   * 
   * TODO: Integrate with your existing toast/notification component
   * For now, you can:
   * 1. Use ToastrService if installed
   * 2. Use NgToast if using ng-bootstrap
   * 3. Create your own notification service
   */
  private showNotification(message: string, type: 'error' | 'warning' | 'success' | 'info'): void {
    console.log(`[${type.toUpperCase()}] ${message}`);
    
    // TODO: Replace with your notification service
    // Example:
    // this.toastr.error(message); // for ngx-toastr
    // this.notificationService.show(message, type);
  }
}
