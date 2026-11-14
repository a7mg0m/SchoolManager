import {Component, HostListener, OnInit, OnDestroy, afterNextRender, ChangeDetectionStrategy, effect, signal} from '@angular/core';
import {ThemeOptions} from '../../../theme-options';
import {Observable, Subject} from 'rxjs';
import {filter, takeUntil} from 'rxjs/operators';
import { ConfigService } from '../../../ThemeOptions/store/config.service';
import { ConfigState } from '../../../ThemeOptions/store/config.state';
import { LanguageService } from '../../../ThemeOptions/store/language.service';
import {NavigationEnd, Router} from '@angular/router';
import { MenuPermissionService } from '../../../core/services/menu-permission.service';
import { PermissionMenuItem } from '../../../core/models/group-permission.model';
import { SchoolInfoService } from '../../../core/services/school-info.service';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    /* Override the existing styles with important to ensure animation works */
    .vsm-dropdown {
      max-height: 0 !important;
      overflow: hidden !important;
      opacity: 0 !important;
      transition: max-height 0.3s ease-in-out, opacity 0.3s ease-in-out !important;
      position: relative !important;
    }

    .vsm-dropdown-show {
      max-height: 1000px !important;
      opacity: 1 !important;
    }

    /* Arrow rotation - override existing transform */
    .vsm-item.has-sub .vsm-arrow {
      transition: transform 0.3s ease !important;
      transform: rotate(270deg) !important;  /* Point right */
    }

    .vsm-item.has-sub.vsm-open .vsm-arrow {
      transform: rotate(360deg) !important;  /* Point down */
    }

    /* Logo + app name at the top of the sidebar */
    .sidebar-logo {
      width: auto !important;
      height: auto !important;
      gap: .6rem;
      text-decoration: none;
    }

    .sidebar-logo__img {
      width: 42px;
      height: 42px;
      object-fit: contain;
      flex-shrink: 0;
    }

    .sidebar-logo .logo-text {
      font-size: 16px !important;
      min-width: 0;
    }

    /* Long school names: cut with "…" (full name in the tooltip) */
    .sidebar-logo .logo-full {
      max-width: 170px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .vsm-state {
      padding: .75rem 1.5rem;
      opacity: .6;
      font-size: .85rem;
    }
  `]
})
export class SidebarComponent implements OnInit, OnDestroy {
  /** Ids of the menu groups that are expanded */
  public openMenus: string[] = [];

  public config$: Observable<ConfigState>;
  public currentLanguage = signal<string>('en');

  private destroy$ = new Subject<void>();

  constructor(
    public globals: ThemeOptions,
    public menu: MenuPermissionService,
    private router: Router,
    private configService: ConfigService,
    private languageService: LanguageService,
    private schoolInfoService: SchoolInfoService
  ) {
    this.config$ = this.configService.config$;
    this.schoolInfoService.loadCurrent();

    // When the menu arrives (or changes), expand the group of the current page
    effect(() => {
      if (this.menu.menuItems().length) {
        this.openActiveGroup(this.router.url);
      }
    });

    afterNextRender(() => {
      this.innerWidth = window.innerWidth;
      if (this.innerWidth < 1200) {
        this.globals.toggleSidebar.set(true);
      }
    });
  }

  private newInnerWidth = 0;
  private innerWidth = 0;

  get menuItems(): PermissionMenuItem[] {
    return this.menu.menuItems();
  }

  toggleSidebar() {
    this.globals.toggleSidebar.set(!this.globals.toggleSidebar());
    if (this.globals.toggleSidebar()) {
      this.globals.sidebarHover.set(false);
    }
  }

  onSidebarMouseEnter() {
    if (this.globals.toggleSidebar()) {
      this.globals.sidebarHover.set(true);
    }
  }

  onSidebarMouseLeave() {
    if (this.globals.toggleSidebar()) {
      this.globals.sidebarHover.set(false);
    }
  }

  ngOnInit() {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe((language) => this.currentLanguage.set(language));

    // Keep the group of the current page open (also on back / forward and links from other pages)
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((e) => this.openActiveGroup(e.urlAfterRedirects));
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleSubmenu(menuId: string, parentIds: string[] = []) {
    // Toggle submenu: close if open, open if closed (and close the others, keeping its parents open)
    if (this.openMenus.includes(menuId)) {
      this.openMenus = this.openMenus.filter((id) => id !== menuId);
    } else {
      this.openMenus = [...parentIds, menuId];
    }
  }

  isOpen(item: PermissionMenuItem): boolean {
    return this.openMenus.includes(item.menuItemId);
  }

  hasChildren(item: PermissionMenuItem): boolean {
    return !!item.children?.length;
  }

  /** True when the current page is this item or one of its descendants (highlights the group) */
  isActiveBranch(item: PermissionMenuItem): boolean {
    return this.findPath(this.menuItems, this.cleanUrl(this.router.url))?.includes(item.menuItemId) ?? false;
  }

  static readonly DEFAULT_LOGO = 'assets/images/logo.png';
  /** School logo that failed to load (broken data) — use the default instead */
  private brokenLogo = signal<string | null>(null);

  /** School Info logo if it has one, otherwise the default logo */
  logoSrc(): string {
    const src = this.schoolInfoService.currentLogoSrc();
    return src && src !== this.brokenLogo() ? src : SidebarComponent.DEFAULT_LOGO;
  }

  onLogoError() {
    const src = this.schoolInfoService.currentLogoSrc();
    if (src) {
      this.brokenLogo.set(src);
    }
  }

  /** School name from School Info in the current language (empty until loaded / not set up) */
  schoolName(): string {
    const info = this.schoolInfoService.current();
    if (!info) {
      return '';
    }
    return (this.currentLanguage() === 'ar' ? info.nameAr || info.nameEn : info.nameEn || info.nameAr) || '';
  }

  getName(item: PermissionMenuItem): string {
    return this.currentLanguage() === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr;
  }

  onNavigate() {
    if (window.innerWidth < 1200) {
      this.globals.toggleSidebarMobile.set(true);
      this.globals.sidebarHover.set(false);
    }
  }

  @HostListener('window:resize', ['$event'])
  onResize(event: Event) {
    this.newInnerWidth = (event.target as Window).innerWidth;
    this.globals.toggleSidebar.set(this.newInnerWidth < 1200);
  }

  private openActiveGroup(url: string) {
    const path = this.findPath(this.menuItems, this.cleanUrl(url));
    if (path) {
      // Every group on the way to the page (the page itself has no submenu)
      this.openMenus = path.slice(0, -1);
    }
  }

  /**
   * Ids from the top level down to the item whose route matches `url`
   * (exact match, or the longest route that `url` starts with, e.g. /users/group-permissions/123).
   */
  private findPath(items: PermissionMenuItem[], url: string): string[] | null {
    let best: { path: string[]; length: number } | null = null;

    const walk = (list: PermissionMenuItem[], trail: string[]) => {
      for (const item of list) {
        const path = [...trail, item.menuItemId];
        const route = item.route ? this.cleanUrl(item.route) : null;
        if (route && (url === route || url.startsWith(route + '/')) && (!best || route.length > best.length)) {
          best = { path, length: route.length };
        }
        walk(item.children || [], path);
      }
    };
    walk(items, []);

    return best ? (best as { path: string[] }).path : null;
  }

  private cleanUrl(url: string): string {
    return (url.split(/[?#]/)[0].toLowerCase().replace(/\/+$/, '')) || '/';
  }
}
