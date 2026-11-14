import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateChild, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { MenuPermissionService, PermissionAction } from '../services/menu-permission.service';

/**
 * Page access from the group permissions of the logged-in user.
 *
 * Route data:
 *   permission: 'view' (default) | 'add' | 'edit' | 'delete' — what the page needs
 *   permission: false — any signed-in user (e.g. My Profile)
 *
 * The page's flags come from the closest menu route above the URL
 * (e.g. /management/sections/save/1 -> /management/sections).
 * A denied /home goes to the first allowed page instead (landing page after login).
 */
@Injectable({
  providedIn: 'root'
})
export class PermissionGuard implements CanActivateChild {

  constructor(
    private authService: AuthService,
    private permissions: MenuPermissionService,
    private router: Router
  ) {}

  canActivateChild(childRoute: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean | UrlTree> {
    const required = childRoute.data?.['permission'] as PermissionAction | false | undefined;

    // Not signed in: AuthGuard handles it. Open pages: nothing to check.
    if (!this.authService.isAuthenticated() || required === false) {
      return of(true);
    }

    const url = state.url;
    return this.permissions.whenReady().pipe(
      map(() => {
        if (this.permissions.can(required || 'view', url)) {
          return true;
        }

        if (this.isHome(url)) {
          const first = this.permissions.firstRoute();
          if (first) {
            return this.router.parseUrl(first);
          }
        }

        return this.router.createUrlTree([this.authService.NOT_AUTHORIZED_URL], {
          queryParams: { reason: 'forbidden', returnUrl: url }
        });
      })
    );
  }

  private isHome(url: string): boolean {
    const path = url.split(/[?#]/)[0].toLowerCase().replace(/\/+$/, '');
    return path === '' || path === '/home';
  }
}
