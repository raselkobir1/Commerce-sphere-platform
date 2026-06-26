import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { AuthApiService } from '../../../core/auth/auth-api.service';
import { NotificationService } from '../../../core/notifications/notification.service';

const STRONG_PASSWORD = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/;

@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatProgressBarModule],
  template: `
    @if (loading()) {
      <mat-progress-bar mode="indeterminate" />
    }
    @if (token) {
      <h1 class="title">Set a new password</h1>
      <p class="subtitle">Choose a strong password you haven't used before.</p>
      <form [formGroup]="form" (ngSubmit)="submit()" class="form">
        <mat-form-field appearance="outline">
          <mat-label>New password</mat-label>
          <input matInput type="password" formControlName="newPassword" autocomplete="new-password" />
          @if (form.controls.newPassword.hasError('pattern') && form.controls.newPassword.touched) {
            <mat-hint class="hint-error">Min 8 chars with upper, lower, digit & special character</mat-hint>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Confirm new password</mat-label>
          <input matInput type="password" formControlName="confirmPassword" autocomplete="new-password" />
          @if (form.hasError('passwordMismatch') && form.controls.confirmPassword.touched) {
            <mat-error>Passwords do not match</mat-error>
          }
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="loading()">Reset password</button>
      </form>
    } @else {
      <h1 class="title">Invalid link</h1>
      <p class="subtitle">This reset link is missing its token. Request a new one.</p>
    }
    <p class="foot"><a routerLink="/auth/login">Back to sign in</a></p>
  `,
  styleUrl: './reset-password.scss',
})
export class ResetPassword implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthApiService);
  private readonly router = inject(Router);
  private readonly notify = inject(NotificationService);

  // Bound from the ?token= query param via withComponentInputBinding().
  @Input() token = '';

  readonly loading = signal(false);

  readonly form = this.fb.nonNullable.group(
    {
      newPassword: ['', [Validators.required, Validators.pattern(STRONG_PASSWORD)]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: matchPasswords },
  );

  ngOnInit(): void {
    /* token arrives via input binding */
  }

  submit(): void {
    if (this.form.invalid || this.loading() || !this.token) return;
    this.loading.set(true);
    this.authApi.resetPassword({ token: this.token, newPassword: this.form.getRawValue().newPassword }).subscribe({
      next: () => {
        this.loading.set(false);
        this.notify.success('Password reset. Please sign in with your new password.');
        void this.router.navigateByUrl('/auth/login');
      },
      error: () => this.loading.set(false),
    });
  }
}

function matchPasswords(group: AbstractControl): ValidationErrors | null {
  return group.get('newPassword')?.value === group.get('confirmPassword')?.value ? null : { passwordMismatch: true };
}
