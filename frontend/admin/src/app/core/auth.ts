import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Observable, catchError, finalize, map, of, shareReplay, tap } from 'rxjs';
import { API_URL, Api, SUPPRESS_ERROR_TOAST } from './api';
import { AuthResult, User } from './models';

const TOKEN_KEY = 'adminsphere.token';
const REFRESH_KEY = 'adminsphere.refresh';

interface Envelope<T> { data: T; }

// Holds the signed-in admin and talks to the Auth service.
@Injectable({ providedIn: 'root' })
export class Auth {
  private api = inject(Api);
  private http = inject(HttpClient);

  // The current user (null when signed out). Components read this reactively.
  user = signal<User | null>(null);
  isAdmin = computed(() => this.user()?.role === 'Admin');
  isAuthenticated = computed(() => this.user() !== null);

  // De-dupes concurrent refreshes: many requests can 401 at once, but only one refresh call runs.
  private refreshInFlight: Observable<string | null> | null = null;

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  // Sign in. Throws a friendly error if the account needs 2FA/OTP (not handled here).
  login(email: string, password: string): Observable<User> {
    // The login page shows its own inline error; success navigates — so no toast either way.
    return this.api.post<AuthResult>('/api/auth/login', { email, password }, { toastSuccess: false, toastError: false }).pipe(
      map((data) => {
        if (!data?.accessToken) {
          throw new Error('This account requires extra verification, which AdminSphere does not support.');
        }
        this.save(data);
        return data.user;
      }),
    );
  }

  // Called once at startup to restore the session from the saved token.
  restore(): Observable<User | null> {
    if (!this.token) return of(null);
    // Silent session restore. If the access token has expired, the auth interceptor will
    // transparently refresh it (using the stored refresh token) and retry this call.
    return this.api.get<User>('/api/auth/me', undefined, { toastError: false }).pipe(
      tap((u) => this.user.set(u)),
      catchError(() => {
        this.logout();
        return of(null);
      }),
    );
  }

  // Exchanges the stored refresh token for a fresh access token (carrying the latest permissions).
  // Returns the new access token, or null if the session can no longer be refreshed. Concurrent
  // callers share one in-flight request.
  refresh(): Observable<string | null> {
    if (this.refreshInFlight) return this.refreshInFlight;

    const token = this.refreshToken;
    if (!token) return of(null);

    // Call HttpClient directly (not via Api) so this never recurses through the toast pipeline;
    // SUPPRESS_ERROR_TOAST keeps the interceptor quiet if the refresh itself fails.
    this.refreshInFlight = this.http
      .post<Envelope<AuthResult>>(
        `${API_URL}/api/auth/refresh-token`,
        { refreshToken: token },
        { context: new HttpContext().set(SUPPRESS_ERROR_TOAST, true) },
      )
      .pipe(
        map((r) => {
          this.save(r.data);
          return r.data.accessToken;
        }),
        catchError(() => of(null)),
        // Clear the in-flight handle once settled so the next refresh starts fresh.
        finalize(() => (this.refreshInFlight = null)),
        shareReplay(1),
      );

    return this.refreshInFlight;
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    this.user.set(null);
    this.refreshInFlight = null;
  }

  private save(data: AuthResult): void {
    localStorage.setItem(TOKEN_KEY, data.accessToken);
    if (data.refreshToken) localStorage.setItem(REFRESH_KEY, data.refreshToken);
    this.user.set(data.user);
  }
}
