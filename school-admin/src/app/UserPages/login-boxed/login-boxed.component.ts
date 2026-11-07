import { Component, ChangeDetectionStrategy, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { AuthService } from '../../core/services/auth.service';
import { LoadingService } from '../../core/services/loading.service';
import { Observable, Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-login-boxed',
  templateUrl: './login-boxed.component.html',
  styleUrls: ['./login-boxed.component.scss'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginBoxedComponent implements OnInit, OnDestroy {
  loginForm!: FormGroup;
  isArabic$: Observable<boolean>;
  loading$: Observable<boolean>;
  loginError: string | null = null;
  isSubmitted = false;

  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private loadingService: LoadingService,
    private router: Router,
    private translateService: TranslateService,
    private languageService: LanguageService
  ) {
    this.isArabic$ = this.languageService.currentLanguage$.pipe(
      map(lang => lang === 'ar')
    );
    this.loading$ = this.loadingService.loading$;
  }

  ngOnInit(): void {
    this.initializeForm();
  }

  /**
   * Initialize reactive form with validators
   */
  private initializeForm(): void {
    this.loginForm = this.formBuilder.group({
      loginName: ['', [
        Validators.required,
        Validators.minLength(3)
      ]],
      password: ['', [
        Validators.required,
        Validators.minLength(6)
      ]],
      rememberMe: [false]
    });
  }

  /**
   * Handle login form submission
   */
  onSubmit(): void {
    // Mark form as submitted
    this.isSubmitted = true;

    // Clear previous error
    this.loginError = null;

    // Validate form
    if (!this.loginForm.valid) {
      return;
    }

    // Prevent multiple submissions while loading
    if (this.loadingService.isLoading()) {
      return;
    }

    // Get form values
    const { loginName, password } = this.loginForm.value;

    // Call auth service to login
    // On success: AuthService stores auth data and navigates to /home
    // On error: Component displays error message
    this.authService.login({ loginName, password })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          console.debug('Login successful - navigation handled by AuthService');
          // Navigation to /home is handled by AuthService
        },
        error: (error) => {
          console.error('Login failed:', error);
          
          // Display error message from API response or HTTP error
          this.loginError = this.getErrorMessage(error);

          // Reset form on error to allow retry
          this.isSubmitted = false;
        }
      });
  }

  /**
   * Handle forgot password action
   */
  onForgotPassword(): void {
    this.router.navigate(['/pages/forgot-password-boxed']);
  }

  /**
   * Get user-friendly error message from error response
   */
  private getErrorMessage(error: any): string {
    if (error.error?.message) {
      return error.error.message;
    }

    switch (error.status) {
      case 0:
        return 'Unable to connect to server. Please check your network connection.';
      case 400:
        return 'Invalid username or password.';
      case 401:
        return 'Invalid credentials. Please try again.';
      case 403:
        return 'Access denied. Your account may be disabled.';
      case 404:
        return 'Login service not found.';
      case 422:
        return 'Validation error. Please check your input.';
      case 500:
      case 502:
      case 503:
      case 504:
        return 'Server error. Please try again later.';
      default:
        return 'Login failed. Please try again.';
    }
  }

  /**
   * Get form control for use in template
   */
  getControl(name: string) {
    return this.loginForm.get(name);
  }

  /**
   * Check if field has error
   */
  hasError(fieldName: string, errorType: string): boolean {
    const field = this.loginForm.get(fieldName);
    return this.isSubmitted && field?.hasError(errorType) && field?.touched;
  }

  /**
   * Cleanup subscriptions
   */
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
