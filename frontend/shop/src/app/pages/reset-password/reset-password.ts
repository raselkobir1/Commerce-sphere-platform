import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { I18n } from '../../core/i18n';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-reset-password',
  imports: [FormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="container auth-wrap">
      <div class="panel">
        @if (!token()) {
          <h1>{{ 'rp.invalidLink' | t }}</h1>
          <p class="muted">{{ 'rp.invalidLinkBody' | t }}</p>
          <a class="btn btn-primary btn-block" routerLink="/forgot-password">{{ 'rp.requestNewLink' | t }}</a>
        } @else if (done()) {
          <h1>{{ 'rp.passwordReset' | t }}</h1>
          <p class="muted">{{ 'rp.passwordResetBody' | t }}</p>
          <a class="btn btn-primary btn-block" routerLink="/login">{{ 'common.signIn' | t }}</a>
        } @else {
          <h1>{{ 'rp.resetYourPassword' | t }}</h1>
          <p class="muted">{{ 'rp.choosePassword' | t }}</p>
          <form (ngSubmit)="submit()">
            <div class="field">
              <label>{{ 'common.newPassword' | t }}</label>
              <div class="pw-field">
                <input class="input" [type]="showPw() ? 'text' : 'password'" name="np" [(ngModel)]="newPassword" required minlength="8" />
                <button type="button" class="pw-toggle" (click)="showPw.set(!showPw())" [attr.aria-label]="(showPw() ? 'common.hidePassword' : 'common.showPassword') | t">
                  @if (showPw()) {
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a21.8 21.8 0 0 1 5.06-6.06M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a21.8 21.8 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>
                  } @else {
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z"></path><circle cx="12" cy="12" r="3"></circle></svg>
                  }
                </button>
              </div>
            </div>
            <div class="field">
              <label>{{ 'common.confirmNewPassword' | t }}</label>
              <div class="pw-field">
                <input class="input" [type]="showConfirmPw() ? 'text' : 'password'" name="cf" [(ngModel)]="confirmPassword" required minlength="8" />
                <button type="button" class="pw-toggle" (click)="showConfirmPw.set(!showConfirmPw())" [attr.aria-label]="(showConfirmPw() ? 'common.hidePassword' : 'common.showPassword') | t">
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
              {{ (loading() ? 'common.updating' : 'rp.resetPassword') | t }}
            </button>
          </form>
        }
      </div>
    </div>
  `,
})
export class ResetPasswordPage implements OnInit {
  private api = inject(Api);
  private route = inject(ActivatedRoute);
  i18n = inject(I18n);

  token = signal<string | null>(null);
  newPassword = '';
  confirmPassword = '';
  loading = signal(false);
  error = signal('');
  done = signal(false);
  showPw = signal(false);
  showConfirmPw = signal(false);

  ngOnInit(): void {
    this.token.set(this.route.snapshot.queryParamMap.get('token'));
  }

  submit(): void {
    this.error.set('');
    if (this.newPassword.length < 8) { this.error.set(this.i18n.t('common.passwordMinLength')); return; }
    if (this.newPassword !== this.confirmPassword) { this.error.set(this.i18n.t('common.passwordsDontMatch')); return; }
    this.loading.set(true);
    this.api.post('/api/auth/password/reset', { token: this.token(), newPassword: this.newPassword }).subscribe({
      next: () => { this.loading.set(false); this.done.set(true); },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? this.i18n.t('rp.couldNotReset'));
      },
    });
  }
}
