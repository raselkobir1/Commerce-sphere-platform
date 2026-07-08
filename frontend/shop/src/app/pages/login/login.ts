import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { SsoButtons } from '../../layout/sso-buttons';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, SsoButtons],
  template: `
    <div class="container auth-wrap">
      <div class="panel">
        @if (!challengeToken()) {
          <h1>Sign in</h1>
          <form (ngSubmit)="submit()">
            <div class="field">
              <label>Email</label>
              <input class="input" type="email" name="email" [(ngModel)]="email" required />
            </div>
            <div class="field">
              <label>Password</label>
              <div class="pw-field">
                <input class="input" [type]="showPw() ? 'text' : 'password'" name="password" [(ngModel)]="password" required />
                <button type="button" class="pw-toggle" (click)="showPw.set(!showPw())" [attr.aria-label]="showPw() ? 'Hide password' : 'Show password'">
                  @if (showPw()) {
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a21.8 21.8 0 0 1 5.06-6.06M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a21.8 21.8 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>
                  } @else {
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z"></path><circle cx="12" cy="12" r="3"></circle></svg>
                  }
                </button>
              </div>
              <div style="text-align:right; margin-top:8px">
                <a routerLink="/forgot-password" style="font-size:13px">Forgot password?</a>
              </div>
            </div>

            @if (error()) {
              <p class="error">{{ error() }}</p>
            }

            <button class="btn btn-primary btn-block" type="submit" [disabled]="loading()">
              {{ loading() ? 'Signing in…' : 'Sign in' }}
            </button>
          </form>

          <app-sso-buttons />

          <p class="muted" style="margin-top:16px">
            New here? <a routerLink="/register">Create an account</a>
          </p>
        } @else {
          <h1>Set a new password</h1>
          <p class="muted">Your temporary password must be replaced before you can continue.</p>
          <form (ngSubmit)="submitNewPassword()">
            <div class="field">
              <label>New password</label>
              <div class="pw-field">
                <input class="input" [type]="showNewPw() ? 'text' : 'password'" name="np" [(ngModel)]="newPassword" required minlength="8" />
                <button type="button" class="pw-toggle" (click)="showNewPw.set(!showNewPw())" [attr.aria-label]="showNewPw() ? 'Hide password' : 'Show password'">
                  @if (showNewPw()) {
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a21.8 21.8 0 0 1 5.06-6.06M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a21.8 21.8 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>
                  } @else {
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z"></path><circle cx="12" cy="12" r="3"></circle></svg>
                  }
                </button>
              </div>
            </div>
            <div class="field">
              <label>Confirm new password</label>
              <div class="pw-field">
                <input class="input" [type]="showConfirmPw() ? 'text' : 'password'" name="cf" [(ngModel)]="confirmPassword" required minlength="8" />
                <button type="button" class="pw-toggle" (click)="showConfirmPw.set(!showConfirmPw())" [attr.aria-label]="showConfirmPw() ? 'Hide password' : 'Show password'">
                  @if (showConfirmPw()) {
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a21.8 21.8 0 0 1 5.06-6.06M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a21.8 21.8 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>
                  } @else {
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z"></path><circle cx="12" cy="12" r="3"></circle></svg>
                  }
                </button>
              </div>
            </div>

            @if (error()) {
              <p class="error">{{ error() }}</p>
            }

            <button class="btn btn-primary btn-block" type="submit" [disabled]="loading()">
              {{ loading() ? 'Updating…' : 'Set password & sign in' }}
            </button>
          </form>
        }
      </div>
    </div>
  `,
})
export class LoginPage {
  private auth = inject(Auth);
  private cart = inject(Cart);
  private router = inject(Router);

  email = '';
  password = '';
  loading = signal(false);
  error = signal('');
  showPw = signal(false);

  challengeToken = signal<string | null>(null);
  newPassword = '';
  confirmPassword = '';
  showNewPw = signal(false);
  showConfirmPw = signal(false);

  submit(): void {
    this.error.set('');
    this.loading.set(true);
    this.auth.login(this.email, this.password).subscribe({
      next: (outcome) => {
        if (outcome.kind === 'passwordChange') {
          this.loading.set(false);
          this.challengeToken.set(outcome.challengeToken);
          return;
        }
        this.loading.set(false);
        this.cart.load();
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? err?.message ?? 'Sign in failed.');
      },
    });
  }

  submitNewPassword(): void {
    this.error.set('');
    if (this.newPassword.length < 8) { this.error.set('New password must be at least 8 characters.'); return; }
    if (this.newPassword !== this.confirmPassword) { this.error.set('Passwords do not match.'); return; }
    this.loading.set(true);
    this.auth.completeForcedPasswordChange(this.challengeToken()!, this.newPassword).subscribe({
      next: () => {
        this.loading.set(false);
        this.cart.load();
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? 'Could not update your password.');
      },
    });
  }
}
