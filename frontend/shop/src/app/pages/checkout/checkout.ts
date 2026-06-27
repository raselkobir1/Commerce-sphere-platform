import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Cart } from '../../core/cart';

@Component({
  selector: 'app-checkout',
  imports: [CurrencyPipe, RouterLink],
  template: `
    <div class="container" style="max-width:560px">
      <h1>Checkout</h1>

      @if (done()) {
        <div class="card">
          <p class="notice">🎉 Order placed! Thank you for shopping with ShopSphere.</p>
          <a class="btn btn-primary" routerLink="/">Continue shopping</a>
        </div>
      } @else if (!cart.cart() || cart.cart()!.items.length === 0) {
        <p class="muted">Your cart is empty. <a routerLink="/">Browse products</a>.</p>
      } @else {
        <div class="summary">
          @for (item of cart.cart()!.items; track item.id) {
            <div class="line">
              <span>{{ item.productName }} × {{ item.quantity }}</span>
              <span>{{ item.lineTotal | currency }}</span>
            </div>
          }
          <div class="line total">
            <span>Total</span>
            <span>{{ cart.cart()!.totalAmount | currency }}</span>
          </div>

          @if (error()) {
            <p class="error">{{ error() }}</p>
          }

          <button class="btn btn-primary btn-block" style="margin-top:16px"
                  (click)="placeOrder()" [disabled]="placing()">
            {{ placing() ? 'Placing order…' : 'Place order' }}
          </button>
        </div>
      }
    </div>
  `,
})
export class CheckoutPage {
  cart = inject(Cart);

  placing = signal(false);
  done = signal(false);
  error = signal('');

  placeOrder(): void {
    this.error.set('');
    this.placing.set(true);
    this.cart.checkout().subscribe({
      next: () => {
        this.placing.set(false);
        this.done.set(true);
      },
      error: (err) => {
        this.placing.set(false);
        this.error.set(err?.error?.message ?? 'Checkout failed. Please try again.');
      },
    });
  }
}
