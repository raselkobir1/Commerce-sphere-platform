import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

// Base URL of the API Gateway. Change this for production.
export const API_URL = 'http://localhost:5000';

// Every backend response is wrapped in this envelope; we just want `data`.
interface Envelope<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}

// Tiny HTTP helper. It adds the gateway URL and unwraps the envelope, so a call
// is as simple as: api.get<Product[]>('/api/products').
@Injectable({ providedIn: 'root' })
export class Api {
  private http = inject(HttpClient);

  get<T>(path: string, params?: Record<string, string | number | boolean | undefined>): Observable<T> {
    return this.http
      .get<Envelope<T>>(API_URL + path, { params: this.params(params) })
      .pipe(map((r) => r.data));
  }

  post<T>(path: string, body?: unknown): Observable<T> {
    return this.http.post<Envelope<T>>(API_URL + path, body ?? {}).pipe(map((r) => r.data));
  }

  put<T>(path: string, body?: unknown): Observable<T> {
    return this.http.put<Envelope<T>>(API_URL + path, body ?? {}).pipe(map((r) => r.data));
  }

  delete<T>(path: string): Observable<T> {
    return this.http.delete<Envelope<T>>(API_URL + path).pipe(map((r) => r.data));
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
