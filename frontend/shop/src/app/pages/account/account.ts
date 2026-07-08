import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { User } from '../../core/models';

@Component({
  selector: 'app-account',
  imports: [FormsModule],
  template: `
    <div class="container" style="max-width:640px">
      <h1>My account</h1>

      <!-- Profile -->
      <div class="panel" style="margin-bottom:18px">
        <h2 style="margin-bottom:14px">Profile</h2>
        <div class="field"><label>First name</label><input class="input" name="fn" [(ngModel)]="form.firstName" /></div>
        <div class="field"><label>Last name</label><input class="input" name="ln" [(ngModel)]="form.lastName" /></div>
        <div class="field">
          <label>Email</label>
          <input class="input" [value]="auth.user()?.email" disabled />
          @if (auth.user() && !auth.user()?.emailVerified) {
            <div class="muted" style="font-size:13px;margin-top:6px">Email not verified.</div>
          }
        </div>
        @if (profileMsg()) { <p class="notice">{{ profileMsg() }}</p> }
        <button class="btn btn-primary" (click)="saveProfile()" [disabled]="savingProfile()">
          {{ savingProfile() ? 'Saving…' : 'Save changes' }}
        </button>
      </div>

      <!-- Password -->
      <div class="panel" style="margin-bottom:18px">
        <h2 style="margin-bottom:14px">Change password</h2>
        <div class="field"><label>Current password</label>
          <div class="pw-field">
            <input class="input" [type]="showCurrentPw() ? 'text' : 'password'" name="cp" [(ngModel)]="pw.current" />
            <button type="button" class="pw-toggle" (click)="showCurrentPw.set(!showCurrentPw())" [attr.aria-label]="showCurrentPw() ? 'Hide password' : 'Show password'">
              @if (showCurrentPw()) {
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a21.8 21.8 0 0 1 5.06-6.06M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a21.8 21.8 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>
              } @else {
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z"></path><circle cx="12" cy="12" r="3"></circle></svg>
              }
            </button>
          </div>
        </div>
        <div class="field"><label>New password</label>
          <div class="pw-field">
            <input class="input" [type]="showNewPw() ? 'text' : 'password'" name="np" [(ngModel)]="pw.next" />
            <button type="button" class="pw-toggle" (click)="showNewPw.set(!showNewPw())" [attr.aria-label]="showNewPw() ? 'Hide password' : 'Show password'">
              @if (showNewPw()) {
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a21.8 21.8 0 0 1 5.06-6.06M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a21.8 21.8 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>
              } @else {
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z"></path><circle cx="12" cy="12" r="3"></circle></svg>
              }
            </button>
          </div>
        </div>
        <div class="field"><label>Confirm new password</label>
          <div class="pw-field">
            <input class="input" [type]="showConfirmPw() ? 'text' : 'password'" name="cf" [(ngModel)]="pw.confirm" />
            <button type="button" class="pw-toggle" (click)="showConfirmPw.set(!showConfirmPw())" [attr.aria-label]="showConfirmPw() ? 'Hide password' : 'Show password'">
              @if (showConfirmPw()) {
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a21.8 21.8 0 0 1 5.06-6.06M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a21.8 21.8 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line></svg>
              } @else {
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z"></path><circle cx="12" cy="12" r="3"></circle></svg>
              }
            </button>
          </div>
        </div>
        @if (pwErr()) { <p class="error">{{ pwErr() }}</p> }
        @if (pwMsg()) { <p class="notice">{{ pwMsg() }}</p> }
        <button class="btn btn-primary" (click)="changePassword()" [disabled]="savingPw()">
          {{ savingPw() ? 'Updating…' : 'Update password' }}
        </button>
      </div>

      <button class="btn" (click)="logout()">Sign out</button>
    </div>
  `,
})
export class AccountPage implements OnInit {
  auth = inject(Auth);
  private api = inject(Api);
  private cart = inject(Cart);
  private router = inject(Router);

  form = { firstName: '', lastName: '' };
  savingProfile = signal(false);
  profileMsg = signal('');

  pw = { current: '', next: '', confirm: '' };
  savingPw = signal(false);
  pwErr = signal('');
  pwMsg = signal('');
  showCurrentPw = signal(false);
  showNewPw = signal(false);
  showConfirmPw = signal(false);

  ngOnInit(): void {
    // Refresh the full profile from the server, then prefill the form.
    this.api.get<User>('/api/auth/me').subscribe((u) => {
      this.auth.user.set(u);
      this.form = { firstName: u.firstName ?? '', lastName: u.lastName ?? '' };
    });
  }

  saveProfile(): void {
    this.profileMsg.set('');
    this.savingProfile.set(true);
    this.api.patch<User>('/api/auth/me', this.form).subscribe({
      next: (u) => { this.auth.user.set(u); this.savingProfile.set(false); this.profileMsg.set('Profile updated.'); },
      error: () => this.savingProfile.set(false),
    });
  }

  changePassword(): void {
    this.pwErr.set(''); this.pwMsg.set('');
    if (this.pw.next.length < 8) { this.pwErr.set('New password must be at least 8 characters.'); return; }
    if (this.pw.next !== this.pw.confirm) { this.pwErr.set('New passwords do not match.'); return; }
    this.savingPw.set(true);
    this.api.post('/api/auth/change-password', { currentPassword: this.pw.current, newPassword: this.pw.next }).subscribe({
      next: () => { this.savingPw.set(false); this.pw = { current: '', next: '', confirm: '' }; this.pwMsg.set('Password updated.'); },
      error: (e) => { this.savingPw.set(false); this.pwErr.set(e?.error?.message ?? 'Could not change password.'); },
    });
  }

  logout(): void {
    this.auth.logout();
    this.cart.clear();
    this.router.navigate(['/']);
  }
}
