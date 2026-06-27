import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Auth } from './auth';

// Endpoints that should NOT carry a token.
const PUBLIC = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh-token'];

// Adds "Authorization: Bearer <token>" to every authenticated request.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(Auth).token;
  const isPublic = PUBLIC.some((p) => req.url.includes(p));

  if (!token || isPublic) return next(req);
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
