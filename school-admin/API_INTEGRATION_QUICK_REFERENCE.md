# API Integration Quick Reference Guide

## Quick Start

### 1. Make API Requests
```typescript
import { ApiService } from './core/services';

constructor(private api: ApiService) {}

// GET request
this.api.get<UserResponse>('/api/users/profile').subscribe(
  data => console.log(data),
  error => console.error(error)
);

// POST request
this.api.post<LoginResponse>('/api/user/login', {
  loginName: 'user1',
  password: 'password'
}).subscribe(
  data => console.log(data),
  error => console.error(error)
);

// With query parameters
this.api.get<any>('/api/users', {
  page: '1',
  limit: '10'
}).subscribe(...);
```

### 2. Handle Authentication
```typescript
import { AuthService } from './core/services';

constructor(private auth: AuthService) {}

// Login
this.auth.login({ loginName: 'user1', password: 'password' })
  .subscribe(
    response => { /* success */ },
    error => { /* error */ }
  );

// Check if authenticated
if (this.auth.isAuthenticated()) {
  console.log('User is logged in');
}

// Get current user
const user = this.auth.getCurrentUser();

// Logout
this.auth.logout();

// Subscribe to auth state changes
this.auth.isAuthenticated$.subscribe(isAuth => {
  console.log('Auth state:', isAuth);
});
```

### 3. Display Loading State
```typescript
import { LoadingService } from './core/services';

constructor(private loading: LoadingService) {}

// Template
<div *ngIf="loading.loading$ | async">Loading...</div>

// Component
public loading$ = this.loading.loading$;
```

### 4. Protect Routes
```typescript
// In routing module - already applied
{
  path: 'dashboard',
  component: DashboardComponent,
  canActivate: [AuthGuard]
}
```

## Common Patterns

### Login Flow
```typescript
// In login component
this.authService.login(credentials)
  .pipe(takeUntil(this.destroy$))
  .subscribe({
    next: (response) => {
      // Automatically stored by AuthService
      // Navigate to dashboard
      this.router.navigate(['/home']);
    },
    error: (error) => {
      this.loginError = 'Invalid credentials';
    }
  });
```

### Authenticated API Call
```typescript
// Token is automatically added by AuthInterceptor
this.api.get<UserData>('/api/users/profile')
  .subscribe(
    data => console.log(data), // Request included Authorization header
    error => { /* handled by ErrorInterceptor */ }
  );
```

### Handling Errors
```typescript
this.api.post('/api/data', payload)
  .subscribe({
    next: (response) => { /* success */ },
    error: (error) => {
      switch (error.status) {
        case 401:
          // Already handled by ErrorInterceptor - user is logged out
          break;
        case 422:
          // Validation errors
          const errors = error.error.errors;
          break;
        case 500:
          // Server error
          break;
      }
    }
  });
```

## File Locations for Common Tasks

### Add a New API Endpoint
1. Create model: `src/app/core/models/my-model.ts`
2. Export from: `src/app/core/models/index.ts`
3. Create service method in feature service that uses `ApiService`

### Add a Public Endpoint (no authentication)
1. Update `src/app/core/interceptors/auth.interceptor.ts`
2. Add endpoint to `publicEndpoints` array

### Change API Gateway URL
1. Update `src/environments/environment.ts`
2. Update `src/environments/environment.prod.ts`

### Add API Error Handling
1. Update `src/app/core/interceptors/error.interceptor.ts`
2. Add new status code case
3. Call `this.showNotification()` with user message

### Add Loading Indicator Bypass
1. Update `src/app/core/interceptors/loading.interceptor.ts`
2. Add endpoint to `bypassLoadingEndpoints` array

## Troubleshooting

### Issue: Login fails with "Network error"
**Solution:** Check if API gateway is running at `http://localhost:53607`

### Issue: Token not being sent
**Solution:** Ensure endpoint is not in `publicEndpoints` in AuthInterceptor

### Issue: Session doesn't persist after page refresh
**Solution:** SessionStorage is automatically cleared - this is by design for security. Use AuthService to check `isAuthenticated()` on app startup.

### Issue: Multiple loading spinners shown
**Solution:** LoadingInterceptor uses reference counting - ensure requests complete properly

### Issue: Infinite redirect loop on 401
**Solution:** ErrorInterceptor includes redirect loop prevention - verify logout completes

## Translation Keys

### Auth Keys (for use in templates)
- `auth.loginName` - Login Name label
- `auth.enterLoginName` - Placeholder text
- `auth.password` - Password label
- `auth.enterPassword` - Placeholder text
- `auth.keepLoggedIn` - Remember me checkbox label
- `auth.welcomeBack` - Welcome message
- `auth.signInMessage` - Sign in instruction

### Validation Keys
- `validation.fieldRequired` - Required field error
- `validation.minLength` - Minimum length error (uses {{min}} placeholder)

### Common Keys
- `common.loading` - Loading text
- `common.error` - Error header
- `common.success` - Success message

## Integration Checklist

- [x] API gateway URL configured in environment files
- [x] CoreModule created with all services and interceptors
- [x] AuthService implements login/logout
- [x] AuthGuard protects routes
- [x] Login component uses reactive forms
- [x] Error interceptor handles common HTTP errors
- [x] Loading interceptor manages loading state
- [x] Auth interceptor adds token to requests
- [x] Translation keys added for login form
- [x] Build completes without errors

## Next Steps

1. **Test Login:** Start the app and test login with credentials
2. **Monitor Network:** Open browser DevTools Network tab to verify:
   - POST request to `http://localhost:53607/api/user/login`
   - `Content-Type: application/json`
   - Authorization header in subsequent requests
3. **Integrate Notifications:** Replace console.log in ErrorInterceptor with toast service
4. **Add More Endpoints:** Create feature-specific services using ApiService
5. **Implement Refresh Token:** Add token refresh logic for production

## Support

For detailed information, see `API_INTEGRATION_IMPLEMENTATION.md`
