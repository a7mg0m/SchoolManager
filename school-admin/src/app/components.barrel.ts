// Barrel file for all components to be used in routing
// This prevents circular dependencies while making components available to routing

// Dashboard components
export { HomeComponent } from './home/home';

// Management components
export { StagesComponent } from './management/stages/stages';
export { GradesComponent } from './management/grades/grades';
export { AcademicYearListComponent } from './management/academic-years/academic-year-list.component';
export { AcademicYearSaveComponent } from './management/academic-years/academic-year-save.component';
export { SchoolInfoComponent } from './management/school-info/school-info.component';
export { SectionListComponent } from './management/sections/section-list.component';
export { SectionSaveComponent } from './management/sections/section-save.component';
export { TeacherListComponent } from './management/teachers/teacher-list.component';
export { TeacherSaveComponent } from './management/teachers/teacher-save.component';
export { SubjectListComponent } from './management/subjects/subject-list.component';
export { SubjectSaveComponent } from './management/subjects/subject-save.component';
export { ParentListComponent } from './management/parents/parent-list.component';
export { ParentSaveComponent } from './management/parents/parent-save.component';
export { TimetableListComponent } from './management/timetables/timetable-list.component';
export { TimetableSaveComponent } from './management/timetables/timetable-save.component';
export { StudentListComponent } from './management/students/student-list.component';
export { StudentProfileComponent } from './management/students/student-profile.component';
// Financial components
export { ExpenseCategoryListComponent } from './financial/expense-categories/expense-category-list.component';
export { ExpenseCategorySaveComponent } from './financial/expense-categories/expense-category-save.component';
export { RevenueCategoryListComponent } from './financial/revenue-categories/revenue-category-list.component';
export { RevenueCategorySaveComponent } from './financial/revenue-categories/revenue-category-save.component';
export { ExpenseListComponent } from './financial/expenses/expense-list.component';
export { ExpenseSaveComponent } from './financial/expenses/expense-save.component';
export { RevenueListComponent } from './financial/revenues/revenue-list.component';
export { RevenueSaveComponent } from './financial/revenues/revenue-save.component';
export { FeeListComponent } from './financial/fees/fee-list.component';
export { FeeSaveComponent } from './financial/fees/fee-save.component';
export { StudentFeeListComponent } from './financial/student-fees/student-fee-list.component';
export { StudentFeeAssignComponent } from './financial/student-fees/student-fee-assign.component';
// Notification components
export { SchoolEventListComponent } from './notification/school-events/school-event-list.component';
export { SchoolEventSaveComponent } from './notification/school-events/school-event-save.component';
export { NotificationListComponent } from './notification/notifications/notification-list.component';
export { NotificationSaveComponent } from './notification/notifications/notification-save.component';
export { NotificationTargetsComponent } from './notification/notifications/notification-targets.component';

// Exam components
export { ExamListComponent } from './exam/exams/exam-list.component';
export { ExamSaveComponent } from './exam/exams/exam-save.component';
export { ExamResultsComponent } from './exam/exam-results/exam-results.component';
