import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Auth } from '../../core/auth';
import { Perms } from '../../core/perms';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  template: `
    <div class="login-wrap">
      <aside class="login-aside">
        <div class="big">Run your store with<br />Admin<span style="color:#c7d2fe">Sphere</span></div>
        <p>Manage products, categories, inventory and customers from one modern dashboard.</p>
        <ul>
          <li>Product & catalog management</li>
          <li>Live inventory control</li>
          <li>Customer insights</li>
        </ul>
      </aside>

      <div class="login-main">
        <form class="login-card" (ngSubmit)="submit()">
          <div class="lead">Admin<span style="color:var(--brand)">Sphere</span></div>
          <p class="sub">Sign in to your dashboard</p>

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

          <button class="btn btn-primary" style="width:100%; justify-content:center" type="submit" [disabled]="loading()">
            {{ loading() ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>
      </div>
    </div>
  `,
})
export class LoginPage {
  private auth = inject(Auth);
  private perms = inject(Perms);
  private router = inject(Router);

  email = '';
  password = '';
  loading = signal(false);
  error = signal('');

  submit(): void {
    this.error.set('');
    this.loading.set(true);
    this.auth.login(this.email, this.password).subscribe({
      next: () => {
        // Load the user's permitted menus; if they have none, they have no admin access.
        this.perms.load().subscribe({
          next: (menus) => {
            this.loading.set(false);
            if (menus.length === 0) {
              this.auth.logout();
              this.perms.clear();
              this.error.set('This account has no admin access.');
              return;
            }
            this.router.navigate([menus[0].route]);
          },
          error: () => {
            this.loading.set(false);
            this.error.set('Could not load your permissions.');
          },
        });
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? err?.message ?? 'Sign in failed.');
      },
    });
  }
}
