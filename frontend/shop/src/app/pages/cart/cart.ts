import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { Products } from '../../core/products';
import { CartItem } from '../../core/models';
import { BdtPipe } from '../../core/bdt.pipe';

@Component({
  selector: 'app-cart',
  imports: [BdtPipe, RouterLink],
  template: `
    <div class="container">
      <h1>Your cart</h1>

      @if (!auth.isLoggedIn()) {
        <div class="empty">Please <a class="btn-ghost" routerLink="/login">sign in</a> to view your cart.</div>
      } @else if (!cart.cart() || cart.cart()!.items.length === 0) {
        <div class="empty">Your cart is empty.<br /><a class="btn btn-primary" routerLink="/" style="margin-top:14px">Start shopping</a></div>
      } @else {
        <div class="cart-grid">
          <div class="panel" style="margin:0">
            @for (item of cart.cart()!.items; track item.id) {
              <div class="cart-row">
                <div class="pic" [style.background-image]="image(item.productId)"></div>
                <div class="info">
                  <div class="nm">{{ item.productName }}</div>
                  <div class="muted">{{ item.unitPrice | bdt }} each</div>
                </div>
                <div class="qty">
                  <button type="button" (click)="dec(item)">−</button>
                  <input type="number" min="1" [value]="item.quantity" readonly />
                  <button type="button" (click)="inc(item)">+</button>
                </div>
                <div class="ln">{{ item.lineTotal | bdt }}</div>
                <button class="btn btn-sm" (click)="remove(item)">Remove</button>
              </div>
            }
          </div>

          <div class="summary">
            <h2>Order summary</h2>
            <div class="line"><span>Subtotal ({{ cart.count() }} items)</span><span>{{ cart.cart()!.totalAmount | bdt }}</span></div>
            <div class="line"><span>Shipping</span><span style="color:var(--green)">Free</span></div>
            <div class="line total"><span>Total</span><span>{{ cart.cart()!.totalAmount | bdt }}</span></div>
            <a class="btn btn-primary btn-block" routerLink="/checkout" style="margin-top:14px">Proceed to checkout</a>
            <a class="btn btn-block btn-ghost" routerLink="/" style="margin-top:10px">Continue shopping</a>
          </div>
        </div>
      }
    </div>
  `,
})
export class CartPage implements OnInit {
  auth = inject(Auth);
  cart = inject(Cart);
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
