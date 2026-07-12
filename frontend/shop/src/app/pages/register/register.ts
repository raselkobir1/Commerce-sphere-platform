import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { SsoButtons } from '../../layout/sso-buttons';
import { I18n } from '../../core/i18n';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink, SsoButtons, TranslatePipe],
  template: `
    <div class="container auth-wrap">
      <div class="panel">
        <h1>{{ 'register.title' | t }}</h1>
        <form (ngSubmit)="submit()">
          <div class="field">
            <label>{{ 'common.firstName' | t }}</label>
            <input class="input" name="firstName" [(ngModel)]="firstName" required />
          </div>
          <div class="field">
            <label>{{ 'common.lastName' | t }}</label>
            <input class="input" name="lastName" [(ngModel)]="lastName" required />
          </div>
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
          </div>

          @if (error()) {
            <p class="error">{{ error() }}</p>
          }

          <button class="btn btn-primary btn-block" type="submit" [disabled]="loading()">
            {{ (loading() ? 'register.creating' : 'register.title') | t }}
          </button>
        </form>

        <app-sso-buttons />

        <p class="muted" style="margin-top:16px">
          {{ 'register.alreadyHaveAccount' | t }} <a routerLink="/login">{{ 'common.signIn' | t }}</a>
        </p>
      </div>
    </div>
  `,
})
export class RegisterPage {
  private auth = inject(Auth);
  private router = inject(Router);
  i18n = inject(I18n);

  firstName = '';
  lastName = '';
  email = '';
  password = '';
  loading = signal(false);
  error = signal('');
  showPw = signal(false);

  submit(): void {
    this.error.set('');
    this.loading.set(true);
    this.auth.register(this.firstName, this.lastName, this.email, this.password).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? this.i18n.t('register.couldNotCreate'));
      },
    });
  }
}
