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