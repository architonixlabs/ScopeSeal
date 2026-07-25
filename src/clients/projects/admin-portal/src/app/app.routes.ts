import { Routes } from '@angular/router';
import { adminAuthGuard } from './core/admin-auth.guard';
import { AdminShellComponent } from './layout/admin-shell.component';
import { LoginComponent } from './pages/login.component';
import { DashboardComponent } from './pages/dashboard.component';
import { TenantsComponent } from './pages/tenants.component';
import { PrivacyQueueComponent } from './pages/privacy-queue.component';
import { FeatureFlagsComponent } from './pages/feature-flags.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: AdminShellComponent,
    canActivate: [adminAuthGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'tenants', component: TenantsComponent },
      { path: 'privacy-queue', component: PrivacyQueueComponent },
      { path: 'feature-flags', component: FeatureFlagsComponent },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
