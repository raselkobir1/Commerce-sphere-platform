import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { BdtPipe } from '../../core/bdt.pipe';
import { Order } from '../../core/models';

@Component({
  selector: 'app-orders',
  imports: [DatePipe, RouterLink, BdtPipe],
  template: `
    <div class="container" style="max-width:760px">
      <h1>My orders</h1>

      @if (loading()) {
        <p class="muted">Loading…</p>
      } @else if (orders().length === 0) {
        <div class="empty">You haven't placed any orders yet.<br /><a class="btn btn-primary" routerLink="/" style="margin-top:14px">Start shopping</a></div>
      } @else {
        @for (o of orders(); track o.id) {
          <div class="panel" style="margin-bottom:16px">
            <div style="display:flex; justify-content:space-between; align-items:center; gap:12px">
              <div>
                <strong>Order #{{ o.id.slice(0, 8).toUpperCase() }}</strong>
                <div class="muted" style="font-size:13px">{{ (o.updatedAt || o.createdAt) | date: 'medium' }}</div>
              </div>
              <span class="pill" [class.in]="o.status === 'CheckedOut'" [class.out]="o.status === 'Cancelled'">
                {{ o.status === 'CheckedOut' ? 'Placed' : o.status }}
              </span>
            </div>
            <div class="order-lines">
              @for (item of o.items; track item.id) {
                <div class="l"><span>{{ item.productName }} <span class="muted">× {{ item.quantity }}</span></span><span>{{ item.lineTotal | bdt }}</span></div>
              }
              <div class="l" style="font-weight:800; border-top:1px solid var(--line); margin-top:6px; padding-top:10px">
                <span>Total</span><span>{{ o.totalAmount | bdt }}</span>
              </div>
            </div>
          </div>
        }
      }
    </div>
  `,
})
export class OrdersPage implements OnInit {
  private api = inject(Api);

  orders = signal<Order[]>([]);
  loading = signal(false);

  ngOnInit(): void {
    this.loading.set(true);
    this.api.get<Order[]>('/api/carts/my-orders').subscribe({
      next: (o) => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
