import { Injectable, computed, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { BehaviorSubject, Observable, Subscription } from 'rxjs';
import { distinctUntilChanged, filter, map, take } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { GroupPermissionService } from './group-permission.service';
import { PermissionFlags, PermissionMenuItem } from '../models/group-permission.model';

/** What a user wants to do on a page */
export type PermissionAction = 'view' | 'add' | 'edit' | 'delete';

const NO_PERMISSIONS: PermissionFlags = { canView: false, canAdd: false, canEdit: false, canDelete: false };

const FLAG_OF: Record<PermissionAction, keyof PermissionFlags> = {
  view: 'canView',
  add: 'canAdd',
  edit: 'canEdit',
  delete: 'canDelete'
};

/**
 * Menu of the logged-in user (from /api/group-permission/my-menu).
 * Loaded on login / page refresh and cleared on logout. The sidebar renders `menuItems`;
 * buttons use `can(action)` (through the *appCan directive) and routes use the permission guard.
 *
 * A page's permissions come from the menu item with the longest route that the URL starts with,
 * so /management/sections/save/1 uses the flags of /management/sections.
 */
@Injectable({
  providedIn: 'root'
})
export class MenuPermissionService {
  /** Menu tree the user may see (parents only included because of visible children) */
  readonly menuItems = signal<PermissionMenuItem[]>([]);
  readonly isFullAccess = signal(false);
  readonly isLoading = signal(false);
  /** True once the menu of the current user has been loaded (successfully or not) */
  readonly isLoaded = signal(false);
  readonly loadFailed = signal(false);
  /** URL of the current page (kept in sync with the router) */
  readonly currentUrl = signal('/');

  /** Emits true when the menu of the signed-in user is loaded and not reloading */
  private readonly ready$ = new BehaviorSubject<boolean>(false);

  /** route -> item (with its flags), for every item in the tree that has a route */
  private readonly permissionsByRoute = computed(() => {
    const result = new Map<string, PermissionMenuItem>();
    const walk = (items: PermissionMenuItem[]) => {
      for (const item of items) {
        if (item.route) {
          result.set(this.normalize(item.route), item);
        }
        walk(item.children || []);
      }
    };
    walk(this.menuItems());
    return result;
  });

  private request?: Subscription;

  constructor(
    private authService: AuthService,
    private groupPermissionService: GroupPermissionService,
    private router: Router
  ) {
    this.currentUrl.set(this.router.url);
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => this.currentUrl.set(e.urlAfterRedirects));

    // Reload whenever the signed-in user changes (login, logout, other account)
    this.authService.currentUser$
      .pipe(
        map((user) => user?.userId ?? null),
        distinctUntilChanged()
      )
      .subscribe((userId) => (userId ? this.load() : this.clear()));
  }

  /** Fetch the menu again (e.g. after the permissions of the user's own group changed) */
  refresh(): void {
    if (this.authService.getCurrentUser()) {
      this.load();
    }
  }

  /** Completes once the menu is loaded (used by the route guard before checking a URL) */
  whenReady(): Observable<void> {
    return this.ready$.pipe(filter(Boolean), take(1), map(() => undefined));
  }

  /**
   * True when the user may do `action` (or any of `actions`) on the page at `route`
   * (default: the current page).
   */
  can(action: PermissionAction | PermissionAction[], route?: string | null): boolean {
    const flags = this.getPermissions(route || this.currentUrl());
    const actions = Array.isArray(action) ? action : [action];
    return actions.some((a) => flags[FLAG_OF[a] ?? 'canView']);
  }

  /** Flags of the page at `route` (closest menu route above it); all false if not allowed */
  getPermissions(route: string): PermissionFlags {
    const item = this.findItem(route);
    return item
      ? { canView: item.canView, canAdd: item.canAdd, canEdit: item.canEdit, canDelete: item.canDelete }
      : NO_PERMISSIONS;
  }

  /** Menu item of the page at `route` (closest menu route above it), or null */
  findItem(route: string): PermissionMenuItem | null {
    const byRoute = this.permissionsByRoute();
    let path = this.normalize(route);
    while (path) {
      const item = byRoute.get(path);
      if (item) {
        return item;
      }
      path = path.substring(0, path.lastIndexOf('/'));
    }
    return null;
  }

  canView(route: string): boolean {
    return this.getPermissions(route).canView;
  }

  /** Route of the first page in the menu (where to land when /home is not allowed) */
  firstRoute(): string | null {
    const find = (items: PermissionMenuItem[]): string | null => {
      for (const item of items) {
        if (item.route && item.canView) {
          return item.route;
        }
        const child = find(item.children || []);
        if (child) {
          return child;
        }
      }
      return null;
    };
    return find(this.menuItems());
  }

  private load(): void {
    this.request?.unsubscribe();
    this.isLoading.set(true);
    this.loadFailed.set(false);
    this.ready$.next(false);

    this.request = this.groupPermissionService.getMyMenu().subscribe({
      next: (response) => {
        const menu = response?.status === 200 ? response.data : null;
        this.menuItems.set(menu?.menuItems ?? []);
        this.isFullAccess.set(!!menu?.isFullAccess);
        this.loadFailed.set(!menu);
        this.finish();
      },
      error: () => {
        this.menuItems.set([]);
        this.isFullAccess.set(false);
        this.loadFailed.set(true);
        this.finish();
      }
    });
  }

  private finish(): void {
    this.isLoading.set(false);
    this.isLoaded.set(true);
    this.ready$.next(true);
  }

  private clear(): void {
    this.request?.unsubscribe();
    this.menuItems.set([]);
    this.isFullAccess.set(false);
    this.isLoading.set(false);
    this.isLoaded.set(false);
    this.loadFailed.set(false);
    this.ready$.next(false);
  }

  /** "/Users/Employees/?x=1#a" -> "/users/employees" */
  private normalize(route: string): string {
    let path = route.split(/[?#]/)[0].trim().toLowerCase();
    if (!path.startsWith('/')) {
      path = '/' + path;
    }
    return path.replace(/\/+$/, '');
  }
}
