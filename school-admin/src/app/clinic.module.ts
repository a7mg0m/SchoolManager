import { NgModule } from '@angular/core';
import { SharedModule } from './shared.module';

// Clinic Components
import { VaccineListComponent } from './clinic/vaccines/vaccine-list.component';
import { VaccineSaveComponent } from './clinic/vaccines/vaccine-save.component';
import { VaccinationCampaignListComponent } from './clinic/vaccination-campaigns/vaccination-campaign-list.component';
import { VaccinationCampaignSaveComponent } from './clinic/vaccination-campaigns/vaccination-campaign-save.component';
import { ClinicVisitListComponent } from './clinic/visits/clinic-visit-list.component';
import { ClinicVisitSaveComponent } from './clinic/visits/clinic-visit-save.component';
import { StudentClinicListComponent } from './clinic/students/student-clinic-list.component';
import { StudentClinicComponent } from './clinic/students/student-clinic.component';

@NgModule({
  declarations: [
    VaccineListComponent,
    VaccineSaveComponent,
    VaccinationCampaignListComponent,
    VaccinationCampaignSaveComponent,
    ClinicVisitListComponent,
    ClinicVisitSaveComponent,
    StudentClinicListComponent,
    StudentClinicComponent
  ],
  imports: [
    SharedModule
  ],
  exports: [
    VaccineListComponent,
    VaccineSaveComponent,
    VaccinationCampaignListComponent,
    VaccinationCampaignSaveComponent,
    ClinicVisitListComponent,
    ClinicVisitSaveComponent,
    StudentClinicListComponent,
    StudentClinicComponent
  ]
})
export class ClinicModule { }
