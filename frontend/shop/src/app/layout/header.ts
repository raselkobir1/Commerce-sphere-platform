import { Component, computed, inject, signal } from '@angular/core';
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
          @if (!auth.isLoggedIn()) {
            <a routerLink="/login">Sign in</a>
          }

          <a class="cart-btn" routerLink="/cart" (click)="menuOpen.set(false)">
            🛒 Cart
            @if (cart.count() > 0) {
              <span class="cart-badge">{{ cart.count() }}</span>
            }
          </a>

          @if (auth.isLoggedIn()) {
            <div class="user-menu">
              <button class="avatar-btn" (click)="menuOpen.set(!menuOpen())" [title]="auth.user()?.firstName || 'Account'">
                {{ initials() }}
              </button>

              @if (menuOpen()) {
                <div class="backdrop" (click)="menuOpen.set(false)"></div>
                <div class="user-dropdown">
                  <div class="ud-head">
                    <span class="avatar-lg">{{ initials() }}</span>
                    <div class="ud-id">
                      <div class="nm">{{ auth.user()?.firstName }} {{ auth.user()?.lastName }}</div>
                      <div class="em">{{ auth.user()?.email }}</div>
                    </div>
                  </div>
                  <a class="ud-item" routerLink="/account" (click)="menuOpen.set(false)">👤 My account</a>
                  <a class="ud-item" routerLink="/orders" (click)="menuOpen.set(false)">🧾 My orders</a>
                  <button class="ud-item danger" (click)="logout()">↩ Sign out</button>
                </div>
              }
            </div>
          }
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

  menuOpen = signal(false);

  initials = computed(() => {
    const u = this.auth.user();
    return ((u?.firstName?.[0] ?? '') + (u?.lastName?.[0] ?? '')).toUpperCase() || 'U';
  });

  submit(): void {
    this.router.navigate(['/']);
  }

  logout(): void {
    this.menuOpen.set(false);
    this.auth.logout();
    this.cart.clear();
    this.router.navigate(['/']);
  }
}
