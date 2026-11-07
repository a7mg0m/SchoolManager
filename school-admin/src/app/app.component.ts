import {Component, ChangeDetectionStrategy} from '@angular/core';
import { LanguageService } from './ThemeOptions/store/language.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
export class AppComponent {
  title = 'ArchitectUI - Angular 20 Bootstrap 5 & Material Design Admin Dashboard Template';

  constructor(private languageService: LanguageService) {
    // Initialize language on app load
    this.languageService.setLanguage(this.languageService.getCurrentLanguage());
  }
}
