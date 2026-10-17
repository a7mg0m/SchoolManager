import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
  },
  {
    path: 'students',
    loadComponent: () =>
      import('./features/students/students.component').then((m) => m.StudentsComponent),
  },
  {
    path: 'classes',
    loadComponent: () =>
      import('./features/classes/classes.component').then((m) => m.ClassesComponent),
  },
  { path: '**', redirectTo: '' },
];
