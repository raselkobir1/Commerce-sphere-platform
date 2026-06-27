import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from './auth';
import { Perms } from './perms';

// Admin app access: must be signed in AND have at least one viewable menu (i.e. their role grants
// some admin access). Pure storefront customers have no menus and are bounced to login.
export const adminGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const perms = inject(Perms);
  const router = inject(Router);

  if (auth.isAuthenticated() && perms.menus().length > 0) return true;
  return router.createUrlTree(['/login']);
};

// Per-route guard: the user must have View permission on the given menu, else send them to the
// first menu they can see (or login).
export function canView(menuKey: string): CanActivateFn {
  return () => {
    const perms = inject(Perms);
    const router = inject(Router);
    if (perms.canView(menuKey)) return true;
    const first = perms.menus()[0];
    return router.createUrlTree([first ? first.route : '/login']);
  };
}
