import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';

// Financial Components
import { ExpenseCategoryListComponent } from './financial/expense-categories/expense-category-list.component';
import { ExpenseCategorySaveComponent } from './financial/expense-categories/expense-category-save.component';
import { RevenueCategoryListComponent } from './financial/revenue-categories/revenue-category-list.component';
import { RevenueCategorySaveComponent } from './financial/revenue-categories/revenue-category-save.component';
import { ExpenseListComponent } from './financial/expenses/expense-list.component';
import { ExpenseSaveComponent } from './financial/expenses/expense-save.component';
import { RevenueListComponent } from './financial/revenues/revenue-list.component';
import { RevenueSaveComponent } from './financial/revenues/revenue-save.component';
import { FeeListComponent } from './financial/fees/fee-list.component';
import { FeeSaveComponent } from './financial/fees/fee-save.component';
import { StudentFeeListComponent } from './financial/student-fees/student-fee-list.component';
import { StudentFeeAssignComponent } from './financial/student-fees/student-fee-assign.component';

@NgModule({
  declarations: [
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
    StudentFeeAssignComponent
  ],
  imports: [
    SharedModule
  ],
  exports: [
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
    StudentFeeAssignComponent
  ]
})
export class FinancialModule { }
