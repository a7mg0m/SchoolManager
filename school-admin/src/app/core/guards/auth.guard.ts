import { Injectable } from '@angular/core';
import {
  Router,
  CanActivate,
  CanActivateChild,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  UrlTree
} from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Authentication Guard
 * Protects every route (and every child route) that requires a logged-in user.
 * If there is no valid (present and non-expired) token, the session is cleared
 * and the user is sent to the Not Authorized page with a returnUrl.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate, CanActivateChild {

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): boolean | UrlTree {
    return this.check(state.url);
  }

  canActivateChild(
    childRoute: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): boolean | UrlTree {
    return this.check(state.url);
  }

  private check(url: string): boolean | UrlTree {
    if (this.authService.isAuthenticated()) {
      return true;
    }

    // Remove any stale/expired token before leaving
    this.authService.clearSession();

    return this.router.createUrlTree([this.authService.NOT_AUTHORIZED_URL], {
      queryParams: { reason: 'unauthenticated', returnUrl: url }
    });
  }
}
