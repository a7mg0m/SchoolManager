import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Observable } from 'rxjs';

/**
 * Example component demonstrating how to use translations and language switching
 * 
 * Usage:
 * - In templates: {{ 'key.name' | translate }}
 * - In components: Inject TranslateService or LanguageService
 * - For reactive updates: Subscribe to currentLanguage$
 */

@Component({
  selector: 'app-example-translations',
  template: `
    <div [dir]="(isArabic$ | async) ? 'rtl' : 'ltr'" class="example-container">
      <!-- Example 1: Simple template translation -->
      <h2>{{ 'dashboard.title' | translate }}</h2>
      <p>{{ 'dashboard.welcome' | translate }}</p>

      <!-- Example 2: Using in button -->
      <div class="button-group">
        <button (click)="saveData()" class="btn btn-primary">
          {{ 'common.save' | translate }}
        </button>
        <button (click)="cancel()" class="btn btn-secondary">
          {{ 'common.cancel' | translate }}
        </button>
      </div>

      <!-- Example 3: Conditional rendering based on language -->
      <div *ngIf="(isArabic$ | async)">
        <p>هذا النص يظهر فقط في النسخة العربية</p>
      </div>

      <!-- Example 4: Dynamic content with translations -->
      <div class="info-box">
        <strong>{{ 'common.info' | translate }}:</strong>
        <span>{{ currentLanguageName$ | async }}</span>
      </div>

      <!-- Example 5: Form with translated labels and validation -->
      <form>
        <div class="form-group">
          <label>{{ 'auth.email' | translate }}:</label>
          <input type="email" placeholder="{{ 'auth.email' | translate }}" />
        </div>
        <div class="form-group">
          <label>{{ 'auth.password' | translate }}:</label>
          <input type="password" placeholder="{{ 'auth.password' | translate }}" />
        </div>
      </form>

      <!-- Language Selector (Alternative to header switcher) -->
      <div class="language-selector mt-3">
        <button 
          (click)="setLanguage('en')"
          [class.active]="!(isArabic$ | async)"
          class="btn btn-sm">
          English
        </button>
        <button 
          (click)="setLanguage('ar')"
          [class.active]="(isArabic$ | async)"
          class="btn btn-sm">
          العربية
        </button>
      </div>
    </div>
  `,
  styleUrls: ['./example-translations.component.scss'],
  standalone: false
})
export class ExampleTranslationsComponent implements OnInit {
  isArabic$: Observable<boolean>;
  currentLanguageName$: Observable<string>;

  constructor(
    private translate: TranslateService,
    private languageService: LanguageService
  ) {
    // Observable for RTL/LTR checking in template
    this.isArabic$ = this.languageService.currentLanguage$.pipe(
      map(lang => lang === 'ar')
    );

    // Observable for displaying current language name
    this.currentLanguageName$ = this.languageService.currentLanguage$.pipe(
      switchMap(lang => {
        return this.translate.get(lang === 'ar' ? 'languages.arabic' : 'languages.english');
      })
    );
  }

  ngOnInit(): void {
    // Example: Get translated message on init
    this.translate.get('messages.loadingData').subscribe((msg) => {
      console.log('Loading:', msg);
    });

    // Example: Listen to language changes
    this.languageService.currentLanguage$.subscribe((lang) => {
      console.log('Language changed to:', lang);
    });
  }

  // Method example: Translate and show message
  saveData(): void {
    this.translate.get('messages.saved').subscribe((msg) => {
      alert(msg); // Shows "Saved successfully" or "تم الحفظ بنجاح"
    });
  }

  // Method example: Cancel operation
  cancel(): void {
    this.translate.get('common.cancel').subscribe((msg) => {
      console.log('User clicked:', msg);
    });
  }

  // Method example: Set language
  setLanguage(lang: string): void {
    this.languageService.setLanguage(lang);
  }

  // Method example: Toggle language
  toggleLanguage(): void {
    this.languageService.toggleLanguage();
  }

  // Method example: Check current language
  getCurrentLanguage(): string {
    return this.languageService.getCurrentLanguage();
  }

  // Method example: Get multiple translations at once
  getMultipleTranslations(): void {
    this.translate.get([
      'common.save',
      'common.cancel',
      'messages.saved'
    ]).subscribe((translations) => {
      console.log('Save button:', translations['common.save']);
      console.log('Cancel button:', translations['common.cancel']);
      console.log('Success message:', translations['messages.saved']);
    });
  }

  // Method example: Translate with parameters (if implemented)
  getTranslationWithParams(): void {
    // You can extend your translation keys to include parameters
    // Example: {{ 'greeting' | translate:{ name: userName } }}
    const params = { name: 'Ahmed' };
    this.translate.get('greeting', params).subscribe((msg) => {
      console.log(msg); // "Hello Ahmed" or "مرحبا أحمد"
    });
  }
}

// Don't forget to import required operators
// import { map, switchMap } from 'rxjs/operators';
// import { map, switchMap } from 'rxjs';

/**
 * USAGE EXAMPLES IN OTHER COMPONENTS:
 * 
 * 1. SIMPLE TEMPLATE TRANSLATION:
 *    {{ 'dashboard.title' | translate }}
 * 
 * 2. COMPONENT TYPESCRIPT:
 *    constructor(private translate: TranslateService) {}
 *    
 *    this.translate.get('common.save').subscribe((msg) => {
 *      console.log(msg);
 *    });
 * 
 * 3. LANGUAGE SWITCHING:
 *    constructor(private languageService: LanguageService) {}
 *    
 *    this.languageService.setLanguage('ar');
 *    this.languageService.toggleLanguage();
 * 
 * 4. REACTIVE LANGUAGE CHANGES:
 *    this.languageService.currentLanguage$.subscribe((lang) => {
 *      // Update UI when language changes
 *    });
 * 
 * 5. CONDITIONAL TEXT:
 *    {{ (languageService.currentLanguage$ | async) === 'ar' ? 'نص عربي' : 'English text' }}
 * 
 * 6. RTL/LTR STYLING:
 *    [dir]="(isArabic$ | async) ? 'rtl' : 'ltr'"
 */
