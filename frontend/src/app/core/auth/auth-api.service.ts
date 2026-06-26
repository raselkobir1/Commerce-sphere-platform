import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../http/api.service';
import { PagedResult } from '../models/api-response';
import {
  AuthTokens,
  ChallengeVerifyRequest,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  LoginOutcome,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  Session,
  TwoFactorSetup,
  UpdateProfileRequest,
  User,
} from '../models/auth.models';

// One method per Auth service endpoint. Pure transport — no state. AuthService composes these.
@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly api = inject(ApiService);

  // ── Session ───────────────────────────────────────────────────────────────
  register(body: RegisterRequest): Observable<AuthTokens> {
    return this.api.post<AuthTokens>('/api/auth/register', body);
  }

  // Login resolves to tokens OR a step-up challenge; normalise the three response shapes.
  login(body: LoginRequest): Observable<LoginOutcome> {
    return this.api.post<Record<string, unknown>>('/api/auth/login', body).pipe(
      map((data) => {
        if ((data as { requiresTwoFactor?: boolean }).requiresTwoFactor) {
          return { kind: 'twoFactor', challengeToken: data['challengeToken'] as string };
        }
        if ((data as { requiresOtp?: boolean }).requiresOtp) {
          return { kind: 'otp', challengeToken: data['challengeToken'] as string };
        }
        return { kind: 'tokens', tokens: data as unknown as AuthTokens };
      }),
    );
  }

  refreshToken(refreshToken: string): Observable<AuthTokens> {
    return this.api.post<AuthTokens>('/api/auth/refresh-token', { refreshToken });
  }

  revokeToken(refreshToken: string): Observable<void> {
    return this.api.post<void>('/api/auth/revoke-token', { refreshToken });
  }

  me(): Observable<User> {
    return this.api.get<User>('/api/auth/me');
  }

  // Admin only.
  users(pageNumber = 1, pageSize = 20): Observable<PagedResult<User>> {
    return this.api.get<PagedResult<User>>('/api/auth/users', { pageNumber, pageSize });
  }

  // ── Login challenge verification ────────────────────────────────────────────
  verifyTwoFactor(body: ChallengeVerifyRequest): Observable<AuthTokens> {
    return this.api.post<AuthTokens>('/api/auth/2fa/verify', body);
  }

  verifyOtp(body: ChallengeVerifyRequest): Observable<AuthTokens> {
    return this.api.post<AuthTokens>('/api/auth/otp/verify', body);
  }

  // ── Account ─────────────────────────────────────────────────────────────────
  updateProfile(body: UpdateProfileRequest): Observable<User> {
    return this.api.patch<User>('/api/auth/me', body);
  }

  changePassword(body: ChangePasswordRequest): Observable<void> {
    return this.api.post<void>('/api/auth/change-password', body);
  }

  sessions(): Observable<Session[]> {
    return this.api.get<Session[]>('/api/auth/sessions');
  }

  revokeAllSessions(): Observable<void> {
    return this.api.delete<void>('/api/auth/sessions');
  }

  // ── Email verification ───────────────────────────────────────────────────────
  sendVerificationEmail(): Observable<void> {
    return this.api.post<void>('/api/auth/email/verify/send');
  }

  resendVerificationEmail(email: string): Observable<void> {
    return this.api.post<void>('/api/auth/email/verify/resend', { email });
  }

  confirmEmail(token: string): Observable<void> {
    return this.api.get<void>('/api/auth/email/verify/confirm', { token });
  }

  // ── Password reset ────────────────────────────────────────────────────────────
  forgotPassword(body: ForgotPasswordRequest): Observable<void> {
    return this.api.post<void>('/api/auth/password/forgot', body);
  }

  resetPassword(body: ResetPasswordRequest): Observable<void> {
    return this.api.post<void>('/api/auth/password/reset', body);
  }

  // ── Two-factor (TOTP) ───────────────────────────────────────────────────────
  setupTwoFactor(): Observable<TwoFactorSetup> {
    return this.api.post<TwoFactorSetup>('/api/auth/2fa/setup');
  }

  confirmTwoFactor(code: string): Observable<AuthTokens> {
    return this.api.post<AuthTokens>('/api/auth/2fa/confirm', { code });
  }

  disableTwoFactor(code: string): Observable<void> {
    return this.api.post<void>('/api/auth/2fa/disable', { code });
  }

  // ── OTP (email one-time code on login) ────────────────────────────────────────
  toggleOtp(enable: boolean): Observable<void> {
    return this.api.post<void>('/api/auth/otp/toggle', { enable });
  }

  // ── SSO ────────────────────────────────────────────────────────────────────────
  ssoProviders(): Observable<string[]> {
    return this.api.get<string[]>('/api/auth/sso/providers');
  }
}
