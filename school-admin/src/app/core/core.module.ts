import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

// Interceptors
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { ErrorInterceptor } from './interceptors/error.interceptor';
import { LoadingInterceptor } from './interceptors/loading.interceptor';

// Services (provided in root, but can also be provided here)
import { ApiService } from './services/api.service';
import { AuthService } from './services/auth.service';
import { LoadingService } from './services/loading.service';

// Guards
import { AuthGuard } from './guards/auth.guard';

/**
 * Core Module
 * 
 * Contains:
 * - HTTP Interceptors (Auth, Error, Loading)
 * - Core Services (API, Auth, Loading)
 * - Route Guards (Auth Guard)
 * 
 * Should be imported only once in AppModule
 * Uses forRoot pattern for provider registration
 */
@NgModule({
  imports: [
    CommonModule,
    HttpClientModule
  ],
  providers: [
    // Services
    ApiService,
    AuthService,
    LoadingService,

    // Guards
    AuthGuard,

    // HTTP Interceptors
    // Order matters: Auth -> Loading -> Error
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: LoadingInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ErrorInterceptor,
      multi: true
    }
  ]
})
export class CoreModule { }
