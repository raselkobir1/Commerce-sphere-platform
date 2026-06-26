import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response';
import { AuthTokens, User } from '../models/auth.models';
import { AuthApiService } from './auth-api.service';

// Wraps a payload in the backend's ApiResponse<T> envelope.
function envelope<T>(data: T): ApiResponse<T> {
  return { success: true, message: 'ok', data, errors: [], traceId: 't', correlationId: 'c' };
}

const base = environment.apiBaseUrl;

const sampleUser: User = {
  id: 'u1',
  email: 'a@test.dev',
  firstName: 'A',
  lastName: 'B',
  role: 'Customer',
  isActive: true,
  emailVerified: false,
  isActiveTwoFactor: false,
  twoFactorConfirmed: false,
  isOtpAuthEnable: false,
  createdAt: '2026-01-01T00:00:00Z',
  lastLoginAt: null,
};

const sampleTokens: AuthTokens = {
  accessToken: 'access',
  refreshToken: 'refresh',
  expiresAt: '2026-01-01T01:00:00Z',
  user: sampleUser,
};

describe('AuthApiService', () => {
  let service: AuthApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('register posts credentials and unwraps tokens', () => {
    let result: AuthTokens | undefined;
    service.register({ email: 'a@test.dev', password: 'Passw0rd!', firstName: 'A', lastName: 'B' }).subscribe((r) => (result = r));

    const req = http.expectOne(`${base}/api/auth/register`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.email).toBe('a@test.dev');
    req.flush(envelope(sampleTokens));
    expect(result).toEqual(sampleTokens);
  });

  it('login maps a tokens response to the "tokens" outcome', () => {
    let kind: string | undefined;
    service.login({ email: 'a@test.dev', password: 'x' }).subscribe((o) => (kind = o.kind));
    http.expectOne(`${base}/api/auth/login`).flush(envelope(sampleTokens));
    expect(kind).toBe('tokens');
  });

  it('login maps a 2FA challenge to the "twoFactor" outcome', () => {
    let token: string | undefined;
    service.login({ email: 'a@test.dev', password: 'x' }).subscribe((o) => {
      if (o.kind === 'twoFactor') token = o.challengeToken;
    });
    http
      .expectOne(`${base}/api/auth/login`)
      .flush(envelope({ requiresTwoFactor: true, challengeToken: 'chal-2fa' }));
    expect(token).toBe('chal-2fa');
  });

  it('login maps an OTP challenge to the "otp" outcome', () => {
    let token: string | undefined;
    service.login({ email: 'a@test.dev', password: 'x' }).subscribe((o) => {
      if (o.kind === 'otp') token = o.challengeToken;
    });
    http.expectOne(`${base}/api/auth/login`).flush(envelope({ requiresOtp: true, challengeToken: 'chal-otp' }));
    expect(token).toBe('chal-otp');
  });

  it('refreshToken posts the refresh token', () => {
    service.refreshToken('rt').subscribe();
    const req = http.expectOne(`${base}/api/auth/refresh-token`);
    expect(req.request.body).toEqual({ refreshToken: 'rt' });
    req.flush(envelope(sampleTokens));
  });

  it('revokeToken posts the refresh token', () => {
    service.revokeToken('rt').subscribe();
    const req = http.expectOne(`${base}/api/auth/revoke-token`);
    expect(req.request.method).toBe('POST');
    req.flush(envelope(null));
  });

  it('me fetches the current user', () => {
    let result: User | undefined;
    service.me().subscribe((u) => (result = u));
    const req = http.expectOne(`${base}/api/auth/me`);
    expect(req.request.method).toBe('GET');
    req.flush(envelope(sampleUser));
    expect(result).toEqual(sampleUser);
  });

  it('users requests a page with query params', () => {
    const paged: PagedResult<User> = {
      items: [sampleUser],
      pageNumber: 2,
      pageSize: 5,
      totalRecords: 6,
      totalPages: 2,
      hasPreviousPage: true,
      hasNextPage: false,
    };
    service.users(2, 5).subscribe();
    const req = http.expectOne((r) => r.url === `${base}/api/auth/users`);
    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('5');
    req.flush(envelope(paged));
  });

  it('verifyTwoFactor posts the challenge token and code', () => {
    service.verifyTwoFactor({ challengeToken: 'c', code: '123456' }).subscribe();
    const req = http.expectOne(`${base}/api/auth/2fa/verify`);
    expect(req.request.body).toEqual({ challengeToken: 'c', code: '123456' });
    req.flush(envelope(sampleTokens));
  });

  it('verifyOtp posts to the otp verify endpoint', () => {
    service.verifyOtp({ challengeToken: 'c', code: '123456' }).subscribe();
    http.expectOne(`${base}/api/auth/otp/verify`).flush(envelope(sampleTokens));
  });

  it('updateProfile PATCHes the profile', () => {
    service.updateProfile({ firstName: 'X', lastName: 'Y' }).subscribe();
    const req = http.expectOne(`${base}/api/auth/me`);
    expect(req.request.method).toBe('PATCH');
    req.flush(envelope(sampleUser));
  });

  it('changePassword posts current + new password', () => {
    service.changePassword({ currentPassword: 'old', newPassword: 'New1!aaa' }).subscribe();
    const req = http.expectOne(`${base}/api/auth/change-password`);
    expect(req.request.body).toEqual({ currentPassword: 'old', newPassword: 'New1!aaa' });
    req.flush(envelope(null));
  });

  it('sessions lists and revokeAllSessions deletes', () => {
    service.sessions().subscribe();
    http.expectOne(`${base}/api/auth/sessions`).flush(envelope([]));

    service.revokeAllSessions().subscribe();
    const del = http.expectOne(`${base}/api/auth/sessions`);
    expect(del.request.method).toBe('DELETE');
    del.flush(envelope(null));
  });

  it('email verification endpoints', () => {
    service.sendVerificationEmail().subscribe();
    http.expectOne(`${base}/api/auth/email/verify/send`).flush(envelope(null));

    service.resendVerificationEmail('a@test.dev').subscribe();
    const resend = http.expectOne(`${base}/api/auth/email/verify/resend`);
    expect(resend.request.body).toEqual({ email: 'a@test.dev' });
    resend.flush(envelope(null));

    service.confirmEmail('tok').subscribe();
    const confirm = http.expectOne((r) => r.url === `${base}/api/auth/email/verify/confirm`);
    expect(confirm.request.params.get('token')).toBe('tok');
    confirm.flush(envelope(null));
  });

  it('password reset endpoints', () => {
    service.forgotPassword({ email: 'a@test.dev' }).subscribe();
    http.expectOne(`${base}/api/auth/password/forgot`).flush(envelope(null));

    service.resetPassword({ token: 'tok', newPassword: 'New1!aaa' }).subscribe();
    const reset = http.expectOne(`${base}/api/auth/password/reset`);
    expect(reset.request.body).toEqual({ token: 'tok', newPassword: 'New1!aaa' });
    reset.flush(envelope(null));
  });

  it('2FA setup/confirm/disable endpoints', () => {
    service.setupTwoFactor().subscribe();
    http
      .expectOne(`${base}/api/auth/2fa/setup`)
      .flush(envelope({ secretKey: 's', qrCodeUri: 'otpauth://', manualEntrySegments: ['ABCD'] }));

    service.confirmTwoFactor('123456').subscribe();
    const confirm = http.expectOne(`${base}/api/auth/2fa/confirm`);
    expect(confirm.request.body).toEqual({ code: '123456' });
    confirm.flush(envelope(sampleTokens));

    service.disableTwoFactor('123456').subscribe();
    http.expectOne(`${base}/api/auth/2fa/disable`).flush(envelope(null));
  });

  it('toggleOtp posts the enable flag', () => {
    service.toggleOtp(true).subscribe();
    const req = http.expectOne(`${base}/api/auth/otp/toggle`);
    expect(req.request.body).toEqual({ enable: true });
    req.flush(envelope(null));
  });

  it('ssoProviders fetches the provider list', () => {
    let providers: string[] | undefined;
    service.ssoProviders().subscribe((p) => (providers = p));
    http.expectOne(`${base}/api/auth/sso/providers`).flush(envelope(['google', 'github']));
    expect(providers).toEqual(['google', 'github']);
  });
});
