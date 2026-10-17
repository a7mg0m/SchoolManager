import {NgModule} from '@angular/core';
import {Routes, RouterModule} from '@angular/router';

import {BaseLayoutComponent} from './Layout/base-layout/base-layout.component';
import {PagesLayoutComponent} from './Layout/pages-layout/pages-layout.component';

// Import components from barrel file
import {
  // Dashboard components
  HomeComponent,
  
  // Management components
  StagesComponent,
  GradesComponent
} from './components.barrel';

const routes: Routes = [
  {
    path: '',
    component: BaseLayoutComponent,
    children: [
      // Dashboards
      {path: '', redirectTo: '/home', pathMatch: 'full'},
      {path: 'home', component: HomeComponent, data: {extraParameter: 'dashboardsMenu'}},

      // Management
      {path: 'management/stages', component: StagesComponent, data: {extraParameter: 'managementMenu'}},
      {path: 'management/grades', component: GradesComponent, data: {extraParameter: 'managementMenu'}},
    ]
  },
  {
    // Documentation section — lazy-loaded so it stays out of the initial bundle
    path: 'docs',
    loadChildren: () => import('./docs/docs.module').then(m => m.DocsModule)
  },
  {path: '**', redirectTo: ''}
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {
    scrollPositionRestoration: 'enabled',
    anchorScrolling: 'enabled'
  })],
  exports: [RouterModule]
})
export class AppRoutingModule {
}