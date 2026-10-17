import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';
import { BaseChartDirective } from 'ng2-charts';

// Dashboard Components
import { HomeComponent } from './home/home';

@NgModule({
  declarations: [
    HomeComponent
  ],
  imports: [
    SharedModule,
    BaseChartDirective
  ],
  exports: [
    HomeComponent
  ]
})
export class DashboardsModule { }