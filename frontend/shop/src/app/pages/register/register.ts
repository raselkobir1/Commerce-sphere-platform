import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="container auth-wrap">
      <div class="card">
        <h1>Create account</h1>
        <form (ngSubmit)="submit()">
          <div class="field">
            <label>First name</label>
            <input class="input" name="firstName" [(ngModel)]="firstName" required />
          </div>
          <div class="field">
            <label>Last name</label>
            <input class="input" name="lastName" [(ngModel)]="lastName" required />
          </div>
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

          <button class="btn btn-primary btn-block" type="submit" [disabled]="loading()">
            {{ loading() ? 'Creating…' : 'Create account' }}
          </button>
        </form>
        <p class="muted" style="margin-top:16px">
          Already have an account? <a routerLink="/login">Sign in</a>
        </p>
      </div>
    </div>
  `,
})
export class RegisterPage {
  private auth = inject(Auth);
  private router = inject(Router);

  firstName = '';
  lastName = '';
  email = '';
  password = '';
  loading = signal(false);
  error = signal('');

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
        this.error.set(err?.error?.message ?? 'Could not create the account.');
      },
    });
  }
}
