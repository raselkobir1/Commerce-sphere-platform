import { Injectable } from '@angular/core';
import { AuthTokens } from '../models/auth.models';

// Persists the access + refresh tokens. localStorage keeps the session across reloads/tabs;
// it is XSS-exposed by nature, which is why access tokens are short-lived and refresh rotates
// (the backend revokes the old refresh token on every use).
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private static readonly ACCESS = 'cs.accessToken';
  private static readonly REFRESH = 'cs.refreshToken';

  get accessToken(): string | null {
    return localStorage.getItem(TokenStorageService.ACCESS);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(TokenStorageService.REFRESH);
  }

  get isAuthenticated(): boolean {
    return !!this.accessToken;
  }

  set(tokens: Pick<AuthTokens, 'accessToken' | 'refreshToken'>): void {
    localStorage.setItem(TokenStorageService.ACCESS, tokens.accessToken);
    localStorage.setItem(TokenStorageService.REFRESH, tokens.refreshToken);
  }

  clear(): void {
    localStorage.removeItem(TokenStorageService.ACCESS);
    localStorage.removeItem(TokenStorageService.REFRESH);
  }
}
