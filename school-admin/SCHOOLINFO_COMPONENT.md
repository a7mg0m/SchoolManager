# SchoolInfo Component - Implementation Guide

## Overview
The SchoolInfo component provides a complete interface for managing school information. It supports creating new school records, fetching existing data, and updating school information through API calls.

## Files Created

### 1. **Model** (`src/app/core/models/school-info.model.ts`)
Defines TypeScript interfaces for:
- `SchoolInfo` - The main school data model
- `CreateSchoolInfoRequest` - Request payload for creating school info
- `UpdateSchoolInfoRequest` - Request payload for updating school info
- `SchoolInfoSingleResponse` - API response wrapper
- `SchoolInfoResponse` - Generic API response wrapper

### 2. **Service** (`src/app/core/services/school-info.service.ts`)
Provides three main methods:
- `get()` - Fetches existing school info from `{{gatewayUrl}}/api/school-info/get`
- `create(request)` - Creates new school info at `{{gatewayUrl}}/api/school-info/create`
- `update(request)` - Updates existing school info at `{{gatewayUrl}}/api/school-info/update`

### 3. **Component** (`src/app/management/school-info/school-info.component.ts`)
Features:
- Reactive form with validation for all fields
- Auto-loads existing school info on component init
- Automatically detects edit vs. create mode
- Real-time validation feedback
- Loading, success, and error states
- Proper cleanup with RxJS unsubscribe pattern

### 4. **Template** (`src/app/management/school-info/school-info.component.html`)
- Responsive form layout (Bootstrap 5)
- Modern control flow syntax (@if, @else)
- Field validation messages
- Loading spinner
- Success/error alerts
- Submit and reset buttons

### 5. **Styles** (`src/app/management/school-info/school-info.component.scss`)
- Bootstrap 5 integration
- Responsive design
- Smooth transitions and hover effects

## Form Fields

| Field | Type | Validation | Required |
|-------|------|-----------|----------|
| name | Text | 2-200 chars | Yes |
| address | Text | 5-300 chars | Yes |
| city | Text | 2-100 chars | Yes |
| state | Text | 2-100 chars | Yes |
| postalCode | Text | 4-10 digits | Yes |
| country | Text | 2-100 chars | Yes |
| phone | Tel | Pattern validated | Yes |
| email | Email | Valid email format | Yes |
| website | URL | URL pattern (optional) | No |
| principal | Text | 2-200 chars | Yes |
| establishedYear | Number | 4-digit year | Yes |
| logo | URL | Free text (optional) | No |

## Usage in Routing

Add this to your `app-routing.module.ts`:

```typescript
import { SchoolInfoComponent } from './components.barrel';

const routes: Routes = [
  {
    path: 'management',
    component: BaseLayoutComponent,
    children: [
      // ... other routes
      {
        path: 'school-info',
        component: SchoolInfoComponent
      }
    ]
  }
];
```

## Component Behavior

### On Initialization
1. Initializes the form with validation rules
2. Makes a GET request to fetch existing school info
3. If data exists: enters edit mode and populates the form
4. If no data exists (404): shows empty form in create mode

### On Form Submit
- **Edit Mode**: Calls the `update()` service method
- **Create Mode**: Calls the `create()` service method
- Both operations show loading state and success/error messages

### Error Handling
- 404 responses are silently handled (no error shown for non-existent data)
- Other errors display user-friendly error messages
- Form validation prevents submission of invalid data

## API Contract

### Get School Info
```
GET {{gatewayUrl}}/api/school-info/get

Response:
{
  "success": true,
  "data": {
    "id": "string",
    "name": "string",
    "address": "string",
    "city": "string",
    "state": "string",
    "postalCode": "string",
    "country": "string",
    "phone": "string",
    "email": "string",
    "website": "string (optional)",
    "principal": "string",
    "establishedYear": 2024,
    "logo": "string (optional)"
  },
  "message": "string (optional)"
}
```

### Create School Info
```
POST {{gatewayUrl}}/api/school-info/create

Request:
{
  "name": "string",
  "address": "string",
  "city": "string",
  "state": "string",
  "postalCode": "string",
  "country": "string",
  "phone": "string",
  "email": "string",
  "website": "string (optional)",
  "principal": "string",
  "establishedYear": 2024,
  "logo": "string (optional)"
}

Response: Same as Get
```

### Update School Info
```
PUT {{gatewayUrl}}/api/school-info/update

Request:
{
  "id": "string",
  "name": "string",
  "address": "string",
  "city": "string",
  "state": "string",
  "postalCode": "string",
  "country": "string",
  "phone": "string",
  "email": "string",
  "website": "string (optional)",
  "principal": "string",
  "establishedYear": 2024,
  "logo": "string (optional)"
}

Response: Same as Get
```

## Module Integration

The component is already:
- ✅ Declared in `ManagementModule`
- ✅ Exported from `ManagementModule`
- ✅ Exported in `components.barrel.ts`
- ✅ Build verified and successful

## Next Steps

1. **Add routing**: Add the component route to your `app-routing.module.ts`
2. **Add navigation**: Link to the school-info route from your navigation/menu
3. **Customize**: Modify validation rules or fields as needed for your API
4. **Testing**: Test with your backend API endpoints

## Dependencies

The component uses:
- Angular 22 (zoneless compatible)
- Reactive Forms
- RxJS for async operations
- Bootstrap 5 for styling
- Signals for state management (loading, saving, etc.)
