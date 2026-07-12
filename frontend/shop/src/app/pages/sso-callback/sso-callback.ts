import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { I18n } from '../../core/i18n';
import { TranslatePipe } from '../../core/translate.pipe';

// Landing page the backend redirects to after social login:
//   success → ?access_token=…&refresh_token=…&expires_at=…
//   failure → ?sso_error=…
@Component({
  selector: 'app-sso-callback',
  imports: [RouterLink, TranslatePipe],
  template: `
    <div class="container auth-wrap">
      <div class="panel" style="text-align:center">
        @if (error()) {
          <h1>{{ 'sso.signInFailedTitle' | t }}</h1>
          <p class="error">{{ error() }}</p>
          <a class="btn btn-primary" routerLink="/login">{{ 'fp.backToSignIn' | t }}</a>
        } @else {
          <h1>{{ 'sso.signingYouIn' | t }}</h1>
          <p class="muted">{{ 'sso.completing' | t }}</p>
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
  private i18n = inject(I18n);

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
      this.error.set(this.i18n.t('sso.cancelledOrNoToken'));
      return;
    }

    this.auth.completeSsoLogin(token).subscribe({
      next: (user) => {
        if (!user) {
          this.error.set(this.i18n.t('sso.couldNotComplete'));
          return;
        }
        this.cart.load();
        this.router.navigate(['/'], { replaceUrl: true }); // drop tokens from the URL/history
      },
      error: () => this.error.set(this.i18n.t('sso.couldNotComplete')),
    });
  }
}
