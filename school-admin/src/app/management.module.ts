import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';

// Management Components
import { StagesComponent } from './management/stages/stages';
import { GradesComponent } from './management/grades/grades';

@NgModule({
  declarations: [
    StagesComponent,
    GradesComponent
  ],
  imports: [
    SharedModule
  ],
  exports: [
    StagesComponent,
    GradesComponent
  ]
})
export class ManagementModule { }
