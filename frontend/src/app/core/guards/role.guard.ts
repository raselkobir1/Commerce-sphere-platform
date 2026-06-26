import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { UserRole } from '../models/auth.models';
import { homePathForRole } from './home-redirect';

// Restricts a route to specific roles. Declare allowed roles via route data:
//   { path: 'admin', canActivate: [authGuard, roleGuard], data: { roles: ['Admin'] } }
// A logged-in user lacking the role is sent to their own home rather than the login page.
export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const allowed = (route.data?.['roles'] as UserRole[] | undefined) ?? [];
  const role = auth.role();

  if (role && (allowed.length === 0 || allowed.includes(role))) return true;

  return router.createUrlTree([homePathForRole(role)]);
};
