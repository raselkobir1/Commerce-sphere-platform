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
              <div style="display:flex; align-items:center; gap:10px; flex-wrap:wrap">
                <span class="pill"
                      [class.in]="o.status === 'CheckedOut' || o.status === 'Confirmed' || o.status === 'Shipped'"
                      [class.out]="o.status === 'Cancelled'">
                  {{ statusLabel(o.status) }}
                </span>
                <button class="btn btn-sm" (click)="track(o.id)">
                  {{ trackingId() === o.id ? 'Hide tracking' : 'Track order' }}
                </button>
                @if (o.status === 'CheckedOut') {
                  <button class="btn btn-sm" [disabled]="cancelling() === o.id" (click)="cancel(o)">
                    {{ cancelling() === o.id ? 'Cancelling…' : 'Cancel order' }}
                  </button>
                }
              </div>
            </div>

            @if (trackingId() === o.id) {
              <div class="track">
                @for (h of (o.statusHistory ?? []); track $index) {
                  <div class="track-step" [class.last]="$last">
                    <span class="track-dot" [class.cancel]="h.status === 'Cancelled'"></span>
                    <div class="track-info">
                      <div class="track-status">{{ statusLabel(h.status) }}</div>
                      @if (h.note) { <div class="track-note">{{ h.note }}</div> }
                      <div class="track-time">{{ h.createdAt | date: 'medium' }}</div>
                    </div>
                  </div>
                }
                @if (!(o.statusHistory && o.statusHistory.length)) {
                  <div class="muted">No tracking details yet.</div>
                }
              </div>
            }

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
  cancelling = signal<string | null>(null);
  trackingId = signal<string | null>(null);

  ngOnInit(): void { this.load(); }

  track(id: string): void { this.trackingId.set(this.trackingId() === id ? null : id); }

  statusLabel(s: string): string { return s === 'CheckedOut' ? 'Placed' : s; }

  load(): void {
    this.loading.set(true);
    this.api.get<Order[]>('/api/carts/my-orders').subscribe({
      next: (o) => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  cancel(o: Order): void {
    if (!confirm(`Cancel order #${o.id.slice(0, 8).toUpperCase()}? Any reserved stock is released and the store team is notified.`)) return;
    this.cancelling.set(o.id);
    this.api.post(`/api/carts/my-orders/${o.id}/cancel`, { reason: 'Cancelled by customer' }).subscribe({
      next: () => { this.cancelling.set(null); this.load(); },
      error: (e) => { this.cancelling.set(null); alert(e?.error?.message ?? 'Could not cancel the order.'); },
    });
  }
}
