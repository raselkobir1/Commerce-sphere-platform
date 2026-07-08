import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Api } from '../../core/api';

@Component({
  selector: 'app-forgot-password',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="container auth-wrap">
      <div class="panel">
        @if (sent()) {
          <h1>Check your email</h1>
          <p class="muted">If that address is registered, we've sent a link to reset your password. The link expires in 30 minutes.</p>
          <a class="btn btn-primary btn-block" routerLink="/login">Back to sign in</a>
        } @else {
          <h1>Forgot password</h1>
          <p class="muted">Enter your email and we'll send you a reset link.</p>
          <form (ngSubmit)="submit()">
            <div class="field">
              <label>Email</label>
              <input class="input" type="email" name="email" [(ngModel)]="email" required />
            </div>

            @if (error()) {
              <p class="error">{{ error() }}</p>
            }

            <button class="btn btn-primary btn-block" type="submit" [disabled]="loading()">
              {{ loading() ? 'Sending…' : 'Send reset link' }}
            </button>
          </form>

          <p class="muted" style="margin-top:16px">
            <a routerLink="/login">Back to sign in</a>
          </p>
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
    this.api.post('/api/auth/password/forgot', { email: this.email }).subscribe({
      next: () => { this.loading.set(false); this.sent.set(true); },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? 'Something went wrong. Please try again.');
      },
    });
  }
}
