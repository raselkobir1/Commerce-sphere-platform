import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { Api } from './api';
import { AuthResult, User } from './models';

const TOKEN_KEY = 'adminsphere.token';

// Holds the signed-in admin and talks to the Auth service.
@Injectable({ providedIn: 'root' })
export class Auth {
  private api = inject(Api);

  // The current user (null when signed out). Components read this reactively.
  user = signal<User | null>(null);
  isAdmin = computed(() => this.user()?.role === 'Admin');
  isAuthenticated = computed(() => this.user() !== null);

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
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
    // Silent session restore on startup — a failed/expired token just logs out, no toast.
    return this.api.get<User>('/api/auth/me', undefined, { toastError: false }).pipe(
      tap((u) => this.user.set(u)),
      catchError(() => {
        this.logout();
        return of(null);
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.user.set(null);
  }

  private save(data: AuthResult): void {
    localStorage.setItem(TOKEN_KEY, data.accessToken);
    this.user.set(data.user);
  }
}
