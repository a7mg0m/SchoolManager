import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  private currentLanguage = new BehaviorSubject<string>('en');
  private readonly LANGUAGE_KEY = 'selectedLanguage';
  private readonly SUPPORTED_LANGUAGES = ['en', 'ar'];
  private readonly DEFAULT_LANGUAGE = 'en';

  currentLanguage$: Observable<string> = this.currentLanguage.asObservable();

  constructor(private translateService: TranslateService) {
    this.initializeLanguage();
  }

  /**
   * Initialize language from localStorage or use default
   */
  private initializeLanguage(): void {
    const savedLanguage = localStorage.getItem(this.LANGUAGE_KEY) || this.DEFAULT_LANGUAGE;
    this.setLanguage(savedLanguage);
  }

  /**
   * Set the application language
   */
  setLanguage(language: string): void {
    if (this.SUPPORTED_LANGUAGES.includes(language)) {
      this.currentLanguage.next(language);
      this.translateService.use(language);
      localStorage.setItem(this.LANGUAGE_KEY, language);
      this.applyTextDirection(language);
    }
  }

  /**
   * Get the current language
   */
  getCurrentLanguage(): string {
    return this.currentLanguage.value;
  }

  /**
   * Toggle between English and Arabic
   */
  toggleLanguage(): void {
    const newLanguage = this.getCurrentLanguage() === 'en' ? 'ar' : 'en';
    this.setLanguage(newLanguage);
  }

  /**
   * Check if current language is Arabic
   */
  isArabic(): boolean {
    return this.getCurrentLanguage() === 'ar';
  }

  /**
   * Check if current language is English
   */
  isEnglish(): boolean {
    return this.getCurrentLanguage() === 'en';
  }

  /**
   * Get text direction (RTL for Arabic, LTR for English)
   */
  getTextDirection(): 'rtl' | 'ltr' {
    return this.isArabic() ? 'rtl' : 'ltr';
  }

  /**
   * Apply text direction to the document
   */
  private applyTextDirection(language: string): void {
    const direction = language === 'ar' ? 'rtl' : 'ltr';
    document.documentElement.dir = direction;
    document.documentElement.lang = language;
    document.body.classList.toggle('rtl', language === 'ar');
    document.body.classList.toggle('ltr', language === 'en');
  }

  /**
   * Get all supported languages
   */
  getSupportedLanguages(): string[] {
    return this.SUPPORTED_LANGUAGES;
  }
}
