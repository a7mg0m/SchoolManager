# Translation Guide - Arabic & English Localization

This guide explains how to use the multi-language localization system in the School Admin Dashboard.

## Overview

The application supports **English (EN)** and **Arabic (AR)** with automatic RTL/LTR switching. The system uses:
- **@ngx-translate/core** - Translation service
- **@ngx-translate/http-loader** - Loads translation files from JSON
- **LocalStorage** - Persists language preference

## Translation Files

Translation files are located in `src/assets/i18n/`:

```
src/assets/i18n/
├── en.json      # English translations
└── ar.json      # Arabic translations
```

### Structure

Translations are organized by feature for easy management:

```json
{
  "common": { ... },
  "header": { ... },
  "sidebar": { ... },
  "dashboard": { ... },
  "management": { ... },
  "forms": { ... },
  "auth": { ... },
  "messages": { ... }
}
```

## Using Translations

### In Templates

Use the `translate` pipe to display translations:

```html
<!-- Simple translation -->
<h1>{{ 'dashboard.title' | translate }}</h1>

<!-- With dynamic parameter -->
<p>{{ 'messages.loadingData' | translate }}</p>

<!-- Button -->
<button class="btn btn-primary">
  {{ 'common.save' | translate }}
</button>
```

### In Components

Inject `TranslateService` to work with translations in TypeScript:

```typescript
import { TranslateService } from '@ngx-translate/core';

export class MyComponent implements OnInit {
  constructor(private translate: TranslateService) {}

  ngOnInit() {
    // Get a translation value
    this.translate.get('dashboard.title').subscribe((res: string) => {
      console.log(res);
    });
  }

  showMessage() {
    // Display translated message
    this.translate.get('messages.saved').subscribe((res: string) => {
      alert(res);
    });
  }
}
```

## Language Switching

### Using LanguageService

Inject `LanguageService` in your component:

```typescript
import { LanguageService } from './ThemeOptions/store/language.service';

export class MyComponent {
  constructor(private languageService: LanguageService) {}

  changeLanguage() {
    // Set specific language
    this.languageService.setLanguage('ar'); // Arabic
    this.languageService.setLanguage('en'); // English
  }

  toggleLanguage() {
    // Toggle between EN and AR
    this.languageService.toggleLanguage();
  }

  checkLanguage() {
    // Check current language
    const current = this.languageService.getCurrentLanguage(); // 'en' or 'ar'
    
    if (this.languageService.isArabic()) {
      // Do something for Arabic
    }

    if (this.languageService.isEnglish()) {
      // Do something for English
    }
  }

  getDirection() {
    // Get text direction
    const direction = this.languageService.getTextDirection(); // 'rtl' or 'ltr'
  }

  // Subscribe to language changes
  subscribeToLanguageChanges() {
    this.languageService.currentLanguage$.subscribe((lang) => {
      console.log('Language changed to:', lang);
    });
  }
}
```

### Built-in Language Switcher

The language switcher is available in the header (EN/AR buttons). Click to toggle language immediately.

## LanguageService API

### Methods

| Method | Description | Returns |
|--------|-------------|---------|
| `setLanguage(language: string)` | Set the application language | void |
| `getCurrentLanguage()` | Get current language code | string |
| `toggleLanguage()` | Switch between EN and AR | void |
| `isArabic()` | Check if current language is Arabic | boolean |
| `isEnglish()` | Check if current language is English | boolean |
| `getTextDirection()` | Get text direction (RTL/LTR) | 'rtl' \| 'ltr' |
| `getSupportedLanguages()` | Get all supported languages | string[] |
| `setDefaultLanguage(language: string)` | Set default language | void |

### Observable

```typescript
// Subscribe to language changes
languageService.currentLanguage$.subscribe((lang) => {
  console.log('Current language:', lang); // 'en' or 'ar'
});
```

## Adding New Translations

### 1. Add to Translation Files

**src/assets/i18n/en.json:**
```json
{
  "newFeature": {
    "title": "New Feature",
    "description": "This is a new feature"
  }
}
```

**src/assets/i18n/ar.json:**
```json
{
  "newFeature": {
    "title": "ميزة جديدة",
    "description": "هذه ميزة جديدة"
  }
}
```

### 2. Use in Template

```html
<h2>{{ 'newFeature.title' | translate }}</h2>
<p>{{ 'newFeature.description' | translate }}</p>
```

## RTL/LTR Automatic Switching

When language changes to Arabic:
- Document direction automatically changes to RTL
- Body class toggles to `rtl`
- Document lang attribute updates to `ar`
- All RTL styles from `styles.scss` apply automatically

When language changes to English:
- Document direction automatically changes to LTR
- Body class toggles to `ltr`
- Document lang attribute updates to `en`
- All LTR styles apply automatically

## RTL/LTR Aware Styling

### Global RTL Adjustments

The `styles.scss` includes comprehensive RTL support:

- Sidebar positioning
- Margin/padding flipping (ms → me for RTL)
- Text alignment (text-start → text-end)
- Icon mirroring (chevron-right rotates)
- Dropdown positioning
- Form alignment
- Table alignment
- List indentation

### Component-Level RTL Styling

Use `[dir="rtl"]` and `[dir="ltr"]` selectors in component SCSS:

```scss
[dir="rtl"] {
  .my-component {
    padding-right: 20px;
    padding-left: 0;
  }
}

[dir="ltr"] {
  .my-component {
    padding-left: 20px;
    padding-right: 0;
  }
}
```

## Language Persistence

The selected language is saved to **localStorage** with key `selectedLanguage`. On app reload, the previously selected language is restored automatically.

```typescript
// Manual localStorage access (usually not needed)
localStorage.getItem('selectedLanguage'); // 'en' or 'ar'
localStorage.setItem('selectedLanguage', 'ar');
localStorage.removeItem('selectedLanguage'); // Reset to default
```

## Translation Best Practices

### 1. Organize by Feature
Keep translations grouped by feature/page for scalability:
```json
{
  "dashboard": { "title": "...", "welcome": "..." },
  "management": { "stages": "...", "grades": "..." },
  "forms": { "validation": { ... } }
}
```

### 2. Use Consistent Key Naming
- Use camelCase for keys
- Use dot notation for nesting
- Examples: `common.save`, `dashboard.title`, `forms.validation.required`

### 3. Avoid Hardcoded Strings
Always use translations, even for common words like "Save", "Cancel", "Delete"

### 4. Test Both Languages
Always test UI with both English and Arabic text:
- Arabic text is typically longer
- RTL can affect layout
- Font rendering may differ

### 5. Add Context Comments (Optional)
```json
{
  "dashboard": {
    "totalStudents": "Total Students",
    "_comment": "Shown in the dashboard overview section"
  }
}
```

## Adding More Languages

To add a new language (e.g., French):

### 1. Create Translation File
Create `src/assets/i18n/fr.json` with all translation keys

### 2. Update LanguageService

Open `src/app/ThemeOptions/store/language.service.ts` and update:

```typescript
private readonly SUPPORTED_LANGUAGES = ['en', 'ar', 'fr'];
```

### 3. Update Language Switcher (Optional)

Add a button to `language-switcher.component.html`:

```html
<button 
  class="lang-btn" 
  [class.active]="currentLanguage === 'fr'"
  (click)="setLanguage('fr')"
  title="Français">
  FR
</button>
```

## Troubleshooting

### Translations Not Showing

1. **Check translation key exists**: Verify key in `en.json` and `ar.json`
2. **Clear browser cache**: Hard refresh with `Ctrl+Shift+R`
3. **Check browser console**: Look for translation loading errors
4. **Verify file paths**: Ensure JSON files are in `src/assets/i18n/`

### RTL Not Working

1. **Clear localStorage**: Remove `selectedLanguage` from storage
2. **Hard refresh**: `Ctrl+Shift+R` to clear styles cache
3. **Check document direction**: Open DevTools and verify `<html dir="rtl">`
4. **Check body class**: Verify `<body class="rtl">` or `<body class="ltr">`

### Language Not Persisting

1. **Check localStorage**: Open DevTools → Application → LocalStorage
2. **Verify LanguageService**: Ensure it's provided in app.module.ts
3. **Check browser settings**: Some browsers restrict localStorage in private mode

### Missing Translations

1. **Check console errors**: Angular logs missing translation keys
2. **Add missing keys**: Add any missing translations to both `en.json` and `ar.json`
3. **Default fallback**: Missing keys display as key name in square brackets

## Example Component

Complete example showing translations in both template and component:

**my-feature.component.ts:**
```typescript
import { Component } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { LanguageService } from './ThemeOptions/store/language.service';

@Component({
  selector: 'app-my-feature',
  templateUrl: './my-feature.component.html',
  styleUrls: ['./my-feature.component.scss'],
  standalone: false
})
export class MyFeatureComponent {
  isArabic$ = this.languageService.currentLanguage$.pipe(
    map(lang => lang === 'ar')
  );

  constructor(
    private translate: TranslateService,
    private languageService: LanguageService
  ) {}

  saveData() {
    // Perform save operation
    this.translate.get('messages.saved').subscribe((msg) => {
      alert(msg); // Shows "Saved successfully" or "تم الحفظ بنجاح"
    });
  }
}
```

**my-feature.component.html:**
```html
<div [dir]="(isArabic$ | async) ? 'rtl' : 'ltr'">
  <h2>{{ 'newFeature.title' | translate }}</h2>
  <p>{{ 'newFeature.description' | translate }}</p>
  
  <button class="btn btn-primary" (click)="saveData()">
    {{ 'common.save' | translate }}
  </button>
</div>
```

## Support

For issues or questions about translations, check:
1. Translation files: `src/assets/i18n/`
2. Language service: `src/app/ThemeOptions/store/language.service.ts`
3. Global RTL styles: `src/styles.scss`
4. Component language switcher: `src/app/Layout/Components/header/elements/language-switcher/`
