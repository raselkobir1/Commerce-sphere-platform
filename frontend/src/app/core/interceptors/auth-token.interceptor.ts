import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TokenStorageService } from '../auth/token-storage.service';

// Endpoints that must NOT receive an Authorization header (they establish/refresh a session).
const ANONYMOUS_PATHS = [
  '/api/auth/login',
  '/api/auth/register',
  '/api/auth/refresh-token',
  '/api/auth/password/forgot',
  '/api/auth/password/reset',
  '/api/auth/2fa/verify',
  '/api/auth/otp/verify',
];

// Attaches the bearer access token to authenticated requests.
export const authTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const storage = inject(TokenStorageService);
  const token = storage.accessToken;

  if (!token || ANONYMOUS_PATHS.some((p) => req.url.includes(p))) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
