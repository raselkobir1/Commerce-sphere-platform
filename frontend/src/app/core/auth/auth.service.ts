import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap } from 'rxjs';
import {
  AuthTokens,
  ChallengeVerifyRequest,
  LoginOutcome,
  LoginRequest,
  RegisterRequest,
  User,
  UserRole,
} from '../models/auth.models';
import { AuthApiService } from './auth-api.service';
import { TokenStorageService } from './token-storage.service';

// Holds session state (the current user) as signals and orchestrates the auth flows on top of
// AuthApiService. Components and guards read `user()` / `isAdmin()` reactively.
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(AuthApiService);
  private readonly storage = inject(TokenStorageService);

  private readonly _user = signal<User | null>(null);
  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly isAdmin = computed(() => this._user()?.role === 'Admin');
  readonly role = computed<UserRole | null>(() => this._user()?.role ?? null);

  get refreshToken(): string | null {
    return this.storage.refreshToken;
  }

  register(body: RegisterRequest): Observable<AuthTokens> {
    return this.api.register(body).pipe(tap((tokens) => this.setSession(tokens)));
  }

  // Returns the raw outcome so the login page can branch to a 2FA/OTP step-up screen.
  login(body: LoginRequest): Observable<LoginOutcome> {
    return this.api.login(body).pipe(
      tap((outcome) => {
        if (outcome.kind === 'tokens') this.setSession(outcome.tokens);
      }),
    );
  }

  verifyTwoFactor(body: ChallengeVerifyRequest): Observable<AuthTokens> {
    return this.api.verifyTwoFactor(body).pipe(tap((tokens) => this.setSession(tokens)));
  }

  verifyOtp(body: ChallengeVerifyRequest): Observable<AuthTokens> {
    return this.api.verifyOtp(body).pipe(tap((tokens) => this.setSession(tokens)));
  }

  // Called once at startup (APP_INITIALIZER) when a token exists, to rehydrate the user.
  loadCurrentUser(): Observable<User | null> {
    if (!this.storage.isAuthenticated) {
      this._user.set(null);
      return of(null);
    }
    return this.api.me().pipe(
      tap((user) => this._user.set(user)),
      catchError(() => {
        this.storage.clear();
        this._user.set(null);
        return of(null);
      }),
    );
  }

  // Keeps the signal in sync after a profile update elsewhere.
  setUser(user: User): void {
    this._user.set(user);
  }

  logout(): Observable<unknown> {
    const refresh = this.storage.refreshToken;
    const finalize = () => {
      this.storage.clear();
      this._user.set(null);
    };
    // Best-effort server-side revoke; clear locally regardless of the result.
    if (!refresh) {
      finalize();
      return of(null);
    }
    return this.api.revokeToken(refresh).pipe(
      catchError(() => of(null)),
      tap(finalize),
    );
  }

  setSession(tokens: AuthTokens): void {
    this.storage.set(tokens);
    this._user.set(tokens.user);
  }
}
