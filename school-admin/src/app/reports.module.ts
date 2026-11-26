import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';

// Reports (src/app/reports)
import { StudentsStatusReportComponent } from './reports/students-status/students-status-report.component';
import { ExpensesReportComponent } from './reports/expenses/expenses-report.component';
import { IncomeReportComponent } from './reports/income/income-report.component';
import { AttendanceReportComponent } from './reports/attendance/attendance-report.component';
import { ReviewsReportComponent } from './reports/reviews/reviews-report.component';
import { ExamResultsReportComponent } from './reports/exam-results/exam-results-report.component';
import { TransportReportComponent } from './reports/transport/transport-report.component';

@NgModule({
  declarations: [StudentsStatusReportComponent, ExpensesReportComponent, IncomeReportComponent, AttendanceReportComponent, ReviewsReportComponent, ExamResultsReportComponent, TransportReportComponent],
  imports: [SharedModule],
  exports: [StudentsStatusReportComponent, ExpensesReportComponent, IncomeReportComponent, AttendanceReportComponent, ReviewsReportComponent, ExamResultsReportComponent, TransportReportComponent]
})
export class ReportsModule { }
