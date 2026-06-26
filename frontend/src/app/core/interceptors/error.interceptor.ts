import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../notifications/notification.service';

// Outermost interceptor: turns failed responses into a human-readable toast using the backend's
// ApiResponse envelope (message + errors[]), then rethrows so callers can still react.
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notify = inject(NotificationService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      notify.error(extractMessage(err));
      return throwError(() => err);
    }),
  );
};

function extractMessage(err: HttpErrorResponse): string {
  if (err.status === 0) return 'Cannot reach the server. Check your connection.';

  const body = err.error as { message?: string; errors?: string[] } | string | null;
  if (body && typeof body === 'object') {
    if (body.errors?.length) return body.errors.join(' ');
    if (body.message) return body.message;
  }
  if (typeof body === 'string' && body.trim()) return body;

  return err.status === 401 ? 'Your session has expired. Please sign in again.' : 'Something went wrong.';
}
