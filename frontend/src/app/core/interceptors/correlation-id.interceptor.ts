import { HttpInterceptorFn } from '@angular/common/http';

// Stamps every outbound request with a correlation id so a request can be traced across the
// gateway and all backend services (they read/propagate X-Correlation-Id).
export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  const correlationId =
    typeof crypto !== 'undefined' && 'randomUUID' in crypto
      ? crypto.randomUUID()
      : `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  return next(req.clone({ setHeaders: { 'X-Correlation-Id': correlationId } }));
};
