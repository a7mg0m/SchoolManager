import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';

// Users Components (Identity service)
import { EmployeeGroupListComponent } from './users/employee-groups/employee-group-list.component';
import { EmployeeGroupSaveComponent } from './users/employee-groups/employee-group-save.component';
import { EmployeeListComponent } from './users/employees/employee-list.component';
import { EmployeeSaveComponent } from './users/employees/employee-save.component';
import { MyProfileComponent } from './users/my-profile/my-profile.component';
import { GroupPermissionsComponent } from './users/group-permissions/group-permissions.component';

@NgModule({
  declarations: [
    EmployeeGroupListComponent,
    EmployeeGroupSaveComponent,
    EmployeeListComponent,
    EmployeeSaveComponent,
    MyProfileComponent,
    GroupPermissionsComponent
  ],
  imports: [
    SharedModule
  ],
  exports: [
    EmployeeGroupListComponent,
    EmployeeGroupSaveComponent,
    EmployeeListComponent,
    EmployeeSaveComponent,
    MyProfileComponent,
    GroupPermissionsComponent
  ]
})
export class UsersModule { }
