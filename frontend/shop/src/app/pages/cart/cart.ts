import { Component, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { CartItem } from '../../core/models';

@Component({
  selector: 'app-cart',
  imports: [CurrencyPipe, FormsModule, RouterLink],
  template: `
    <div class="container">
      <h1>Your cart</h1>

      @if (!auth.isLoggedIn()) {
        <p class="muted">Please <a routerLink="/login">sign in</a> to view your cart.</p>
      } @else if (!cart.cart() || cart.cart()!.items.length === 0) {
        <p class="muted">Your cart is empty. <a routerLink="/">Browse products</a>.</p>
      } @else {
        <div class="card">
          @for (item of cart.cart()!.items; track item.id) {
            <div class="cart-row">
              <span class="name">{{ item.productName }}</span>
              <span class="muted">{{ item.unitPrice | currency }}</span>
              <input class="qty" type="number" min="1" [ngModel]="item.quantity"
                     (change)="changeQty(item, $event)" />
              <span style="width:90px; text-align:right; font-weight:600">{{ item.lineTotal | currency }}</span>
              <button class="btn" (click)="remove(item)">Remove</button>
            </div>
          }

          <div class="summary" style="margin-top:20px">
            <div class="line total">
              <span>Total</span>
              <span>{{ cart.cart()!.totalAmount | currency }}</span>
            </div>
            <a class="btn btn-primary btn-block" routerLink="/checkout" style="margin-top:14px">Checkout</a>
          </div>
        </div>
      }
    </div>
  `,
})
export class CartPage {
  auth = inject(Auth);
  cart = inject(Cart);

  changeQty(item: CartItem, e: Event): void {
    const qty = Number((e.target as HTMLInputElement).value);
    if (qty >= 1) this.cart.updateQuantity(item.productId, qty).subscribe();
  }

  remove(item: CartItem): void {
    this.cart.remove(item.productId).subscribe();
  }
}
