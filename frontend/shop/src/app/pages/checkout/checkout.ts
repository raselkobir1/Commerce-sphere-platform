import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { Orders } from '../../core/orders';
import { PlacedOrder } from '../../core/models';
import { BdtPipe } from '../../core/bdt.pipe';

@Component({
  selector: 'app-checkout',
  imports: [BdtPipe, FormsModule, RouterLink],
  template: `
    <div class="container">
      <h1>Checkout</h1>

      @if (!cart.cart() || cart.cart()!.items.length === 0) {
        <div class="empty">Your cart is empty. <a class="btn-ghost" routerLink="/">Browse products</a></div>
      } @else {
        <div class="checkout-grid">
          <form (ngSubmit)="placeOrder()">
            <!-- Shipping -->
            <div class="panel">
              <div class="step"><span class="n">1</span><h2 style="margin:0">Shipping address</h2></div>
              <div class="field">
                <label>Full name</label>
                <input class="input" name="fullName" [(ngModel)]="form.fullName" required />
              </div>
              <div class="field">
                <label>Phone</label>
                <input class="input" name="phone" [(ngModel)]="form.phone" required />
              </div>
              <div class="field">
                <label>Address</label>
                <input class="input" name="line1" [(ngModel)]="form.line1" placeholder="Street address" required />
              </div>
              <div class="row2">
                <div class="field"><label>City</label><input class="input" name="city" [(ngModel)]="form.city" required /></div>
                <div class="field"><label>Postcode</label><input class="input" name="postcode" [(ngModel)]="form.postcode" required /></div>
              </div>
            </div>

            <!-- Payment -->
            <div class="panel">
              <div class="step"><span class="n">2</span><h2 style="margin:0">Payment method</h2></div>
              <label class="pay-option selected">
                <input type="radio" name="pay" checked />
                <span class="ico">💵</span>
                <span><div class="t">Cash on Delivery</div><div class="muted">Pay with cash when your order arrives</div></span>
              </label>
              <div class="pay-option disabled">
                <input type="radio" name="pay" disabled />
                <span class="ico">💳</span>
                <span><div class="t">Card / Online payment</div><div class="muted">Coming soon</div></span>
              </div>
            </div>

            @if (error()) { <p class="error">{{ error() }}</p> }
          </form>

          <!-- Summary -->
          <div class="summary">
            <h2>Your order</h2>
            @for (item of cart.cart()!.items; track item.id) {
              <div class="line"><span>{{ item.productName }} × {{ item.quantity }}</span><span>{{ item.lineTotal | bdt }}</span></div>
            }
            <div class="line"><span>Shipping</span><span style="color:var(--green)">Free</span></div>
            <div class="line total"><span>Total (COD)</span><span>{{ cart.cart()!.totalAmount | bdt }}</span></div>
            <button class="btn btn-primary btn-block" style="margin-top:14px" (click)="placeOrder()" [disabled]="placing()">
              {{ placing() ? 'Placing order…' : 'Place order' }}
            </button>
            <p class="muted" style="font-size:13px;text-align:center;margin-top:10px">You'll pay {{ cart.cart()!.totalAmount | bdt }} in cash on delivery.</p>
          </div>
        </div>
      }
    </div>
  `,
})
export class CheckoutPage {
  cart = inject(Cart);
  private auth = inject(Auth);
  private orders = inject(Orders);
  private router = inject(Router);

  form = { fullName: '', phone: '', line1: '', city: '', postcode: '' };
  placing = signal(false);
  error = signal('');

  placeOrder(): void {
    this.error.set('');
    const current = this.cart.cart();
    if (!current || current.items.length === 0) return;

    const f = this.form;
    if (!f.fullName || !f.phone || !f.line1 || !f.city || !f.postcode) {
      this.error.set('Please fill in all shipping fields.');
      return;
    }

    // Snapshot the cart now — checkout() clears it on success.
    const snapshot = current.items.map((i) => ({ ...i }));
    const total = current.totalAmount;
    const reference = 'SS-' + current.id.slice(0, 8).toUpperCase();

    this.placing.set(true);
    this.cart.checkout().subscribe({
      next: () => {
        const order: PlacedOrder = {
          reference,
          items: snapshot,
          total,
          address: { ...f },
          paymentMethod: 'COD',
          placedAt: new Date(),
        };
        this.orders.last.set(order);
        this.placing.set(false);
        this.router.navigate(['/order-success']);
      },
      error: (err) => {
        this.placing.set(false);
        this.error.set(err?.error?.message ?? 'Checkout failed. Please try again.');
      },
    });
  }
}
