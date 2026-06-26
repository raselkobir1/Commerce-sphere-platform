import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { AuthApiService } from '../../../core/auth/auth-api.service';

type State = 'verifying' | 'success' | 'error';

@Component({
  selector: 'app-verify-email',
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressBarModule],
  template: `
    @switch (state()) {
      @case ('verifying') {
        <mat-progress-bar mode="indeterminate" />
        <h1 class="title">Verifying your email…</h1>
        <p class="subtitle">One moment while we confirm your address.</p>
      }
      @case ('success') {
        <mat-icon class="state-icon ok">check_circle</mat-icon>
        <h1 class="title">Email verified</h1>
        <p class="subtitle">Your email address is confirmed. You're all set.</p>
      }
      @case ('error') {
        <mat-icon class="state-icon bad">error</mat-icon>
        <h1 class="title">Verification failed</h1>
        <p class="subtitle">This link is invalid or has expired. Request a new one from your account.</p>
      }
    }
    <p class="foot"><a routerLink="/auth/login">Back to sign in</a></p>
  `,
  styleUrl: './verify-email.scss',
})
export class VerifyEmail implements OnInit {
  private readonly authApi = inject(AuthApiService);

  @Input() token = '';
  readonly state = signal<State>('verifying');

  ngOnInit(): void {
    if (!this.token) {
      this.state.set('error');
      return;
    }
    this.authApi.confirmEmail(this.token).subscribe({
      next: () => this.state.set('success'),
      error: () => this.state.set('error'),
    });
  }
}
