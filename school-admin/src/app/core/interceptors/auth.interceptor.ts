import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Authentication Interceptor
 * Automatically attaches JWT access token to outgoing HTTP requests
 * 
 * Responsibilities:
 * - Attach Bearer token to Authorization header
 * - Skip token attachment for public endpoints if needed
 * - Handle missing tokens gracefully
 * 
 * The interceptor will add the Authorization header with format:
 * Authorization: Bearer {accessToken}
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
    // Check if this is a public endpoint
    const isPublicEndpoint = this.isPublicEndpoint(request.url);

    // Get JWT token from localStorage via auth service
    const token = this.authService.getToken();

    // If no token or this is a public endpoint, pass request as is
    // The error interceptor will handle 401 responses
    if (!token || isPublicEndpoint) {
      return next.handle(request);
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
   * Check if the request URL is a public endpoint
   * Public endpoints don't require authentication headers
   */
  private isPublicEndpoint(url: string): boolean {
    return this.publicEndpoints.some(endpoint => url.includes(endpoint));
  }
}
