import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { TranslatePipe } from '@ngx-translate/core';

// Shared Components
import { PageTitleComponent } from './Layout/Components/page-title/page-title.component';
import { LanguageSwitcherComponent } from './Layout/Components/header/elements/language-switcher/language-switcher.component';

// Permission directives (*appCan / [appCanDisable])
import { CanDirective, CanDisableDirective } from './core/directives/can.directive';

@NgModule({
  declarations: [
    PageTitleComponent,
    LanguageSwitcherComponent,
    CanDirective,
    CanDisableDirective
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    NgbModule,
    FontAwesomeModule,
    TranslatePipe
  ],
  exports: [
    PageTitleComponent,
    LanguageSwitcherComponent,
    CanDirective,
    CanDisableDirective,
    // Re-export commonly used modules for convenience
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    NgbModule,
    FontAwesomeModule,
    TranslatePipe
  ]
})
export class SharedModule { }