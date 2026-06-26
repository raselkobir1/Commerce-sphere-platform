import { TestBed } from '@angular/core/testing';
import { TokenStorageService } from './token-storage.service';

describe('TokenStorageService', () => {
  let service: TokenStorageService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenStorageService);
  });

  it('starts unauthenticated', () => {
    expect(service.isAuthenticated).toBe(false);
    expect(service.accessToken).toBeNull();
  });

  it('persists and reads tokens', () => {
    service.set({ accessToken: 'a', refreshToken: 'r' });
    expect(service.accessToken).toBe('a');
    expect(service.refreshToken).toBe('r');
    expect(service.isAuthenticated).toBe(true);
  });

  it('clears tokens', () => {
    service.set({ accessToken: 'a', refreshToken: 'r' });
    service.clear();
    expect(service.accessToken).toBeNull();
    expect(service.isAuthenticated).toBe(false);
  });
});
