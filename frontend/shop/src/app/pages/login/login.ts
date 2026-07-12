import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { SsoButtons } from '../../layout/sso-buttons';
import { I18n } from '../../core/i18n';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, SsoButtons, TranslatePipe],
  template: `
    <div class="container auth-wrap">
      <div class="panel">
        @if (!challengeToken()) {
          <h1>{{ 'login.title' | t }}</h1>
          <form (ngSubmit)="submit()">
            <div class="field">
              <label>{{ 'common.email' | t }}</label>
              <input class="input" type="email" name="email" [(ngModel)]="email" required />
            </div>
            <div class="field">
              <label>{{ 'common.password' | t }}</label>
              <div class="pw-field">
                <input class="input" [type]="showPw() ? 'text' : 'password'" name="password" [(ngModel)]="password" required />
                <button type="button" class="pw-toggle" (click)="showPw.set(!showPw())" [attr.aria-label]="(showPw() ? 'common.hidePassword' : 'common.showPassword') | t">
                  @if (showPw()) {
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a21.8 21.8 0 0 1 5.06-6.06M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a21.8 21.8 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>
                  } @else {
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z"></path><circle cx="12" cy="12" r="3"></circle></svg>
                  }
                </button>
              </div>
              <div style="text-align:right; margin-top:8px">
                <a routerLink="/forgot-password" style="font-size:13px">{{ 'login.forgotPassword' | t }}</a>
              </div>
            </div>

            @if (error()) {
              <p class="error">{{ error() }}</p>
            }

            <button class="btn btn-primary btn-block" type="submit" [disabled]="loading()">
              {{ (loading() ? 'login.signingIn' : 'login.title') | t }}
            </button>
          </form>

          <app-sso-buttons />

          <p class="muted" style="margin-top:16px">
            {{ 'login.newHere' | t }} <a routerLink="/register">{{ 'login.createAccountLink' | t }}</a>
          </p>
        } @else {
          <h1>{{ 'login.setNewPasswordTitle' | t }}</h1>
          <p class="muted">{{ 'login.tempPasswordNotice' | t }}</p>
          <form (ngSubmit)="submitNewPassword()">
            <div class="field">
              <label>{{ 'common.newPassword' | t }}</label>
              <div class="pw-field">
                <input class="input" [type]="showNewPw() ? 'text' : 'password'" name="np" [(ngModel)]="newPassword" required minlength="8" />
                <button type="button" class="pw-toggle" (click)="showNewPw.set(!showNewPw())" [attr.aria-label]="(showNewPw() ? 'common.hidePassword' : 'common.showPassword') | t">
                  @if (showNewPw()) {
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
              {{ (loading() ? 'common.updating' : 'login.setPasswordAndSignIn') | t }}
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
  i18n = inject(I18n);

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
        this.error.set(err?.error?.message ?? err?.message ?? this.i18n.t('login.signInFailed'));
      },
    });
  }

  submitNewPassword(): void {
    this.error.set('');
    if (this.newPassword.length < 8) { this.error.set(this.i18n.t('common.passwordMinLength')); return; }
    if (this.newPassword !== this.confirmPassword) { this.error.set(this.i18n.t('common.passwordsDontMatch')); return; }
    this.loading.set(true);
    this.auth.completeForcedPasswordChange(this.challengeToken()!, this.newPassword).subscribe({
      next: () => {
        this.loading.set(false);
        this.cart.load();
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? this.i18n.t('login.couldNotUpdatePassword'));
      },
    });
  }
}
