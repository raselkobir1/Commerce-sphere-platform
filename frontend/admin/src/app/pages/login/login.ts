import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { Perms } from '../../core/perms';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="login-wrap">
      <aside class="login-aside">
        <div class="big">Run your store with<br />Admin<span style="color:#c7d2fe">Sphere</span></div>
        <p>Manage products, categories, inventory and customers from one modern dashboard.</p>
        <ul>
          <li>Product & catalog management</li>
          <li>Live inventory control</li>
          <li>Customer insights</li>
        </ul>
      </aside>

      <div class="login-main">
        @if (!challengeToken()) {
          <form class="login-card" (ngSubmit)="submit()">
            <div class="lead">Admin<span style="color:var(--brand)">Sphere</span></div>
            <p class="sub">Sign in to your dashboard</p>

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

            <button class="btn btn-primary" style="width:100%; justify-content:center" type="submit" [disabled]="loading()">
              {{ loading() ? 'Signing in…' : 'Sign in' }}
            </button>
          </form>
        } @else {
          <form class="login-card" (ngSubmit)="submitNewPassword()">
            <div class="lead">Set a new password</div>
            <p class="sub">Your temporary password must be replaced before you can continue.</p>

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

            <button class="btn btn-primary" style="width:100%; justify-content:center" type="submit" [disabled]="loading()">
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
  private perms = inject(Perms);
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
        this.afterAuthenticated();
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
      next: () => this.afterAuthenticated(),
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? 'Could not update your password.');
      },
    });
  }

  // Load the user's permitted menus; if they have none, they have no admin access.
  private afterAuthenticated(): void {
    this.perms.load().subscribe({
      next: (menus) => {
        this.loading.set(false);
        if (menus.length === 0) {
          this.auth.logout();
          this.perms.clear();
          this.error.set('This account has no admin access.');
          return;
        }
        this.router.navigate([menus[0].route]);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Could not load your permissions.');
      },
    });
  }
}
