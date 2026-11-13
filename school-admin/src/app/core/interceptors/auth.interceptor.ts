import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

/**
 * Authentication Interceptor
 * Handles authorization for every outgoing API request.
 *
 * - Public endpoints (login/register/forgot-password) pass through untouched.
 * - Requests to our API gateway get `Authorization: Bearer {token}`.
 * - If a protected API request is made without a valid (non-expired) token,
 *   the request is NOT sent: the session is cleared, the user is sent to the
 *   Not Authorized page, and a 401 error is returned to the caller.
 * - Requests to other origins (e.g. translation files in /assets) never get the token.
 */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  // Public endpoints that don't require authentication
  private publicEndpoints = [
    '/api/user/login',
    '/api/user/register',
    '/api/user/forgot-password'
  ];

  constructor(private authService: AuthService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Only our API needs (and may receive) the token
    if (!this.isApiRequest(request.url) || this.isPublicEndpoint(request.url)) {
      return next.handle(request);
    }

    const token = this.authService.getToken();

    if (!token || this.authService.isTokenExpired(token)) {
      this.authService.handleUnauthorized('unauthenticated');
      return throwError(() => new HttpErrorResponse({
        status: 401,
        statusText: 'Unauthorized',
        url: request.url,
        error: { message: 'Not authorized. Please log in.' }
      }));
    }

    // Clone request and add Authorization header with Bearer token
    const authenticatedRequest = request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });

    return next.handle(authenticatedRequest);
  }

  /**
   * Check if the request targets our API gateway
   */
  private isApiRequest(url: string): boolean {
    const gatewayUrl = environment.gatewayUrl?.replace(/\/+$/, '');
    return (!!gatewayUrl && url.startsWith(gatewayUrl)) || url.startsWith('/api/');
  }

  /**
   * Check if the request URL is a public endpoint
   * Public endpoints don't require authentication headers
   */
  private isPublicEndpoint(url: string): boolean {
    return this.publicEndpoints.some(endpoint => url.includes(endpoint));
  }
}
