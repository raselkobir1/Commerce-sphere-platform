import { TitleCasePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { AuthApiService } from '../../../core/auth/auth-api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { homePathForRole } from '../../../core/guards/home-redirect';

type Mode = 'credentials' | 'twoFactor' | 'otp';

@Component({
  selector: 'app-login',
  imports: [
    TitleCasePipe,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatDividerModule,
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly authApi = inject(AuthApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(false);
  readonly mode = signal<Mode>('credentials');
  readonly providers = signal<string[]>([]);
  private challengeToken = '';
  private returnUrl: string | null = null;

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  readonly codeForm = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]],
  });

  ngOnInit(): void {
    this.returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    // SSO buttons are best-effort; if the call fails the error interceptor toasts and we show none.
    this.authApi.ssoProviders().subscribe({
      next: (providers) => this.providers.set(providers ?? []),
      error: () => this.providers.set([]),
    });
  }

  submit(): void {
    if (this.form.invalid || this.loading()) return;
    this.loading.set(true);
    this.auth.login(this.form.getRawValue()).subscribe({
      next: (outcome) => {
        this.loading.set(false);
        if (outcome.kind === 'tokens') {
          this.goHome();
        } else {
          this.challengeToken = outcome.challengeToken;
          this.mode.set(outcome.kind);
        }
      },
      error: () => this.loading.set(false),
    });
  }

  verify(): void {
    if (this.codeForm.invalid || this.loading()) return;
    this.loading.set(true);
    const body = { challengeToken: this.challengeToken, code: this.codeForm.getRawValue().code };
    const call = this.mode() === 'twoFactor' ? this.auth.verifyTwoFactor(body) : this.auth.verifyOtp(body);
    call.subscribe({
      next: () => {
        this.loading.set(false);
        this.goHome();
      },
      error: () => this.loading.set(false),
    });
  }

  cancelChallenge(): void {
    this.mode.set('credentials');
    this.codeForm.reset();
  }

  loginWithProvider(provider: string): void {
    const redirectUri = `${window.location.origin}/auth/sso/callback`;
    window.location.href = `${environment.apiBaseUrl}/api/auth/sso/login/${provider}?redirectUri=${encodeURIComponent(redirectUri)}`;
  }

  private goHome(): void {
    const target = this.returnUrl ?? homePathForRole(this.auth.role());
    void this.router.navigateByUrl(target);
  }
}
