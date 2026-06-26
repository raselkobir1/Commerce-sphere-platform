import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { TokenStorageService } from '../auth/token-storage.service';
import { homePathForRole } from './home-redirect';

// Keeps already-authenticated users off the login/register pages by sending them home.
export const guestGuard: CanActivateFn = () => {
  const storage = inject(TokenStorageService);
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!storage.isAuthenticated) return true;

  return router.createUrlTree([homePathForRole(auth.role())]);
};
