import { Component, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Component({
  selector: 'app-login-boxed',
  templateUrl: './login-boxed.component.html',
  styleUrls: ['./login-boxed.component.scss'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Eager
})
export class LoginBoxedComponent implements OnInit {
  isArabic$: Observable<boolean>;

  constructor(
    private translateService: TranslateService,
    private languageService: LanguageService
  ) {
    this.isArabic$ = this.languageService.currentLanguage$.pipe(
      map(lang => lang === 'ar')
    );
  }

  ngOnInit(): void {
    // Component initialization if needed
  }

  onSubmit() {
    // Handle login form submission
  }

  onForgotPassword() {
    // Handle forgot password action
  }

}
