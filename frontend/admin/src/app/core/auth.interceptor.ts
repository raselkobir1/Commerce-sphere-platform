import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { Auth } from './auth';
import { SUPPRESS_ERROR_TOAST } from './api';
import { Toast } from './toast';

// Endpoints that should NOT carry a token (and must never trigger the refresh-and-retry path).
const PUBLIC = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh-token'];

// Attaches the bearer token, and centralises token-expiry handling:
//   • 401 → transparently refresh the access token (using the refresh token) and retry the
//     request, so the user is never bounced to login while their session is still refreshable.
//     The refreshed token carries the latest permissions. Only a failed refresh logs them out.
//   • 403 → a clear "no permission" message (unless the caller opted out for background reads).
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(Auth);
  const router = inject(Router);
  const toast = inject(Toast);

  const isPublic = PUBLIC.some((p) => req.url.includes(p));
  const withAuth = (token: string | null) =>
    token && !isPublic ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(withAuth(auth.token)).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && !isPublic && auth.refreshToken) {
        return auth.refresh().pipe(
          switchMap((newToken) => {
            if (newToken) return next(withAuth(newToken)); // retry with the fresh token
            // Refresh failed → the session is genuinely over.
            auth.logout();
            toast.error('Your session has expired. Please sign in again.');
            router.navigate(['/login']);
            return throwError(() => err);
          }),
        );
      }

      // Authenticated but lacks the required permission → clear message, unless the caller opted
      // out (e.g. background data loads on a page where some tiles are simply not accessible).
      if (err.status === 403 && !req.context.get(SUPPRESS_ERROR_TOAST)) {
        toast.error("You don't have permission to perform this action.");
      }
      return throwError(() => err);
    }),
  );
};
