import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { Auth } from '../../core/auth';
import { Session, TwoFactorSetup, User } from '../../core/models';

@Component({
  selector: 'app-settings',
  imports: [FormsModule, DatePipe],
  template: `
    <div class="page-head"><div><h1>Settings</h1><div class="sub">Manage your account and security</div></div></div>

    <div class="settings">
      <!-- Profile -->
      <div class="card">
        <div class="card-head"><h2>Profile</h2></div>
        <div class="card-pad">
          <div class="row">
            <div class="field"><label>First name</label><input class="input" name="fn" [(ngModel)]="profile.firstName" /></div>
            <div class="field"><label>Last name</label><input class="input" name="ln" [(ngModel)]="profile.lastName" /></div>
          </div>
          <div class="field">
            <label>Email</label>
            <input class="input" [value]="user()?.email" disabled />
            <div class="hint">
              @if (user()?.emailVerified) { <span class="badge on">Verified</span> }
              @else {
                <span class="badge low">Unverified</span>
                <button class="btn btn-sm" type="button" (click)="verifyEmail()">Send verification email</button>
              }
            </div>
          </div>
          @if (profileMsg()) { <p class="note-ok">{{ profileMsg() }}</p> }
          <button class="btn btn-primary" (click)="saveProfile()" [disabled]="savingProfile()">
            {{ savingProfile() ? 'Saving…' : 'Save profile' }}
          </button>
        </div>
      </div>

      <!-- Password -->
      <div class="card">
        <div class="card-head"><h2>Change password</h2></div>
        <div class="card-pad">
          <div class="field"><label>Current password</label><input class="input" type="password" name="cp" [(ngModel)]="pw.current" /></div>
          <div class="row">
            <div class="field"><label>New password</label><input class="input" type="password" name="np" [(ngModel)]="pw.next" /></div>
            <div class="field"><label>Confirm new password</label><input class="input" type="password" name="cf" [(ngModel)]="pw.confirm" /></div>
          </div>
          @if (pwErr()) { <p class="error">{{ pwErr() }}</p> }
          @if (pwMsg()) { <p class="note-ok">{{ pwMsg() }}</p> }
          <button class="btn btn-primary" (click)="changePassword()" [disabled]="savingPw()">
            {{ savingPw() ? 'Updating…' : 'Update password' }}
          </button>
        </div>
      </div>

      <!-- Two-factor -->
      <div class="card">
        <div class="card-head"><h2>Two-factor authentication</h2>
          @if (user()?.isActiveTwoFactor) { <span class="badge on">Enabled</span> } @else { <span class="badge off">Disabled</span> }
        </div>
        <div class="card-pad">
          @if (user()?.isActiveTwoFactor) {
            <p class="muted">Your account is protected with an authenticator app. Enter a current code to turn it off.</p>
            <div class="row" style="align-items:flex-end">
              <div class="field" style="max-width:180px"><label>Authenticator code</label><input class="input" name="d2" [(ngModel)]="code2fa" placeholder="000000" /></div>
              <button class="btn btn-danger" style="margin-bottom:16px" (click)="disable2fa()">Disable 2FA</button>
            </div>
          } @else if (setup()) {
            <p class="muted">Scan the QR in your authenticator app, or enter the key manually, then confirm a code.</p>
            <div class="secret">{{ setup()!.manualEntrySegments.join(' ') }}</div>
            <p class="cell-sub" style="word-break:break-all">{{ setup()!.qrCodeUri }}</p>
            <div class="row" style="align-items:flex-end">
              <div class="field" style="max-width:180px"><label>Enter code</label><input class="input" name="c2" [(ngModel)]="code2fa" placeholder="000000" /></div>
              <button class="btn btn-primary" style="margin-bottom:16px" (click)="confirm2fa()">Confirm & enable</button>
            </div>
          } @else {
            <p class="muted">Add an extra layer of security using an authenticator app (Google Authenticator, Authy…).</p>
            <button class="btn btn-primary" (click)="start2fa()">Enable 2FA</button>
          }
          @if (twoFaErr()) { <p class="error">{{ twoFaErr() }}</p> }
          @if (twoFaMsg()) { <p class="note-ok">{{ twoFaMsg() }}</p> }
        </div>
      </div>

      <!-- Email OTP -->
      <div class="card">
        <div class="card-head"><h2>Email one-time code at login</h2></div>
        <div class="card-pad">
          <label class="switch-row">
            <span><strong>Require an emailed code</strong><br /><span class="muted">A 6-digit code is emailed each time you sign in.</span></span>
            <span class="switch"><input type="checkbox" [checked]="user()?.isOtpAuthEnable" (change)="toggleOtp($event)" /><i></i></span>
          </label>
        </div>
      </div>

      <!-- Sessions -->
      <div class="card">
        <div class="card-head"><h2>Active sessions</h2>
          <button class="btn btn-sm btn-danger" (click)="revokeAll()">Sign out all sessions</button>
        </div>
        @if (sessions().length) {
          @for (s of sessions(); track s.id) {
            <div class="list-row">
              <span class="thumb">IP</span>
              <div class="gr"><div class="t">{{ s.createdByIp || 'unknown' }}</div>
                <div class="s">Started {{ s.createdAt | date: 'medium' }} · expires {{ s.expiresAt | date: 'mediumDate' }}</div></div>
              <span class="badge" [class.on]="s.isActive" [class.off]="!s.isActive">{{ s.isActive ? 'Active' : 'Expired' }}</span>
            </div>
          }
        } @else { <div class="empty">No active sessions.</div> }
      </div>
    </div>
  `,
})
export class SettingsPage implements OnInit {
  private api = inject(Api);
  private auth = inject(Auth);

  user = this.auth.user;

  profile = { firstName: '', lastName: '' };
  savingProfile = signal(false);
  profileMsg = signal('');

  pw = { current: '', next: '', confirm: '' };
  savingPw = signal(false);
  pwErr = signal('');
  pwMsg = signal('');

  setup = signal<TwoFactorSetup | null>(null);
  code2fa = '';
  twoFaErr = signal('');
  twoFaMsg = signal('');

  sessions = signal<Session[]>([]);

  ngOnInit(): void {
    const u = this.user();
    this.profile = { firstName: u?.firstName ?? '', lastName: u?.lastName ?? '' };
    this.loadSessions();
  }

  // ── Profile ──
  saveProfile(): void {
    this.profileMsg.set('');
    this.savingProfile.set(true);
    this.api.patch<User>('/api/auth/me', this.profile).subscribe({
      next: (u) => { this.auth.user.set(u); this.savingProfile.set(false); this.profileMsg.set('Profile updated.'); },
      error: () => this.savingProfile.set(false),
    });
  }

  verifyEmail(): void {
    this.api.post('/api/auth/email/verify/send').subscribe(() => this.profileMsg.set('Verification email sent.'));
  }

  // ── Password ──
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

  // ── Two-factor ──
  start2fa(): void {
    this.twoFaErr.set('');
    this.api.post<TwoFactorSetup>('/api/auth/2fa/setup').subscribe((s) => this.setup.set(s));
  }

  confirm2fa(): void {
    this.twoFaErr.set('');
    this.api.post('/api/auth/2fa/confirm', { code: this.code2fa }).subscribe({
      next: () => this.afterTwoFaChange('Two-factor authentication enabled.'),
      error: (e) => this.twoFaErr.set(e?.error?.message ?? 'Invalid code.'),
    });
  }

  disable2fa(): void {
    this.twoFaErr.set('');
    this.api.post('/api/auth/2fa/disable', { code: this.code2fa }).subscribe({
      next: () => this.afterTwoFaChange('Two-factor authentication disabled.'),
      error: (e) => this.twoFaErr.set(e?.error?.message ?? 'Invalid code.'),
    });
  }

  private afterTwoFaChange(msg: string): void {
    this.setup.set(null);
    this.code2fa = '';
    this.twoFaMsg.set(msg);
    this.auth.restore().subscribe(); // refresh user flags
  }

  // ── Email OTP ──
  toggleOtp(e: Event): void {
    const enable = (e.target as HTMLInputElement).checked;
    this.api.post('/api/auth/otp/toggle', { enable }).subscribe(() => this.auth.restore().subscribe());
  }

  // ── Sessions ──
  loadSessions(): void {
    this.api.get<Session[]>('/api/auth/sessions').subscribe((s) => this.sessions.set(s));
  }

  revokeAll(): void {
    this.api.delete('/api/auth/sessions').subscribe(() => this.loadSessions());
  }
}
