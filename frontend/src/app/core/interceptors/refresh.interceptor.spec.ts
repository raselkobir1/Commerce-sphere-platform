import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { TokenStorageService } from '../auth/token-storage.service';
import { refreshInterceptor } from './refresh.interceptor';

const base = environment.apiBaseUrl;
const newTokens = {
  accessToken: 'newaccess',
  refreshToken: 'newrefresh',
  expiresAt: '2026',
  user: {
    id: 'u',
    email: 'a@test.dev',
    firstName: 'A',
    lastName: 'B',
    role: 'Customer',
    isActive: true,
    emailVerified: true,
    isActiveTwoFactor: false,
    twoFactorConfirmed: false,
    isOtpAuthEnable: false,
    createdAt: '2026',
    lastLoginAt: null,
  },
};

function envelope<T>(data: T) {
  return { success: true, message: '', data, errors: [], traceId: '', correlationId: '' };
}

describe('refreshInterceptor', () => {
  let http: HttpClient;
  let mock: HttpTestingController;
  let storage: TokenStorageService;
  const routerStub = { navigate: () => Promise.resolve(true) };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([refreshInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: routerStub },
      ],
    });
    http = TestBed.inject(HttpClient);
    mock = TestBed.inject(HttpTestingController);
    storage = TestBed.inject(TokenStorageService);
    storage.set({ accessToken: 'old', refreshToken: 'oldrefresh' });
  });

  afterEach(() => mock.verify());

  it('on 401 it refreshes the token once and retries the original request', () => {
    let result: unknown;
    http.get(`${base}/api/data`).subscribe((r) => (result = r));

    // 1. Original request fails with 401.
    mock.expectOne(`${base}/api/data`).flush(null, { status: 401, statusText: 'Unauthorized' });

    // 2. Interceptor calls the refresh endpoint.
    const refresh = mock.expectOne(`${base}/api/auth/refresh-token`);
    expect(refresh.request.body).toEqual({ refreshToken: 'oldrefresh' });
    refresh.flush(envelope(newTokens));

    // 3. Original request is retried with the new bearer token.
    const retried = mock.expectOne(`${base}/api/data`);
    expect(retried.request.headers.get('Authorization')).toBe('Bearer newaccess');
    retried.flush({ ok: true });

    expect(result).toEqual({ ok: true });
    expect(storage.accessToken).toBe('newaccess');
  });

  it('when refresh fails it logs out and does not retry', () => {
    const auth = TestBed.inject(AuthService);
    const logoutSpy = vi.spyOn(auth, 'logout').mockReturnValue(of(null));

    let errored = false;
    http.get(`${base}/api/data`).subscribe({ error: () => (errored = true) });

    mock.expectOne(`${base}/api/data`).flush(null, { status: 401, statusText: 'Unauthorized' });
    mock.expectOne(`${base}/api/auth/refresh-token`).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(errored).toBe(true);
    expect(logoutSpy).toHaveBeenCalled();
  });
});
