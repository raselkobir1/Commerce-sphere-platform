import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Api } from './api';

interface SsoLoginUrl {
  provider: string;
  authorizationUrl: string;
  state: string;
}

// Social login (Google / GitHub / Facebook) brokered by Keycloak.
@Injectable({ providedIn: 'root' })
export class Sso {
  private api = inject(Api);

  // Providers the backend has enabled — used to render the buttons.
  providers(): Observable<string[]> {
    return this.api.get<string[]>('/api/auth/sso/providers');
  }

  // Ask the backend for the provider's authorization URL, then send the browser there.
  // After login the backend redirects back to <origin>/sso-callback with the tokens.
  start(provider: string): void {
    const redirectUri = `${window.location.origin}/sso-callback`;
    this.api
      .get<SsoLoginUrl>(`/api/auth/sso/login/${provider}`, { redirectUri })
      .subscribe((res) => (window.location.href = res.authorizationUrl));
  }
}
