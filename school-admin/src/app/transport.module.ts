import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';

// Transport Components
import { BusRouteListComponent } from './transport/routes/bus-route-list.component';
import { BusRouteSaveComponent } from './transport/routes/bus-route-save.component';
import { BusListComponent } from './transport/buses/bus-list.component';
import { BusSaveComponent } from './transport/buses/bus-save.component';
import { BusAssignmentListComponent } from './transport/assignments/bus-assignment-list.component';
import { BusAssignmentAssignComponent } from './transport/assignments/bus-assignment-assign.component';
import { TripListComponent } from './transport/trips/trip-list.component';
import { TripDetailsComponent } from './transport/trips/trip-details.component';

@NgModule({
  declarations: [
    BusRouteListComponent,
    BusRouteSaveComponent,
    BusListComponent,
    BusSaveComponent,
    BusAssignmentListComponent,
    BusAssignmentAssignComponent,
    TripListComponent,
    TripDetailsComponent
  ],
  imports: [
    SharedModule
  ],
  exports: [
    BusRouteListComponent,
    BusRouteSaveComponent,
    BusListComponent,
    BusSaveComponent,
    BusAssignmentListComponent,
    BusAssignmentAssignComponent,
    TripListComponent,
    TripDetailsComponent
  ]
})
export class TransportModule { }
