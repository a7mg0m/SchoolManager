import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';

// Notification Components (Notification service: school events, ...)
import { SchoolEventListComponent } from './notification/school-events/school-event-list.component';
import { SchoolEventSaveComponent } from './notification/school-events/school-event-save.component';

@NgModule({
  declarations: [
    SchoolEventListComponent,
    SchoolEventSaveComponent
  ],
  imports: [
    SharedModule
  ],
  exports: [
    SchoolEventListComponent,
    SchoolEventSaveComponent
  ]
})
export class NotificationModule { }
