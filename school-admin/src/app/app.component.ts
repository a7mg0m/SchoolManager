import {Component, ChangeDetectionStrategy} from '@angular/core';
import { LanguageService } from './ThemeOptions/store/language.service';
import { PageTitleService } from './core/services/page-title.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
export class AppComponent {
  constructor(
    private languageService: LanguageService,
    // Keeps the browser tab title in sync with the current page
    private pageTitle: PageTitleService
  ) {
    // Initialize language on app load
    this.languageService.setLanguage(this.languageService.getCurrentLanguage());
  }
}
