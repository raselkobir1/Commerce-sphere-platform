import { Routes } from '@angular/router';

// Shared account pages, mounted under both the admin and storefront shells.
export const ACCOUNT_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./profile/profile').then((m) => m.Profile) },
  { path: 'security', loadComponent: () => import('./security/security').then((m) => m.Security) },
];
