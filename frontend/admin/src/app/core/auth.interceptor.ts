import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { Auth } from './auth';
import { SUPPRESS_ERROR_TOAST } from './api';
import { Toast } from './toast';

// Endpoints that should NOT carry a token.
const PUBLIC = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh-token'];

// Adds "Authorization: Bearer <token>" to every authenticated request, and handles an expired or
// invalid token in one place: a 401 on any authenticated call signs the user out and sends them to
// the login page with a clear message — instead of a confusing generic error toast on each call.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(Auth);
  const router = inject(Router);
  const toast = inject(Toast);

  const token = auth.token;
  const isPublic = PUBLIC.some((p) => req.url.includes(p));

  const request = !token || isPublic
    ? req
    : req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });

  return next(request).pipe(
    catchError((err: HttpErrorResponse) => {
      // Token expired/rejected mid-session → log out once and bounce to login.
      if (err.status === 401 && !isPublic && auth.isAuthenticated()) {
        auth.logout();
        toast.error('Your session has expired. Please sign in again.');
        router.navigate(['/login']);
      }
      // Authenticated but lacks the required permission → clear message, unless the caller opted
      // out (e.g. background data loads on a page where some tiles are simply not accessible).
      else if (err.status === 403 && !req.context.get(SUPPRESS_ERROR_TOAST)) {
        toast.error("You don't have permission to perform this action.");
      }
      return throwError(() => err);
    }),
  );
};
