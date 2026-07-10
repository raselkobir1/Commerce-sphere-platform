import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Api } from './api';

interface SsoLoginUrl {
  provider: string;
  authorizationUrl: string;
  state: string;
}

// One provider in the catalog: always listed; `enabled` is false until its credentials are configured.
export interface SsoProvider {
  name: string;
  enabled: boolean;
}

// Social login (Google / Facebook) via direct OAuth to each provider.
@Injectable({ providedIn: 'root' })
export class Sso {
  private api = inject(Api);

  // Full catalog of supported providers (each with an `enabled` flag) — used to render the buttons.
  providers(): Observable<SsoProvider[]> {
    return this.api.get<SsoProvider[]>('/api/auth/sso/providers');
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
