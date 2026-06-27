import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Orders } from '../../core/orders';
import { BdtPipe } from '../../core/bdt.pipe';

@Component({
  selector: 'app-order-success',
  imports: [BdtPipe, DatePipe, RouterLink],
  template: `
    <div class="container">
      @if (orders.last(); as o) {
        <div class="success">
          <div class="check">✓</div>
          <h1>Thank you for your order!</h1>
          <p class="muted">Your order has been placed and will be paid by <strong>Cash on Delivery</strong>.</p>
          <div class="ref">Order ref: {{ o.reference }}</div>

          <div class="panel" style="text-align:left">
            <h2>Delivery to</h2>
            <p style="margin:0">
              {{ o.address.fullName }}<br />
              {{ o.address.line1 }}, {{ o.address.city }} {{ o.address.postcode }}<br />
              📞 {{ o.address.phone }}
            </p>
            <p class="muted" style="margin-top:8px">Placed {{ o.placedAt | date: 'medium' }}</p>

            <div class="order-lines">
              @for (item of o.items; track item.id) {
                <div class="l"><span>{{ item.productName }} × {{ item.quantity }}</span><span>{{ item.lineTotal | bdt }}</span></div>
              }
              <div class="l" style="font-weight:800;border-top:1px solid var(--line);margin-top:6px;padding-top:10px">
                <span>Total (pay on delivery)</span><span>{{ o.total | bdt }}</span>
              </div>
            </div>
          </div>

          <a class="btn btn-primary" routerLink="/">Continue shopping</a>
        </div>
      } @else {
        <div class="empty">No recent order to show. <a class="btn-ghost" routerLink="/">Go shopping</a></div>
      }
    </div>
  `,
})
export class OrderSuccessPage {
  orders = inject(Orders);
}
