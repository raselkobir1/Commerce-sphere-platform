import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthApiService } from '../auth/auth-api.service';
import { AuthService } from '../auth/auth.service';
import { TokenStorageService } from '../auth/token-storage.service';

// Single-flight refresh state shared across all in-flight requests. When the access token expires,
// the first 401 triggers a refresh; concurrent 401s queue on `refreshed$` and retry once it lands.
let isRefreshing = false;
const refreshed$ = new BehaviorSubject<string | null>(null);

// Paths where a 401 is terminal (no point refreshing) — login/refresh themselves.
const NO_REFRESH_PATHS = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh-token'];

export const refreshInterceptor: HttpInterceptorFn = (req, next) => {
  const storage = inject(TokenStorageService);
  const auth = inject(AuthService);
  const authApi = inject(AuthApiService);
  const router = inject(Router);

  if (NO_REFRESH_PATHS.some((p) => req.url.includes(p))) {
    return next(req);
  }

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401 || !storage.refreshToken) {
        return throwError(() => err);
      }

      // A refresh is already underway — wait for the new token, then retry this request.
      if (isRefreshing) {
        return refreshed$.pipe(
          filter((token): token is string => token !== null),
          take(1),
          switchMap((token) => next(withBearer(req, token))),
        );
      }

      // Kick off the single refresh.
      isRefreshing = true;
      refreshed$.next(null);
      return authApi.refreshToken(storage.refreshToken).pipe(
        switchMap((tokens) => {
          auth.setSession(tokens);
          isRefreshing = false;
          refreshed$.next(tokens.accessToken);
          return next(withBearer(req, tokens.accessToken));
        }),
        catchError((refreshErr) => {
          isRefreshing = false;
          refreshed$.next(null);
          // Refresh failed — the session is dead. Clear and bounce to login.
          void auth.logout().subscribe();
          void router.navigate(['/auth/login']);
          return throwError(() => refreshErr);
        }),
      );
    }),
  );
};

function withBearer(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
