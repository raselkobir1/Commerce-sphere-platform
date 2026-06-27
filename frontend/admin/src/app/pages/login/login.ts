import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Auth } from '../../core/auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  template: `
    <div class="login-wrap">
      <form class="login-card" (ngSubmit)="submit()">
        <h1>Admin<span style="color:#3056d3">Sphere</span></h1>
        <p class="sub">Sign in to manage your store</p>

        <div class="field">
          <label>Email</label>
          <input class="input" type="email" name="email" [(ngModel)]="email" required />
        </div>
        <div class="field">
          <label>Password</label>
          <input class="input" type="password" name="password" [(ngModel)]="password" required />
        </div>

        @if (error()) {
          <p class="error">{{ error() }}</p>
        }

        <button class="btn btn-primary" style="width:100%" type="submit" [disabled]="loading()">
          {{ loading() ? 'Signing in…' : 'Sign in' }}
        </button>
      </form>
    </div>
  `,
})
export class LoginPage {
  private auth = inject(Auth);
  private router = inject(Router);

  email = '';
  password = '';
  loading = signal(false);
  error = signal('');

  submit(): void {
    this.error.set('');
    this.loading.set(true);
    this.auth.login(this.email, this.password).subscribe({
      next: (user) => {
        this.loading.set(false);
        if (user.role !== 'Admin') {
          this.auth.logout();
          this.error.set('This account is not an administrator.');
          return;
        }
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? err?.message ?? 'Sign in failed.');
      },
    });
  }
}
