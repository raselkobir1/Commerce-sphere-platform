import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { I18n } from '../../core/i18n';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-forgot-password',
  imports: [FormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="container auth-wrap">
      <div class="panel">
        @if (sent()) {
          <h1>{{ 'fp.checkEmail' | t }}</h1>
          <p class="muted">{{ 'fp.checkEmailBody' | t }}</p>
          <a class="btn btn-primary btn-block" routerLink="/login">{{ 'fp.backToSignIn' | t }}</a>
        } @else {
          <h1>{{ 'fp.title' | t }}</h1>
          <p class="muted">{{ 'fp.instructions' | t }}</p>
          <form (ngSubmit)="submit()">
            <div class="field">
              <label>{{ 'common.email' | t }}</label>
              <input class="input" type="email" name="email" [(ngModel)]="email" required />
            </div>

            @if (error()) {
              <p class="error">{{ error() }}</p>
            }

            <button class="btn btn-primary btn-block" type="submit" [disabled]="loading()">
              {{ (loading() ? 'fp.sending' : 'fp.sendResetLink') | t }}
            </button>
          </form>

          <p class="muted" style="margin-top:16px">
            <a routerLink="/login">{{ 'fp.backToSignIn' | t }}</a>
          </p>
        }
      </div>
    </div>
  `,
})
export class ForgotPasswordPage {
  private api = inject(Api);
  i18n = inject(I18n);

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
        this.error.set(err?.error?.message ?? this.i18n.t('fp.genericError'));
      },
    });
  }
}
