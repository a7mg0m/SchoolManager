import { Injectable, effect, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRouteSnapshot, Data, NavigationEnd, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { filter } from 'rxjs/operators';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { MenuPermissionService } from './menu-permission.service';

/**
 * Browser tab title: "<page> | <app name>", in the current language.
 *
 * Page name:
 * 1. route data `title` (translation key), e.g. login, my profile
 * 2. otherwise the menu item of the page (name from the database), plus "Add" / "Edit"
 *    for routes with data `permission: 'add' | 'edit'` (the save pages)
 * 3. otherwise just the app name
 */
@Injectable({
  providedIn: 'root'
})
export class PageTitleService {
  private readonly routeData = signal<Data>({});
  private readonly language = signal('en');
  /** Bumped when translations (re)load, so instant() results are refreshed */
  private readonly translationsVersion = signal(0);

  constructor(
    private router: Router,
    private title: Title,
    private translate: TranslateService,
    private languageService: LanguageService,
    private menu: MenuPermissionService
  ) {
    this.routeData.set(this.deepest(this.router.routerState.snapshot.root).data);
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => this.routeData.set(this.deepest(this.router.routerState.snapshot.root).data));

    this.languageService.currentLanguage$.subscribe((lang) => this.language.set(lang));
    this.translate.onLangChange.subscribe(() => this.translationsVersion.update((v) => v + 1));
    this.translate.onTranslationChange.subscribe(() => this.translationsVersion.update((v) => v + 1));

    effect(() => this.title.setTitle(this.buildTitle()));
  }

  private buildTitle(): string {
    this.translationsVersion(); // dependency: re-run when translations arrive
    const isArabic = this.language() === 'ar';
    const data = this.routeData();
    const appName = this.text('pageTitle.appName', 'School Manager');

    let page: string | null = null;

    if (data['title']) {
      page = this.text(data['title'], '');
    } else {
      const item = this.menu.findItem(this.menu.currentUrl());
      if (item) {
        page = isArabic ? item.nameAr || item.nameEn : item.nameEn || item.nameAr;
        if (data['permission'] === 'add') {
          page += ' - ' + this.text('common.add', 'Add');
        } else if (data['permission'] === 'edit') {
          page += ' - ' + this.text('common.edit', 'Edit');
        }
      }
    }

    return page ? `${page} | ${appName}` : appName;
  }

  /** Translation of `key`, or `fallback` while translations are not loaded yet */
  private text(key: string, fallback: string): string {
    const value = this.translate.instant(key);
    return value && value !== key ? value : fallback || key;
  }

  private deepest(route: ActivatedRouteSnapshot): ActivatedRouteSnapshot {
    while (route.firstChild) {
      route = route.firstChild;
    }
    return route;
  }
}
