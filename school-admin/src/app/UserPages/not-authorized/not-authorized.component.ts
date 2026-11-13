import { Component, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { AuthService } from '../../core/services/auth.service';

/**
 * Not Authorized page
 * Shown when the user has no valid session (reason=unauthenticated)
 * or the API rejected the request with 403 (reason=forbidden).
 */
@Component({
  selector: 'app-not-authorized',
  templateUrl: './not-authorized.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotAuthorizedComponent {
  isArabic$: Observable<boolean>;
  readonly isForbidden: boolean;
  readonly isLoggedIn: boolean;
  private readonly returnUrl: string | null;

  constructor(
    route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    languageService: LanguageService
  ) {
    const params = route.snapshot.queryParamMap;
    this.isForbidden = params.get('reason') === 'forbidden';
    this.returnUrl = params.get('returnUrl');
    this.isLoggedIn = this.authService.isAuthenticated();
    this.isArabic$ = languageService.currentLanguage$.pipe(map(lang => lang === 'ar'));
  }

  goToLogin(): void {
    // Start a fresh session when logging in from here
    this.authService.clearSession();
    const queryParams = this.returnUrl ? { returnUrl: this.authService.getSafeReturnUrl(this.returnUrl) } : {};
    this.router.navigate(['/login'], { queryParams });
  }

  goHome(): void {
    this.router.navigate(['/home']);
  }
}
