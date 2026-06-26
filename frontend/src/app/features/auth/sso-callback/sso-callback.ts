import { Component, OnInit, inject, signal } from '@angular/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { TokenStorageService } from '../../../core/auth/token-storage.service';
import { homePathForRole } from '../../../core/guards/home-redirect';
import { NotificationService } from '../../../core/notifications/notification.service';

// Landing page for the Keycloak/SSO redirect. The backend redirects here with the issued tokens
// in the query string (access_token / refresh_token / expires_at) or sso_error on failure.
@Component({
  selector: 'app-sso-callback',
  imports: [RouterLink, MatProgressBarModule],
  template: `
    @if (failed()) {
      <h1 class="title">Sign-in failed</h1>
      <p class="subtitle">{{ message() }}</p>
      <p class="foot"><a routerLink="/auth/login">Back to sign in</a></p>
    } @else {
      <mat-progress-bar mode="indeterminate" />
      <h1 class="title">Completing sign-in…</h1>
      <p class="subtitle">Hang tight while we finish setting up your session.</p>
    }
  `,
  styleUrl: './sso-callback.scss',
})
export class SsoCallback implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly storage = inject(TokenStorageService);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);

  readonly failed = signal(false);
  readonly message = signal('');

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const ssoError = params.get('sso_error');
    if (ssoError) {
      this.fail(ssoError);
      return;
    }

    const accessToken = params.get('access_token');
    const refreshToken = params.get('refresh_token');
    if (!accessToken || !refreshToken) {
      this.fail('Missing tokens in the sign-in response.');
      return;
    }

    this.storage.set({ accessToken, refreshToken });
    // We have tokens but not the user object — fetch it, then route by role.
    this.auth.loadCurrentUser().subscribe({
      next: () => {
        this.notify.success('Signed in successfully.');
        void this.router.navigateByUrl(homePathForRole(this.auth.role()));
      },
      error: () => this.fail('Could not load your profile after sign-in.'),
    });
  }

  private fail(message: string): void {
    this.failed.set(true);
    this.message.set(message);
  }
}
