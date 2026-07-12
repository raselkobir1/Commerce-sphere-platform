import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { Products } from '../../core/products';
import { CartItem } from '../../core/models';
import { BdtPipe } from '../../core/bdt.pipe';
import { I18n } from '../../core/i18n';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-cart',
  imports: [BdtPipe, RouterLink, TranslatePipe],
  template: `
    <div class="container">
      <h1>{{ 'cart.title' | t }}</h1>

      @if (!auth.isLoggedIn()) {
        <div class="empty">{{ 'cart.pleaseSignInPrefix' | t }} <a class="btn-ghost" routerLink="/login">{{ 'common.signIn' | t }}</a> {{ 'cart.pleaseSignInToView' | t }}</div>
      } @else if (!cart.cart() || cart.cart()!.items.length === 0) {
        <div class="empty">{{ 'cart.empty' | t }}<br /><a class="btn btn-primary" routerLink="/" style="margin-top:14px">{{ 'common.startShopping' | t }}</a></div>
      } @else {
        <div class="cart-grid">
          <div class="panel" style="margin:0">
            @for (item of cart.cart()!.items; track item.id) {
              <div class="cart-row">
                <div class="pic" [style.background-image]="image(item.productId)"></div>
                <div class="info">
                  <div class="nm">{{ item.productName }}</div>
                  <div class="muted">{{ item.unitPrice | bdt }} {{ 'cart.each' | t }}</div>
                </div>
                <div class="qty">
                  <button type="button" (click)="dec(item)">−</button>
                  <input type="number" min="1" [value]="item.quantity" readonly />
                  <button type="button" (click)="inc(item)">+</button>
                </div>
                <div class="ln">{{ item.lineTotal | bdt }}</div>
                <button class="btn btn-sm" (click)="remove(item)">{{ 'cart.remove' | t }}</button>
              </div>
            }
          </div>

          <div class="summary">
            <h2>{{ 'cart.orderSummary' | t }}</h2>
            <div class="line"><span>{{ i18n.t('cart.subtotal', { n: cart.count() }) }}</span><span>{{ cart.cart()!.totalAmount | bdt }}</span></div>
            <div class="line"><span>{{ 'common.shipping' | t }}</span><span style="color:var(--green)">{{ 'common.free' | t }}</span></div>
            <div class="line total"><span>{{ 'common.total' | t }}</span><span>{{ cart.cart()!.totalAmount | bdt }}</span></div>
            <a class="btn btn-primary btn-block" routerLink="/checkout" style="margin-top:14px">{{ 'cart.proceedToCheckout' | t }}</a>
            <a class="btn btn-block btn-ghost" routerLink="/" style="margin-top:10px">{{ 'cart.continueShopping' | t }}</a>
          </div>
        </div>
      }
    </div>
  `,
})
export class CartPage implements OnInit {
  auth = inject(Auth);
  cart = inject(Cart);
  i18n = inject(I18n);
  private products = inject(Products);

  ngOnInit(): void {
    this.products.load();
  }

  image(productId: string): string | null {
    const url = this.products.byId(productId)?.imageUrl;
    return url ? `url(${url})` : null;
  }

  inc(item: CartItem): void { this.cart.updateQuantity(item.productId, item.quantity + 1).subscribe(); }
  dec(item: CartItem): void { if (item.quantity > 1) this.cart.updateQuantity(item.productId, item.quantity - 1).subscribe(); }
  remove(item: CartItem): void { this.cart.remove(item.productId).subscribe(); }
}
