import { HttpClient, HttpContext, HttpContextToken, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, tap, throwError } from 'rxjs';
import { Toast } from './toast';

// Base URL of the API Gateway. Change this for production.
export const API_URL = 'http://localhost:5000';

// Set on a request when the caller opted out of error toasts (toastError: false). The auth
// interceptor reads this so its global 401/403 handling can stay silent for background reads.
export const SUPPRESS_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

// Every backend response is wrapped in this envelope; we just want `data`.
interface Envelope<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}

// Per-call toast control. Defaults: mutations (post/put/patch/delete) show the success
// message from the envelope; all verbs show a toast on error. Opt out for silent/background
// calls or where a page shows its own inline message.
export interface ReqOptions {
  params?: Record<string, string | number | boolean | undefined>;
  toastSuccess?: boolean;
  toastError?: boolean;
}

// Tiny HTTP helper. It adds the gateway URL, unwraps the envelope, and surfaces the backend's
// own success/error message as a toast — so every add/update/delete across the app gets feedback
// for free. A call is as simple as: api.get<Product[]>('/api/products').
@Injectable({ providedIn: 'root' })
export class Api {
  private http = inject(HttpClient);
  private toast = inject(Toast);

  get<T>(path: string, params?: Record<string, string | number | boolean | undefined>, opts?: ReqOptions): Observable<T> {
    return this.handle(
      this.http.get<Envelope<T>>(API_URL + path, { params: this.params(params), context: this.context(opts) }),
      false,
      opts,
    );
  }

  post<T>(path: string, body?: unknown, opts?: ReqOptions): Observable<T> {
    return this.handle(this.http.post<Envelope<T>>(API_URL + path, body ?? {}, { context: this.context(opts) }), true, opts);
  }

  put<T>(path: string, body?: unknown, opts?: ReqOptions): Observable<T> {
    return this.handle(this.http.put<Envelope<T>>(API_URL + path, body ?? {}, { context: this.context(opts) }), true, opts);
  }

  patch<T>(path: string, body?: unknown, opts?: ReqOptions): Observable<T> {
    return this.handle(this.http.patch<Envelope<T>>(API_URL + path, body ?? {}, { context: this.context(opts) }), true, opts);
  }

  delete<T>(path: string, opts?: ReqOptions): Observable<T> {
    return this.handle(this.http.delete<Envelope<T>>(API_URL + path, { context: this.context(opts) }), true, opts);
  }

  // Flags the request so the auth interceptor stays silent on errors when toastError is false.
  private context(opts?: ReqOptions): HttpContext {
    return new HttpContext().set(SUPPRESS_ERROR_TOAST, opts?.toastError === false);
  }

  // Unwraps the envelope and fires toasts. `isMutation` decides the default for success toasts.
  private handle<T>(source: Observable<Envelope<T>>, isMutation: boolean, opts?: ReqOptions): Observable<T> {
    const wantSuccess = opts?.toastSuccess ?? isMutation;
    const wantError = opts?.toastError ?? true;

    return source.pipe(
      tap((r) => {
        if (wantSuccess && r?.message) this.toast.success(r.message);
      }),
      map((r) => r.data),
      catchError((err) => {
        // 401 (session expired) and 403 (no permission) are handled globally by the auth
        // interceptor, so don't also show the generic error toast for them.
        const status = (err as { status?: number })?.status;
        if (wantError && status !== 401 && status !== 403) this.toast.error(this.errorMessage(err));
        return throwError(() => err);
      }),
    );
  }

  private errorMessage(err: unknown): string {
    const e = err as { error?: { message?: string }; status?: number };
    if (e?.error?.message) return e.error.message;
    if (e?.status === 0) return 'Cannot reach the server. Check your connection.';
    return 'Something went wrong. Please try again.';
  }

  private params(input?: Record<string, string | number | boolean | undefined>): HttpParams {
    let p = new HttpParams();
    if (!input) return p;
    for (const [key, value] of Object.entries(input)) {
      if (value !== undefined && value !== null && value !== '') p = p.set(key, String(value));
    }
    return p;
  }
}
