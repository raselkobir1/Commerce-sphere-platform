import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../core/auth';
import { Cart } from '../core/cart';

@Component({
  selector: 'app-header',
  imports: [RouterLink],
  template: `
    <header class="header">
      <div class="header-inner">
        <a class="logo" routerLink="/">Shop<span>Sphere</span></a>
        <nav>
          <a routerLink="/">Shop</a>
          <a class="cart-link" routerLink="/cart">
            Cart
            @if (cart.count() > 0) {
              <span class="cart-count">{{ cart.count() }}</span>
            }
          </a>
          @if (auth.isLoggedIn()) {
            <span class="muted">Hi, {{ auth.user()?.firstName }}</span>
            <a href="#" (click)="logout($event)">Sign out</a>
          } @else {
            <a routerLink="/login">Sign in</a>
          }
        </nav>
      </div>
    </header>
  `,
})
export class Header {
  auth = inject(Auth);
  cart = inject(Cart);
  private router = inject(Router);

  logout(e: Event): void {
    e.preventDefault();
    this.auth.logout();
    this.cart.clear();
    this.router.navigate(['/']);
  }
}
