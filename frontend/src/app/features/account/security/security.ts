import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AuthApiService } from '../../../core/auth/auth-api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { TwoFactorSetup } from '../../../core/models/auth.models';
import { NotificationService } from '../../../core/notifications/notification.service';
import { Session } from '../../../core/models/auth.models';

const STRONG_PASSWORD = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/;

@Component({
  selector: 'app-security',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
  ],
  templateUrl: './security.html',
  styleUrl: './security.scss',
})
export class Security implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly authApi = inject(AuthApiService);
  private readonly notify = inject(NotificationService);

  readonly user = this.auth.user;
  readonly busy = signal(false);

  // ── Change password ──
  readonly pwForm = this.fb.nonNullable.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.pattern(STRONG_PASSWORD)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: matchPasswords },
  );

  // ── Two-factor ──
  readonly twoFactorSetup = signal<TwoFactorSetup | null>(null);
  readonly confirmForm = this.fb.nonNullable.group({ code: ['', [Validators.required, Validators.minLength(6)]] });
  readonly disableForm = this.fb.nonNullable.group({ code: ['', [Validators.required, Validators.minLength(6)]] });

  // ── Sessions ──
  readonly sessions = signal<Session[]>([]);

  ngOnInit(): void {
    this.loadSessions();
  }

  changePassword(): void {
    if (this.pwForm.invalid || this.busy()) return;
    this.busy.set(true);
    const { currentPassword, newPassword } = this.pwForm.getRawValue();
    this.authApi.changePassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.busy.set(false);
        this.pwForm.reset();
        this.notify.success('Password changed. Other sessions were signed out.');
        this.loadSessions();
      },
      error: () => this.busy.set(false),
    });
  }

  startTwoFactorSetup(): void {
    this.busy.set(true);
    this.authApi.setupTwoFactor().subscribe({
      next: (setup) => {
        this.busy.set(false);
        this.twoFactorSetup.set(setup);
      },
      error: () => this.busy.set(false),
    });
  }

  confirmTwoFactor(): void {
    if (this.confirmForm.invalid || this.busy()) return;
    this.busy.set(true);
    this.authApi.confirmTwoFactor(this.confirmForm.getRawValue().code).subscribe({
      next: (tokens) => {
        this.auth.setSession(tokens);
        this.busy.set(false);
        this.twoFactorSetup.set(null);
        this.confirmForm.reset();
        this.notify.success('Two-factor authentication enabled.');
      },
      error: () => this.busy.set(false),
    });
  }

  cancelTwoFactorSetup(): void {
    this.twoFactorSetup.set(null);
    this.confirmForm.reset();
  }

  disableTwoFactor(): void {
    if (this.disableForm.invalid || this.busy()) return;
    this.busy.set(true);
    this.authApi.disableTwoFactor(this.disableForm.getRawValue().code).subscribe({
      next: () => {
        this.disableForm.reset();
        this.refreshUser('Two-factor authentication disabled.');
      },
      error: () => this.busy.set(false),
    });
  }

  toggleOtp(enable: boolean): void {
    this.busy.set(true);
    this.authApi.toggleOtp(enable).subscribe({
      next: () => this.refreshUser(enable ? 'Email OTP enabled.' : 'Email OTP disabled.'),
      error: () => this.busy.set(false),
    });
  }

  revokeAllSessions(): void {
    this.busy.set(true);
    this.authApi.revokeAllSessions().subscribe({
      next: () => {
        this.busy.set(false);
        this.notify.success('All sessions revoked.');
        this.loadSessions();
      },
      error: () => this.busy.set(false),
    });
  }

  private loadSessions(): void {
    this.authApi.sessions().subscribe({
      next: (list) => this.sessions.set(list),
      error: () => this.sessions.set([]),
    });
  }

  private refreshUser(message: string): void {
    this.auth.loadCurrentUser().subscribe({
      next: () => {
        this.busy.set(false);
        this.notify.success(message);
      },
      error: () => this.busy.set(false),
    });
  }
}

function matchPasswords(group: AbstractControl): ValidationErrors | null {
  return group.get('newPassword')?.value === group.get('confirmPassword')?.value ? null : { passwordMismatch: true };
}
