import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { Api } from './api';
import { AuthResult, User } from './models';

const TOKEN_KEY = 'shopsphere.token';

// Holds the signed-in customer and talks to the Auth service.
@Injectable({ providedIn: 'root' })
export class Auth {
  private api = inject(Api);

  user = signal<User | null>(null);
  isLoggedIn = computed(() => this.user() !== null);

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  login(email: string, password: string): Observable<User> {
    return this.api.post<AuthResult>('/api/auth/login', { email, password }).pipe(
      map((data) => {
        if (!data?.accessToken) {
          throw new Error('This account requires extra verification, which is not supported here.');
        }
        this.save(data);
        return data.user;
      }),
    );
  }

  register(firstName: string, lastName: string, email: string, password: string): Observable<User> {
    return this.api
      .post<AuthResult>('/api/auth/register', { firstName, lastName, email, password, role: 'Customer' })
      .pipe(
        tap((data) => this.save(data)),
        map((data) => data.user),
      );
  }

  // Called once at startup to restore the session from the saved token.
  restore(): Observable<User | null> {
    if (!this.token) return of(null);
    return this.api.get<User>('/api/auth/me').pipe(
      tap((u) => this.user.set(u)),
      catchError(() => {
        this.logout();
        return of(null);
      }),
    );
  }

  // Finish a social login: store the access token returned on the SSO callback URL, then load
  // the user from /me (the callback only carries tokens, not the user object).
  completeSsoLogin(accessToken: string): Observable<User | null> {
    localStorage.setItem(TOKEN_KEY, accessToken);
    return this.restore();
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
