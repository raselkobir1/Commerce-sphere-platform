import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response';
import { AuthTokens, User } from '../models/auth.models';
import { AuthService } from './auth.service';
import { TokenStorageService } from './token-storage.service';

function envelope<T>(data: T): ApiResponse<T> {
  return { success: true, message: 'ok', data, errors: [], traceId: 't', correlationId: 'c' };
}

const base = environment.apiBaseUrl;
const user: User = {
  id: 'u1',
  email: 'a@test.dev',
  firstName: 'A',
  lastName: 'B',
  role: 'Admin',
  isActive: true,
  emailVerified: true,
  isActiveTwoFactor: false,
  twoFactorConfirmed: false,
  isOtpAuthEnable: false,
  createdAt: '2026-01-01',
  lastLoginAt: null,
};
const tokens: AuthTokens = { accessToken: 'a', refreshToken: 'r', expiresAt: '2026', user };

describe('AuthService', () => {
  let service: AuthService;
  let storage: TokenStorageService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AuthService);
    storage = TestBed.inject(TokenStorageService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('login with tokens sets the session and exposes the user + role', () => {
    service.login({ email: 'a@test.dev', password: 'x' }).subscribe();
    http.expectOne(`${base}/api/auth/login`).flush(envelope(tokens));

    expect(storage.accessToken).toBe('a');
    expect(service.user()?.email).toBe('a@test.dev');
    expect(service.isAuthenticated()).toBe(true);
    expect(service.isAdmin()).toBe(true);
  });

  it('login with a 2FA challenge does NOT establish a session', () => {
    let outcomeKind: string | undefined;
    service.login({ email: 'a@test.dev', password: 'x' }).subscribe((o) => (outcomeKind = o.kind));
    http.expectOne(`${base}/api/auth/login`).flush(envelope({ requiresTwoFactor: true, challengeToken: 'c' }));

    expect(outcomeKind).toBe('twoFactor');
    expect(storage.isAuthenticated).toBe(false);
    expect(service.user()).toBeNull();
  });

  it('verifyTwoFactor establishes the session', () => {
    service.verifyTwoFactor({ challengeToken: 'c', code: '123456' }).subscribe();
    http.expectOne(`${base}/api/auth/2fa/verify`).flush(envelope(tokens));
    expect(service.isAuthenticated()).toBe(true);
  });

  it('logout revokes the refresh token and clears state', () => {
    service.setSession(tokens);
    expect(service.isAuthenticated()).toBe(true);

    service.logout().subscribe();
    http.expectOne(`${base}/api/auth/revoke-token`).flush(envelope(null));

    expect(storage.accessToken).toBeNull();
    expect(service.user()).toBeNull();
  });

  it('loadCurrentUser hydrates the user when a token exists', () => {
    storage.set({ accessToken: 'a', refreshToken: 'r' });
    service.loadCurrentUser().subscribe();
    http.expectOne(`${base}/api/auth/me`).flush(envelope(user));
    expect(service.user()?.id).toBe('u1');
  });

  it('loadCurrentUser is a no-op without a token', () => {
    service.loadCurrentUser().subscribe();
    http.expectNone(`${base}/api/auth/me`);
    expect(service.user()).toBeNull();
  });
});
