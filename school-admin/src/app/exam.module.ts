import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';

// Exam Components (ExamsHomework service)
import { ExamListComponent } from './exam/exams/exam-list.component';
import { ExamSaveComponent } from './exam/exams/exam-save.component';
import { ExamResultsComponent } from './exam/exam-results/exam-results.component';

@NgModule({
  declarations: [
    ExamListComponent,
    ExamSaveComponent,
    ExamResultsComponent
  ],
  imports: [
    SharedModule
  ],
  exports: [
    ExamListComponent,
    ExamSaveComponent,
    ExamResultsComponent
  ]
})
export class ExamModule { }
