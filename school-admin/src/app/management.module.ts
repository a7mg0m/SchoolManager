import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';

// Management Components
import { StagesComponent } from './management/stages/stages';
import { GradesComponent } from './management/grades/grades';
import { AcademicYearListComponent } from './management/academic-years/academic-year-list.component';
import { AcademicYearSaveComponent } from './management/academic-years/academic-year-save.component';
import { SchoolInfoComponent } from './management/school-info/school-info.component';
import { SectionListComponent } from './management/sections/section-list.component';
import { SectionSaveComponent } from './management/sections/section-save.component';
import { TeacherListComponent } from './management/teachers/teacher-list.component';
import { TeacherSaveComponent } from './management/teachers/teacher-save.component';
import { SubjectListComponent } from './management/subjects/subject-list.component';
import { SubjectSaveComponent } from './management/subjects/subject-save.component';
import { ParentListComponent } from './management/parents/parent-list.component';
import { ParentSaveComponent } from './management/parents/parent-save.component';
import { TimetableListComponent } from './management/timetables/timetable-list.component';
import { TimetableSaveComponent } from './management/timetables/timetable-save.component';

@NgModule({
  declarations: [
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
    TimetableSaveComponent
  ],
  imports: [
    SharedModule
  ],
  exports: [
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
    TimetableSaveComponent
  ]
})
export class ManagementModule { }
