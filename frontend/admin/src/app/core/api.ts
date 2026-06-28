import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, tap, throwError } from 'rxjs';
import { Toast } from './toast';

// Base URL of the API Gateway. Change this for production.
export const API_URL = 'http://localhost:5000';

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
      this.http.get<Envelope<T>>(API_URL + path, { params: this.params(params) }),
      false,
      opts,
    );
  }

  post<T>(path: string, body?: unknown, opts?: ReqOptions): Observable<T> {
    return this.handle(this.http.post<Envelope<T>>(API_URL + path, body ?? {}), true, opts);
  }

  put<T>(path: string, body?: unknown, opts?: ReqOptions): Observable<T> {
    return this.handle(this.http.put<Envelope<T>>(API_URL + path, body ?? {}), true, opts);
  }

  patch<T>(path: string, body?: unknown, opts?: ReqOptions): Observable<T> {
    return this.handle(this.http.patch<Envelope<T>>(API_URL + path, body ?? {}), true, opts);
  }

  delete<T>(path: string, opts?: ReqOptions): Observable<T> {
    return this.handle(this.http.delete<Envelope<T>>(API_URL + path), true, opts);
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
        if (wantError) this.toast.error(this.errorMessage(err));
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
