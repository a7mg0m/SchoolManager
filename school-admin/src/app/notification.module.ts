import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';

// Notification Components (Notification service: school events, notifications)
import { AudiencePickerComponent } from './notification/shared/audience-picker.component';
import { SchoolEventListComponent } from './notification/school-events/school-event-list.component';
import { SchoolEventSaveComponent } from './notification/school-events/school-event-save.component';
import { NotificationListComponent } from './notification/notifications/notification-list.component';
import { NotificationSaveComponent } from './notification/notifications/notification-save.component';
import { NotificationTargetsComponent } from './notification/notifications/notification-targets.component';

@NgModule({
  declarations: [
    AudiencePickerComponent,
    SchoolEventListComponent,
    SchoolEventSaveComponent,
    NotificationListComponent,
    NotificationSaveComponent,
    NotificationTargetsComponent
  ],
  imports: [
    SharedModule
  ],
  exports: [
    SchoolEventListComponent,
    SchoolEventSaveComponent,
    NotificationListComponent,
    NotificationSaveComponent,
    NotificationTargetsComponent
  ]
})
export class NotificationModule { }
