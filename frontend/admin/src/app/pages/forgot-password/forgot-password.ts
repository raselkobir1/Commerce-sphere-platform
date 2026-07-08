import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Api } from '../../core/api';

@Component({
  selector: 'app-forgot-password',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="login-wrap">
      <aside class="login-aside">
        <div class="big">Run your store with<br />Admin<span style="color:#c7d2fe">Sphere</span></div>
        <p>Manage products, categories, inventory and customers from one modern dashboard.</p>
      </aside>

      <div class="login-main">
        @if (sent()) {
          <div class="login-card" style="text-align:center">
            <div class="lead">Check your email</div>
            <p class="sub">If that address is registered, we've sent a link to reset your password. The link expires in 30 minutes.</p>
            <a class="btn btn-primary" style="width:100%; justify-content:center" routerLink="/login">Back to sign in</a>
          </div>
        } @else {
          <form class="login-card" (ngSubmit)="submit()">
            <div class="lead">Forgot password</div>
            <p class="sub">Enter your email and we'll send you a reset link.</p>

            <div class="field">
              <label>Email</label>
              <input class="input" type="email" name="email" [(ngModel)]="email" required />
            </div>

            @if (error()) {
              <p class="error">{{ error() }}</p>
            }

            <button class="btn btn-primary" style="width:100%; justify-content:center" type="submit" [disabled]="loading()">
              {{ loading() ? 'Sending…' : 'Send reset link' }}
            </button>

            <p class="muted" style="margin-top:16px; text-align:center">
              <a routerLink="/login">Back to sign in</a>
            </p>
          </form>
        }
      </div>
    </div>
  `,
})
export class ForgotPasswordPage {
  private api = inject(Api);

  email = '';
  loading = signal(false);
  error = signal('');
  sent = signal(false);

  submit(): void {
    this.error.set('');
    this.loading.set(true);
    this.api.post('/api/auth/password/forgot', { email: this.email }, { toastSuccess: false }).subscribe({
      next: () => { this.loading.set(false); this.sent.set(true); },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? 'Something went wrong. Please try again.');
      },
    });
  }
}
