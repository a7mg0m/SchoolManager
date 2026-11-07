import { Injectable, effect } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { ApiService } from './api.service';
import { LoginRequest, LoginResponse, LoginData } from '../models';

/**
 * Authentication Service
 * Handles user login, token management, and authentication state
 * 
 * Security Notes:
 * - Tokens are stored in localStorage (persists across browser sessions)
 * - Never log JWT tokens to console
 * - Never log passwords
 * - Always use HTTPS in production
 * - Validate token expiration as needed
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'token';
  private readonly LOGIN_NAME_KEY = 'loginName';
  private readonly USER_ID_KEY = 'userId';

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  private currentUserSubject = new BehaviorSubject<Partial<LoginData> | null>(this.loadStoredUserInfo());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private apiService: ApiService,
    private router: Router
  ) {
    // Initialize authentication state from storage on service creation
    this.initializeAuthState();
  }

  /**
   * Initialize authentication state from stored tokens and user info
   * Called on service instantiation to restore session after page refresh
   */
  private initializeAuthState(): void {
    const hasToken = this.hasToken();
    this.isAuthenticatedSubject.next(hasToken);

    const userInfo = this.loadStoredUserInfo();
    if (userInfo) {
      this.currentUserSubject.next(userInfo);
    }
  }

  /**
   * Login user with credentials
   * Sends POST request to /api/user/login
   * 
   * Handles success (HTTP 200 with response.status === 200):
   * - Extracts userId, loginName, token from response.data
   * - Stores in localStorage with keys: 'token', 'loginName', 'userId'
   * - Updates authentication state
   * - Navigates to /home
   * 
   * Handles failure:
   * - Does not store any authentication data
   * - Does not navigate to /home
   * - Returns error to component for error message display
   * 
   * @param loginRequest - LoginRequest object with loginName and password
   * @returns Observable<LoginResponse> - API response with authentication data
   */
  login(loginRequest: LoginRequest): Observable<LoginResponse> {
    const loginUrl = '/api/user/login';
    
    return this.apiService.post<LoginResponse>(loginUrl, loginRequest).pipe(
      tap((response: LoginResponse) => {
        // Validate response structure
        if (response.status === 200 && response.data) {
          const { token, loginName, userId } = response.data;

          // Validate required fields exist
          if (!token || !loginName || !userId) {
            throw new Error('Invalid response: missing required authentication fields');
          }

          // Store authentication data in localStorage
          localStorage.setItem(this.TOKEN_KEY, token);
          localStorage.setItem(this.LOGIN_NAME_KEY, loginName);
          localStorage.setItem(this.USER_ID_KEY, userId);

          // Update user info subject
          this.currentUserSubject.next({
            userId,
            loginName,
            token
          });

          // Update authentication state
          this.isAuthenticatedSubject.next(true);

          // Log successful login (without logging the token)
          console.debug('User authentication successful');

          // Navigate to home page
          this.router.navigate(['/home']);
        } else {
          // Response status is not 200 or data is null
          throw new Error(`Authentication failed: ${response.message || 'Invalid response'}`);
        }
      }),
      catchError(error => {
        console.error('Authentication failed:', error.status || error.message);
        // Return error for component to handle
        return throwError(() => error);
      })
    );
  }

  /**
   * Logout user
   * Removes all authentication data from localStorage
   * Updates authentication state
   * Navigates to login page
   */
  logout(): void {
    // Clear all stored authentication data
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.LOGIN_NAME_KEY);
    localStorage.removeItem(this.USER_ID_KEY);

    // Update authentication state
    this.isAuthenticatedSubject.next(false);
    this.currentUserSubject.next(null);

    console.debug('User logged out');

    // Navigate to login page
    this.router.navigate(['/login']);
  }

  /**
   * Get current authentication token
   * @returns JWT token string or null if not authenticated
   */
  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Check if user is authenticated (has valid token)
   * @returns Boolean indicating authentication status
   */
  isAuthenticated(): boolean {
    return this.hasToken();
  }

  /**
   * Get current user ID
   * @returns User ID string or null if not authenticated
   */
  getUserId(): string | null {
    return localStorage.getItem(this.USER_ID_KEY);
  }

  /**
   * Get current user's login name
   * @returns Login name string or null if not authenticated
   */
  getLoginName(): string | null {
    return localStorage.getItem(this.LOGIN_NAME_KEY);
  }

  /**
   * Get current user information
   * @returns User info object or null if not authenticated
   */
  getCurrentUser(): Partial<LoginData> | null {
    return this.currentUserSubject.value;
  }

  /**
   * Check if authentication token exists in localStorage
   * @returns Boolean indicating token availability
   */
  private hasToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Load user information from localStorage
   * @returns User info object or null if not found
   */
  private loadStoredUserInfo(): Partial<LoginData> | null {
    try {
      const token = localStorage.getItem(this.TOKEN_KEY);
      const userId = localStorage.getItem(this.USER_ID_KEY);
      const loginName = localStorage.getItem(this.LOGIN_NAME_KEY);

      if (token && userId && loginName) {
        return { token, userId, loginName };
      }
      return null;
    } catch (e) {
      console.error('Failed to load stored user info');
      return null;
    }
  }
}
