import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenStorageService } from '../auth/token-storage.service';

// Blocks routes for unauthenticated visitors, preserving the intended URL for post-login redirect.
export const authGuard: CanActivateFn = (_route, state) => {
  const storage = inject(TokenStorageService);
  const router = inject(Router);

  if (storage.isAuthenticated) return true;

  return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
};
