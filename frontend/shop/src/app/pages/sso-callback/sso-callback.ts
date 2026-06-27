import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';

// Landing page the backend redirects to after social login:
//   success → ?access_token=…&refresh_token=…&expires_at=…
//   failure → ?sso_error=…
@Component({
  selector: 'app-sso-callback',
  imports: [RouterLink],
  template: `
    <div class="container auth-wrap">
      <div class="panel" style="text-align:center">
        @if (error()) {
          <h1>Sign-in failed</h1>
          <p class="error">{{ error() }}</p>
          <a class="btn btn-primary" routerLink="/login">Back to sign in</a>
        } @else {
          <h1>Signing you in…</h1>
          <p class="muted">Completing social login, please wait.</p>
        }
      </div>
    </div>
  `,
})
export class SsoCallbackPage implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auth = inject(Auth);
  private cart = inject(Cart);

  error = signal('');

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const ssoError = qp.get('sso_error');
    const token = qp.get('access_token');

    if (ssoError) {
      this.error.set(ssoError);
      return;
    }
    if (!token) {
      this.error.set('Sign-in was cancelled or no token was returned.');
      return;
    }

    this.auth.completeSsoLogin(token).subscribe({
      next: (user) => {
        if (!user) {
          this.error.set('Could not complete sign-in. Please try again.');
          return;
        }
        this.cart.load();
        this.router.navigate(['/'], { replaceUrl: true }); // drop tokens from the URL/history
      },
      error: () => this.error.set('Could not complete sign-in. Please try again.'),
    });
  }
}
