import {NgModule} from '@angular/core';
import {Routes, RouterModule} from '@angular/router';

import {BaseLayoutComponent} from './Layout/base-layout/base-layout.component';
import {PagesLayoutComponent} from './Layout/pages-layout/pages-layout.component';
import { AuthGuard } from './core/guards/auth.guard';

// Import components from barrel file
import {
  // Dashboard components
  HomeComponent,
  
  // Management components
  StagesComponent,
  GradesComponent,
  AcademicYearListComponent,
  AcademicYearSaveComponent,
  SchoolInfoComponent,
  SectionListComponent,
  SectionSaveComponent,
  TeacherListComponent,
  TeacherSaveComponent,
  SubjectListComponent,
  SubjectSaveComponent,
  ParentListComponent,
  ParentSaveComponent
} from './components.barrel';

// Import user pages
import {UserPagesModule} from './user-pages.module';
import {LoginBoxedComponent} from './UserPages/login-boxed/login-boxed.component';

const routes: Routes = [
  {
    path: 'login',
    component: PagesLayoutComponent,
    children: [
      {path: '', component: LoginBoxedComponent}
    ]
  },
  {
    path: '',
    component: BaseLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      // Dashboards
      {path: '', redirectTo: '/home', pathMatch: 'full'},
      {path: 'home', component: HomeComponent, data: {extraParameter: 'dashboardsMenu'}},

      // Management
      {path: 'management/school-info', component: SchoolInfoComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/stages', component: StagesComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/grades', component: GradesComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/academic-years', component: AcademicYearListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/academic-years/save', component: AcademicYearSaveComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/academic-years/save/:id', component: AcademicYearSaveComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/sections', component: SectionListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/sections/save', component: SectionSaveComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/sections/save/:id', component: SectionSaveComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/teachers', component: TeacherListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/teachers/save', component: TeacherSaveComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/teachers/save/:id', component: TeacherSaveComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/subjects', component: SubjectListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/subjects/save', component: SubjectSaveComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/subjects/save/:id', component: SubjectSaveComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/parents', component: ParentListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/parents/save', component: ParentSaveComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/parents/save/:id', component: ParentSaveComponent, data: {extraParameter: 'managementMenu'}},
    ]
  },
  {
    // Documentation section — lazy-loaded so it stays out of the initial bundle
    path: 'docs',
    loadChildren: () => import('./docs/docs.module').then(m => m.DocsModule)
  },
  {path: '**', redirectTo: '/login'}
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {
    scrollPositionRestoration: 'enabled',
    anchorScrolling: 'enabled'
  }), UserPagesModule],
  exports: [RouterModule]
})
export class AppRoutingModule {
}