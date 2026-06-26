import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { AuthApiService } from '../../../core/auth/auth-api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/notifications/notification.service';

@Component({
  selector: 'app-profile',
  imports: [DatePipe, ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly authApi = inject(AuthApiService);
  private readonly notify = inject(NotificationService);

  readonly user = this.auth.user;
  readonly saving = signal(false);
  readonly sendingVerification = signal(false);

  readonly form = this.fb.nonNullable.group({
    firstName: [this.user()?.firstName ?? '', [Validators.required, Validators.maxLength(100)]],
    lastName: [this.user()?.lastName ?? '', [Validators.required, Validators.maxLength(100)]],
  });

  save(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    this.authApi.updateProfile(this.form.getRawValue()).subscribe({
      next: (updated) => {
        this.auth.setUser(updated);
        this.saving.set(false);
        this.notify.success('Profile updated');
      },
      error: () => this.saving.set(false),
    });
  }

  sendVerification(): void {
    this.sendingVerification.set(true);
    this.authApi.sendVerificationEmail().subscribe({
      next: () => {
        this.sendingVerification.set(false);
        this.notify.success('Verification email sent. Check your inbox.');
      },
      error: () => this.sendingVerification.set(false),
    });
  }
}
