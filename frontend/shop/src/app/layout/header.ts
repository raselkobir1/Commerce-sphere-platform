import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../core/auth';
import { Cart } from '../core/cart';
import { Search } from '../core/search';

@Component({
  selector: 'app-header',
  imports: [FormsModule, RouterLink],
  template: `
    <header class="header">
      <div class="header-inner">
        <a class="logo" routerLink="/">Shop<span>Sphere</span></a>

        <form class="searchbar" (ngSubmit)="submit()">
          <input
            [ngModel]="search.term()"
            (ngModelChange)="search.term.set($event)"
            name="q"
            placeholder="Search products, brands and more…"
          />
          <button type="submit">Search</button>
        </form>

        <div class="header-actions">
          @if (auth.isLoggedIn()) {
            <span class="muted">Hi, {{ auth.user()?.firstName }}</span>
            <a href="#" (click)="logout($event)">Sign out</a>
          } @else {
            <a routerLink="/login">Sign in</a>
          }
          <a class="cart-btn" routerLink="/cart">
            🛒 Cart
            @if (cart.count() > 0) {
              <span class="cart-badge">{{ cart.count() }}</span>
            }
          </a>
        </div>
      </div>
    </header>
  `,
})
export class Header {
  auth = inject(Auth);
  cart = inject(Cart);
  search = inject(Search);
  private router = inject(Router);

  submit(): void {
    this.router.navigate(['/']);
  }

  logout(e: Event): void {
    e.preventDefault();
    this.auth.logout();
    this.cart.clear();
    this.router.navigate(['/']);
  }
}
