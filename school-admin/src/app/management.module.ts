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
    TeacherSaveComponent
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
    TeacherSaveComponent
  ]
})
export class ManagementModule { }
