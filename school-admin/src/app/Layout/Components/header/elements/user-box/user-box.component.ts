import { Component, ChangeDetectionStrategy } from '@angular/core';
import { ThemeOptions } from '../../../../../theme-options';
import { AuthService } from '../../../../../core/services/auth.service';

@Component({
  selector: 'app-user-box',
  templateUrl: './user-box.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false,
})
export class UserBoxComponent {
  constructor(
    public globals: ThemeOptions,
    private authService: AuthService
  ) {}

  /**
   * Get the current user's login name from AuthService
   * Returns login name or 'User' if not available
   */
  get loginName(): string {
    return this.authService.getLoginName() || 'User';
  }

  /**
   * Handle sign out action
   * Clears all authentication data from localStorage
   * Updates authentication state
   * Navigates to login page
   */
  onSignOut(): void {
    this.authService.logout();
  }
}
