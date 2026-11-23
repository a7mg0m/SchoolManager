import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';

// Attendance Components
import { DailyAttendanceListComponent } from './attendance/daily/daily-attendance-list.component';
import { StudentAttendanceComponent } from './attendance/student/student-attendance.component';
import { EvaluationListComponent } from './attendance/evaluations/evaluation-list.component';

@NgModule({
  declarations: [
    DailyAttendanceListComponent,
    StudentAttendanceComponent,
    EvaluationListComponent
  ],
  imports: [
    SharedModule
  ],
  exports: [
    DailyAttendanceListComponent,
    StudentAttendanceComponent,
    EvaluationListComponent
  ]
})
export class AttendanceModule { }
