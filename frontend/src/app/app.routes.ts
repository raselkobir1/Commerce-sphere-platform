import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'shop' },

  // Unauthenticated area (already-signed-in users are bounced home by guestGuard).
  {
    path: 'auth',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },

  // Admin area — requires an authenticated Admin.
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES),
  },

  // Customer storefront — any authenticated user.
  {
    path: 'shop',
    canActivate: [authGuard],
    loadChildren: () => import('./features/storefront/storefront.routes').then((m) => m.STOREFRONT_ROUTES),
  },

  { path: '**', redirectTo: 'shop' },
];
