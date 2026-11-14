import { Component, ChangeDetectionStrategy, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ThemeOptions } from '../../../../../theme-options';
import { AuthService } from '../../../../../core/services/auth.service';
import { UserInfo } from '../../../../../core/models';

@Component({
  selector: 'app-user-box',
  templateUrl: './user-box.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false,
})
export class UserBoxComponent {
  /** Logged-in user with profile (updates on login / logout / profile change) */
  readonly user = signal<UserInfo | null>(null);

  /** "First Last" from the profile, else the login name */
  readonly displayName = computed(() => {
    this.user(); // re-evaluate when the user changes
    return this.authService.getDisplayName() || 'User';
  });

  /** Login name of the current user (kept for templates that still bind to it) */
  get loginName(): string {
    return this.user()?.loginName || this.authService.getLoginName() || 'User';
  }

  readonly email = computed(() => this.user()?.userProfile?.email || '');

  readonly phoneNumber = computed(() => this.user()?.userProfile?.phoneNumber || '');

  /** Profile image data URL, or null to show initials */
  readonly avatarUrl = computed(() => {
    this.user();
    return this.authService.getProfileImageUrl();
  });

  /** Up to two initials for the avatar when there is no image */
  readonly initials = computed(() => {
    const profile = this.user()?.userProfile;
    const parts = profile
      ? [profile.firstName, profile.lastName]
      : this.displayName().split(' ');
    return parts
      .filter(Boolean)
      .slice(0, 2)
      .map(p => p.trim().charAt(0).toUpperCase())
      .join('') || '?';
  });

  constructor(
    public globals: ThemeOptions,
    private authService: AuthService
  ) {
    this.authService.currentUser$
      .pipe(takeUntilDestroyed(inject(DestroyRef)))
      .subscribe(user => this.user.set(user));
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
