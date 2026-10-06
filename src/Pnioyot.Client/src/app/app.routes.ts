import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { InquiryDetailComponent } from './inquiries/inquiry-detail.component';
import { InquiryListComponent } from './inquiries/inquiry-list.component';

export const routes: Routes = [
  { path: '', component: DashboardComponent },
  { path: 'inquiries', component: InquiryListComponent },
  { path: 'inquiries/:id', component: InquiryDetailComponent },
  { path: '**', redirectTo: '' }
];
