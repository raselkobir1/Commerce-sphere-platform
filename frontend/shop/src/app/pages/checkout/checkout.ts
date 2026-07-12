import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { Orders } from '../../core/orders';
import { PlacedOrder } from '../../core/models';
import { BdtPipe } from '../../core/bdt.pipe';
import { I18n } from '../../core/i18n';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-checkout',
  imports: [BdtPipe, FormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="container">
      <h1>{{ 'checkout.title' | t }}</h1>

      @if (!cart.cart() || cart.cart()!.items.length === 0) {
        <div class="empty">{{ 'checkout.emptyCart' | t }} <a class="btn-ghost" routerLink="/">{{ 'checkout.browseProducts' | t }}</a></div>
      } @else {
        <div class="checkout-grid">
          <form (ngSubmit)="placeOrder()">
            <!-- Shipping -->
            <div class="panel">
              <div class="step"><span class="n">1</span><h2 style="margin:0">{{ 'checkout.shippingAddress' | t }}</h2></div>
              <div class="field">
                <label>{{ 'checkout.fullName' | t }}</label>
                <input class="input" name="fullName" [(ngModel)]="form.fullName" required />
              </div>
              <div class="field">
                <label>{{ 'checkout.phone' | t }}</label>
                <input class="input" name="phone" [(ngModel)]="form.phone" required />
              </div>
              <div class="field">
                <label>{{ 'checkout.address' | t }}</label>
                <input class="input" name="line1" [(ngModel)]="form.line1" [placeholder]="'checkout.streetAddress' | t" required />
              </div>
              <div class="row2">
                <div class="field"><label>{{ 'checkout.city' | t }}</label><input class="input" name="city" [(ngModel)]="form.city" required /></div>
                <div class="field"><label>{{ 'checkout.postcode' | t }}</label><input class="input" name="postcode" [(ngModel)]="form.postcode" required /></div>
              </div>
            </div>

            <!-- Payment -->
            <div class="panel">
              <div class="step"><span class="n">2</span><h2 style="margin:0">{{ 'checkout.paymentMethod' | t }}</h2></div>
              <label class="pay-option selected">
                <input type="radio" name="pay" checked />
                <span class="ico">💵</span>
                <span><div class="t">{{ 'checkout.cod' | t }}</div><div class="muted">{{ 'checkout.codDesc' | t }}</div></span>
              </label>
              <div class="pay-option disabled">
                <input type="radio" name="pay" disabled />
                <span class="ico">💳</span>
                <span><div class="t">{{ 'checkout.cardPayment' | t }}</div><div class="muted">{{ 'checkout.comingSoon' | t }}</div></span>
              </div>
            </div>

            @if (error()) { <p class="error">{{ error() }}</p> }
          </form>

          <!-- Summary -->
          <div class="summary">
            <h2>{{ 'checkout.yourOrder' | t }}</h2>
            @for (item of cart.cart()!.items; track item.id) {
              <div class="line"><span>{{ item.productName }} × {{ item.quantity }}</span><span>{{ item.lineTotal | bdt }}</span></div>
            }
            <div class="line"><span>{{ 'common.shipping' | t }}</span><span style="color:var(--green)">{{ 'common.free' | t }}</span></div>
            <div class="line total"><span>{{ 'checkout.totalCod' | t }}</span><span>{{ cart.cart()!.totalAmount | bdt }}</span></div>
            <button class="btn btn-primary btn-block" style="margin-top:14px" (click)="placeOrder()" [disabled]="placing()">
              {{ (placing() ? 'checkout.placingOrder' : 'checkout.placeOrder') | t }}
            </button>
            <p class="muted" style="font-size:13px;text-align:center;margin-top:10px">{{ i18n.t('checkout.payCashNote', { amount: cashAmount() }) }}</p>
          </div>
        </div>
      }
    </div>
  `,
})
export class CheckoutPage {
  cart = inject(Cart);
  i18n = inject(I18n);
  private auth = inject(Auth);
  private orders = inject(Orders);
  private router = inject(Router);

  form = { fullName: '', phone: '', line1: '', city: '', postcode: '' };
  placing = signal(false);
  error = signal('');

  cashAmount(): string {
    return new BdtPipe().transform(this.cart.cart()?.totalAmount);
  }

  placeOrder(): void {
    this.error.set('');
    const current = this.cart.cart();
    if (!current || current.items.length === 0) return;

    const f = this.form;
    if (!f.fullName || !f.phone || !f.line1 || !f.city || !f.postcode) {
      this.error.set(this.i18n.t('checkout.fillAllFields'));
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
        this.error.set(err?.error?.message ?? this.i18n.t('checkout.checkoutFailed'));
      },
    });
  }
}
