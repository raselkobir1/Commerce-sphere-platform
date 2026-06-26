import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { TokenStorageService } from '../auth/token-storage.service';
import { User } from '../models/auth.models';
import { authGuard } from './auth.guard';
import { guestGuard } from './guest.guard';
import { homePathForRole } from './home-redirect';
import { roleGuard } from './role.guard';

function makeUser(role: 'Admin' | 'Customer'): User {
  return {
    id: 'u',
    email: 'a@test.dev',
    firstName: 'A',
    lastName: 'B',
    role,
    isActive: true,
    emailVerified: true,
    isActiveTwoFactor: false,
    twoFactorConfirmed: false,
    isOtpAuthEnable: false,
    createdAt: '2026-01-01',
    lastLoginAt: null,
  };
}

describe('route guards', () => {
  let storage: TokenStorageService;
  let auth: AuthService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    storage = TestBed.inject(TokenStorageService);
    auth = TestBed.inject(AuthService);
  });

  describe('homePathForRole', () => {
    it('routes Admin to /admin and everyone else to /shop', () => {
      expect(homePathForRole('Admin')).toBe('/admin');
      expect(homePathForRole('Customer')).toBe('/shop');
      expect(homePathForRole(null)).toBe('/shop');
    });
  });

  describe('authGuard', () => {
    it('allows authenticated users', () => {
      storage.set({ accessToken: 'a', refreshToken: 'r' });
      const result = TestBed.runInInjectionContext(() =>
        authGuard({} as ActivatedRouteSnapshot, { url: '/admin' } as RouterStateSnapshot),
      );
      expect(result).toBe(true);
    });

    it('redirects anonymous users to login with returnUrl', () => {
      const result = TestBed.runInInjectionContext(() =>
        authGuard({} as ActivatedRouteSnapshot, { url: '/admin' } as RouterStateSnapshot),
      );
      expect(result instanceof UrlTree).toBe(true);
      expect((result as UrlTree).toString()).toContain('/auth/login');
      expect((result as UrlTree).toString()).toContain('returnUrl');
    });
  });

  describe('roleGuard', () => {
    it('allows a user whose role is permitted', () => {
      auth.setUser(makeUser('Admin'));
      const route = { data: { roles: ['Admin'] } } as unknown as ActivatedRouteSnapshot;
      const result = TestBed.runInInjectionContext(() => roleGuard(route, {} as RouterStateSnapshot));
      expect(result).toBe(true);
    });

    it('redirects a user lacking the role to their home', () => {
      auth.setUser(makeUser('Customer'));
      const route = { data: { roles: ['Admin'] } } as unknown as ActivatedRouteSnapshot;
      const result = TestBed.runInInjectionContext(() => roleGuard(route, {} as RouterStateSnapshot));
      expect(result instanceof UrlTree).toBe(true);
      expect((result as UrlTree).toString()).toContain('/shop');
    });
  });

  describe('guestGuard', () => {
    it('allows anonymous visitors', () => {
      const result = TestBed.runInInjectionContext(() =>
        guestGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
      );
      expect(result).toBe(true);
    });

    it('bounces authenticated users to their home', () => {
      storage.set({ accessToken: 'a', refreshToken: 'r' });
      auth.setUser(makeUser('Admin'));
      const result = TestBed.runInInjectionContext(() =>
        guestGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
      );
      expect(result instanceof UrlTree).toBe(true);
      expect((result as UrlTree).toString()).toContain('/admin');
    });
  });
});
