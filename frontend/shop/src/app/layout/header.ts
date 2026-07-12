import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map } from 'rxjs';
import { Auth } from '../core/auth';
import { Chat } from '../core/chat';
import { Cart } from '../core/cart';
import { Search } from '../core/search';
import { I18n } from '../core/i18n';
import { TranslatePipe } from '../core/translate.pipe';
import { CatalogUi } from '../core/catalog-ui';

@Component({
  selector: 'app-header',
  imports: [FormsModule, RouterLink, TranslatePipe],
  template: `
    <header class="header">
      <div class="header-inner">
        <div class="logo-group">
          @if (isHome()) {
            <button
              type="button"
              class="filters-toggle"
              (click)="catalogUi.filtersOpen.set(!catalogUi.filtersOpen())"
              [attr.aria-label]="(catalogUi.filtersOpen() ? 'catalog.hideCategories' : 'catalog.showCategories') | t"
            >☰</button>
          }
          <a class="logo" routerLink="/">Shop<span>Sphere</span></a>
        </div>

        <form class="searchbar" (ngSubmit)="submit()">
          <input
            [ngModel]="search.term()"
            (ngModelChange)="search.term.set($event)"
            name="q"
            [placeholder]="'header.searchPlaceholder' | t"
          />
          <button type="submit">{{ 'header.search' | t }}</button>
        </form>

        <div class="header-actions">
          <div class="lang-toggle" role="group" aria-label="Language">
            <button type="button" [class.active]="i18n.lang() === 'en'" (click)="i18n.set('en')">EN</button>
            <button type="button" [class.active]="i18n.lang() === 'bn'" (click)="i18n.set('bn')">বাং</button>
          </div>

          @if (!auth.isLoggedIn()) {
            <a routerLink="/login">{{ 'common.signIn' | t }}</a>
          }

          <a class="cart-btn" routerLink="/cart" (click)="menuOpen.set(false)">
            🛒 {{ 'header.cart' | t }}
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
                  <a class="ud-item" routerLink="/account" (click)="menuOpen.set(false)">👤 {{ 'header.myAccount' | t }}</a>
                  <a class="ud-item" routerLink="/orders" (click)="menuOpen.set(false)">🧾 {{ 'header.myOrders' | t }}</a>
                  <button class="ud-item danger" (click)="logout()">↩ {{ 'header.signOut' | t }}</button>
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
  i18n = inject(I18n);
  catalogUi = inject(CatalogUi);
  private chat = inject(Chat);
  private router = inject(Router);

  menuOpen = signal(false);

  private url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => this.router.url),
    ),
    { initialValue: this.router.url },
  );
  isHome = computed(() => this.url().split('?')[0] === '/');

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
    void this.chat.reset();
    this.router.navigate(['/']);
  }
}
