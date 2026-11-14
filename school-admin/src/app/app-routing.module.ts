import {NgModule} from '@angular/core';
import {Routes, RouterModule} from '@angular/router';

import {BaseLayoutComponent} from './Layout/base-layout/base-layout.component';
import {PagesLayoutComponent} from './Layout/pages-layout/pages-layout.component';
import { AuthGuard } from './core/guards/auth.guard';
import { PermissionGuard } from './core/guards/permission.guard';

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
  ParentSaveComponent,
  TimetableListComponent,
  TimetableSaveComponent,
  StudentListComponent,
  StudentProfileComponent,

  // Financial components
  ExpenseCategoryListComponent,
  ExpenseCategorySaveComponent,
  RevenueCategoryListComponent,
  RevenueCategorySaveComponent,
  ExpenseListComponent,
  ExpenseSaveComponent,
  RevenueListComponent,
  RevenueSaveComponent,
  FeeListComponent,
  FeeSaveComponent,
  StudentFeeListComponent,
  StudentFeeAssignComponent,

  // Notification components
  SchoolEventListComponent,
  SchoolEventSaveComponent,
  NotificationListComponent,
  NotificationSaveComponent,
  NotificationTargetsComponent,

  // Exam components
  ExamListComponent,
  ExamSaveComponent,
  ExamResultsComponent,

  // Users components
  EmployeeGroupListComponent,
  EmployeeGroupSaveComponent,
  EmployeeListComponent,
  EmployeeSaveComponent,
  MyProfileComponent,
  GroupPermissionsComponent
} from './components.barrel';

// Import user pages
import {UserPagesModule} from './user-pages.module';
import {LoginBoxedComponent} from './UserPages/login-boxed/login-boxed.component';
import {NotAuthorizedComponent} from './UserPages/not-authorized/not-authorized.component';

const routes: Routes = [
  // App always opens on the login page
  {path: '', redirectTo: '/login', pathMatch: 'full'},
  {
    path: 'login',
    component: PagesLayoutComponent,
    children: [
      {path: '', component: LoginBoxedComponent, data: {title: 'pageTitle.login'}}
    ]
  },
  {
    path: 'not-authorized',
    component: PagesLayoutComponent,
    children: [
      {path: '', component: NotAuthorizedComponent, data: {title: 'pageTitle.notAuthorized'}}
    ]
  },
  {
    path: '',
    component: BaseLayoutComponent,
    canActivate: [AuthGuard],
    // AuthGuard: signed in; PermissionGuard: group permissions (route data `permission`, default 'view')
    canActivateChild: [AuthGuard, PermissionGuard],
    children: [
      // Dashboards
      {path: 'home', component: HomeComponent, data: {extraParameter: 'dashboardsMenu'}},

      // Management
      {path: 'management/school-info', component: SchoolInfoComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/stages', component: StagesComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/grades', component: GradesComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/academic-years', component: AcademicYearListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/academic-years/save', component: AcademicYearSaveComponent, data: {extraParameter: 'managementMenu', permission: 'add'}},
      {path: 'management/academic-years/save/:id', component: AcademicYearSaveComponent, data: {extraParameter: 'managementMenu', permission: 'edit'}},
      {path: 'management/sections', component: SectionListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/sections/save', component: SectionSaveComponent, data: {extraParameter: 'managementMenu', permission: 'add'}},
      {path: 'management/sections/save/:id', component: SectionSaveComponent, data: {extraParameter: 'managementMenu', permission: 'edit'}},
      {path: 'management/teachers', component: TeacherListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/teachers/save', component: TeacherSaveComponent, data: {extraParameter: 'managementMenu', permission: 'add'}},
      {path: 'management/teachers/save/:id', component: TeacherSaveComponent, data: {extraParameter: 'managementMenu', permission: 'edit'}},
      {path: 'management/subjects', component: SubjectListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/subjects/save', component: SubjectSaveComponent, data: {extraParameter: 'managementMenu', permission: 'add'}},
      {path: 'management/subjects/save/:id', component: SubjectSaveComponent, data: {extraParameter: 'managementMenu', permission: 'edit'}},
      {path: 'management/students', component: StudentListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/students/:id', component: StudentProfileComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/parents', component: ParentListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/parents/save', component: ParentSaveComponent, data: {extraParameter: 'managementMenu', permission: 'add'}},
      {path: 'management/parents/save/:id', component: ParentSaveComponent, data: {extraParameter: 'managementMenu', permission: 'edit'}},
      {path: 'management/timetables', component: TimetableListComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/timetables/save', component: TimetableSaveComponent, data: {extraParameter: 'managementMenu', permission: 'add'}},
      {path: 'management/timetables/save/:id', component: TimetableSaveComponent, data: {extraParameter: 'managementMenu', permission: 'edit'}},

      // Financial
      {path: 'financial/expense-categories', component: ExpenseCategoryListComponent, data: {extraParameter: 'financialMenu'}},
      {path: 'financial/expense-categories/save', component: ExpenseCategorySaveComponent, data: {extraParameter: 'financialMenu', permission: 'add'}},
      {path: 'financial/expense-categories/save/:id', component: ExpenseCategorySaveComponent, data: {extraParameter: 'financialMenu', permission: 'edit'}},
      {path: 'financial/revenue-categories', component: RevenueCategoryListComponent, data: {extraParameter: 'financialMenu'}},
      {path: 'financial/revenue-categories/save', component: RevenueCategorySaveComponent, data: {extraParameter: 'financialMenu', permission: 'add'}},
      {path: 'financial/revenue-categories/save/:id', component: RevenueCategorySaveComponent, data: {extraParameter: 'financialMenu', permission: 'edit'}},
      {path: 'financial/expenses', component: ExpenseListComponent, data: {extraParameter: 'financialMenu'}},
      {path: 'financial/expenses/save', component: ExpenseSaveComponent, data: {extraParameter: 'financialMenu', permission: 'add'}},
      {path: 'financial/expenses/save/:id', component: ExpenseSaveComponent, data: {extraParameter: 'financialMenu', permission: 'edit'}},
      {path: 'financial/revenues', component: RevenueListComponent, data: {extraParameter: 'financialMenu'}},
      {path: 'financial/revenues/save', component: RevenueSaveComponent, data: {extraParameter: 'financialMenu', permission: 'add'}},
      {path: 'financial/revenues/save/:id', component: RevenueSaveComponent, data: {extraParameter: 'financialMenu', permission: 'edit'}},
      {path: 'financial/fees', component: FeeListComponent, data: {extraParameter: 'financialMenu'}},
      {path: 'financial/fees/save', component: FeeSaveComponent, data: {extraParameter: 'financialMenu', permission: 'add'}},
      {path: 'financial/fees/save/:id', component: FeeSaveComponent, data: {extraParameter: 'financialMenu', permission: 'edit'}},
      {path: 'financial/student-fees', component: StudentFeeListComponent, data: {extraParameter: 'financialMenu'}},
      {path: 'financial/student-fees/student/:studentId', component: StudentFeeListComponent, data: {extraParameter: 'financialMenu'}},
      {path: 'financial/student-fees/assign', component: StudentFeeAssignComponent, data: {extraParameter: 'financialMenu', permission: 'add'}},

      // Notification
      {path: 'notification/school-events', component: SchoolEventListComponent, data: {extraParameter: 'notificationMenu'}},
      {path: 'notification/school-events/save', component: SchoolEventSaveComponent, data: {extraParameter: 'notificationMenu', permission: 'add'}},
      {path: 'notification/school-events/save/:id', component: SchoolEventSaveComponent, data: {extraParameter: 'notificationMenu', permission: 'edit'}},
      {path: 'notification/notifications', component: NotificationListComponent, data: {extraParameter: 'notificationMenu'}},
      {path: 'notification/notifications/save', component: NotificationSaveComponent, data: {extraParameter: 'notificationMenu', permission: 'add'}},
      {path: 'notification/notifications/save/:id', component: NotificationSaveComponent, data: {extraParameter: 'notificationMenu', permission: 'edit'}},
      {path: 'notification/notifications/:id/recipients', component: NotificationTargetsComponent, data: {extraParameter: 'notificationMenu'}},

      // Exam
      {path: 'exam/exams', component: ExamListComponent, data: {extraParameter: 'examMenu'}},
      {path: 'exam/exams/save', component: ExamSaveComponent, data: {extraParameter: 'examMenu', permission: 'add'}},
      {path: 'exam/exams/save/:id', component: ExamSaveComponent, data: {extraParameter: 'examMenu', permission: 'edit'}},
      {path: 'exam/exams/:id/results', component: ExamResultsComponent, data: {extraParameter: 'examMenu'}},

      // Users
      {path: 'users/employee-groups', component: EmployeeGroupListComponent, data: {extraParameter: 'usersMenu'}},
      {path: 'users/employee-groups/save', component: EmployeeGroupSaveComponent, data: {extraParameter: 'usersMenu', permission: 'add'}},
      {path: 'users/employee-groups/save/:id', component: EmployeeGroupSaveComponent, data: {extraParameter: 'usersMenu', permission: 'edit'}},
      {path: 'users/employees', component: EmployeeListComponent, data: {extraParameter: 'usersMenu'}},
      {path: 'users/employees/save', component: EmployeeSaveComponent, data: {extraParameter: 'usersMenu', permission: 'add'}},
      {path: 'users/employees/save/:id', component: EmployeeSaveComponent, data: {extraParameter: 'usersMenu', permission: 'edit'}},
      {path: 'users/group-permissions', component: GroupPermissionsComponent, data: {extraParameter: 'usersMenu'}},
      {path: 'users/group-permissions/:groupId', component: GroupPermissionsComponent, data: {extraParameter: 'usersMenu'}},

      // My profile (opened from the header user box)
      {path: 'my-profile', component: MyProfileComponent, data: {permission: false, title: 'myProfile.title'}},
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