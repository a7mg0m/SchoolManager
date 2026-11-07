import { Component, OnInit } from '@angular/core';
import { LanguageService } from '../../../../../ThemeOptions/store/language.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-language-switcher',
  templateUrl: './language-switcher.component.html',
  styleUrls: ['./language-switcher.component.scss'],
  standalone: false
})
export class LanguageSwitcherComponent implements OnInit {
  currentLanguage: string = 'en';

  constructor(private languageService: LanguageService) {}

  ngOnInit(): void {
    this.currentLanguage = this.languageService.getCurrentLanguage();
    
    // Subscribe to language changes
    this.languageService.currentLanguage$.subscribe((lang) => {
      this.currentLanguage = lang;
    });
  }

  toggleLanguage(): void {
    this.languageService.toggleLanguage();
  }

  setLanguage(language: string): void {
    this.languageService.setLanguage(language);
  }

  isArabic(): boolean {
    return this.languageService.isArabic();
  }

  isEnglish(): boolean {
    return this.languageService.isEnglish();
  }
}
