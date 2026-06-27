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
        <div class="field"><label>Current password</label><input class="input" type="password" name="cp" [(ngModel)]="pw.current" /></div>
        <div class="field"><label>New password</label><input class="input" type="password" name="np" [(ngModel)]="pw.next" /></div>
        <div class="field"><label>Confirm new password</label><input class="input" type="password" name="cf" [(ngModel)]="pw.confirm" /></div>
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
