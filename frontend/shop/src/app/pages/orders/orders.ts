import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { BdtPipe } from '../../core/bdt.pipe';
import { Order } from '../../core/models';
import { I18n } from '../../core/i18n';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-orders',
  imports: [DatePipe, RouterLink, BdtPipe, TranslatePipe],
  template: `
    <div class="container" style="max-width:760px">
      <h1>{{ 'orders.title' | t }}</h1>

      @if (loading()) {
        <p class="muted">{{ 'common.loading' | t }}</p>
      } @else if (orders().length === 0) {
        <div class="empty">{{ 'orders.noOrders' | t }}<br /><a class="btn btn-primary" routerLink="/" style="margin-top:14px">{{ 'common.startShopping' | t }}</a></div>
      } @else {
        @for (o of orders(); track o.id) {
          <div class="panel" style="margin-bottom:16px">
            <div style="display:flex; justify-content:space-between; align-items:center; gap:12px">
              <div>
                <strong>{{ i18n.t('orders.orderNumber', { id: o.id.slice(0, 8).toUpperCase() }) }}</strong>
                <div class="muted" style="font-size:13px">{{ (o.updatedAt || o.createdAt) | date: 'medium' }}</div>
              </div>
              <div style="display:flex; align-items:center; gap:10px; flex-wrap:wrap">
                <span class="pill"
                      [class.in]="o.status === 'CheckedOut' || o.status === 'Confirmed' || o.status === 'Shipped'"
                      [class.out]="o.status === 'Cancelled'">
                  {{ statusLabel(o.status) }}
                </span>
                <button class="btn btn-sm" (click)="track(o.id)">
                  {{ (trackingId() === o.id ? 'orders.hideTracking' : 'orders.trackOrder') | t }}
                </button>
                @if (o.status === 'CheckedOut') {
                  <button class="btn btn-sm" [disabled]="cancelling() === o.id" (click)="cancel(o)">
                    {{ (cancelling() === o.id ? 'orders.cancelling' : 'orders.cancelOrder') | t }}
                  </button>
                }
              </div>
            </div>

            @if (trackingId() === o.id) {
              <div class="track">
                @for (step of timeline(o); track $index) {
                  <div class="track-step" [class.last]="$last" [class.pending]="!step.done">
                    <span class="track-dot" [class.cancel]="step.cancel"></span>
                    <div class="track-info">
                      <div class="track-status">{{ step.label }}</div>
                      @if (step.note) { <div class="track-note">{{ step.note }}</div> }
                      @if (step.time) { <div class="track-time">{{ step.time | date: 'medium' }}</div> }
                      @else if (!step.done) { <div class="track-time">{{ 'orders.pending' | t }}</div> }
                    </div>
                  </div>
                }
              </div>
            }

            <div class="order-lines">
              @for (item of o.items; track item.id) {
                <div class="l"><span>{{ item.productName }} <span class="muted">× {{ item.quantity }}</span></span><span>{{ item.lineTotal | bdt }}</span></div>
              }
              <div class="l" style="font-weight:800; border-top:1px solid var(--line); margin-top:6px; padding-top:10px">
                <span>{{ 'common.total' | t }}</span><span>{{ o.totalAmount | bdt }}</span>
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
  i18n = inject(I18n);

  orders = signal<Order[]>([]);
  loading = signal(false);
  cancelling = signal<string | null>(null);
  trackingId = signal<string | null>(null);

  ngOnInit(): void { this.load(); }

  track(id: string): void { this.trackingId.set(this.trackingId() === id ? null : id); }

  statusLabel(s: string): string {
    const key: Record<string, string> = {
      CheckedOut: 'orders.statusPlaced',
      Confirmed: 'orders.statusConfirmed',
      Shipped: 'orders.statusShipped',
      Cancelled: 'orders.statusCancelled',
    };
    return key[s] ? this.i18n.t(key[s]) : s;
  }

  // The normal order journey. The tracker always shows every step so the customer sees the full
  // path — completed steps (with their real timestamp where recorded) and the remaining ones as
  // "Pending". Cancelled orders show the steps reached, then a Cancelled step.
  private readonly linear = ['CheckedOut', 'Confirmed', 'Shipped'];

  timeline(o: Order): { label: string; note?: string | null; time?: string | null; done: boolean; cancel: boolean }[] {
    const hist = new Map((o.statusHistory ?? []).map((h) => [h.status, h]));
    const build = (status: string, done: boolean, cancel = false) => {
      const h = hist.get(status);
      const time = h?.createdAt ?? (status === 'CheckedOut' && done ? o.createdAt : null);
      return { label: this.statusLabel(status), note: h?.note, time, done, cancel };
    };

    if (o.status === 'Cancelled') {
      const reached = this.linear.filter((s) => hist.has(s));
      if (!reached.includes('CheckedOut')) reached.unshift('CheckedOut');
      return [...reached.map((s) => build(s, true)), build('Cancelled', true, true)];
    }

    const current = this.linear.indexOf(o.status);
    return this.linear.map((s, i) => build(s, i <= current));
  }

  load(): void {
    this.loading.set(true);
    this.api.get<Order[]>('/api/carts/my-orders').subscribe({
      next: (o) => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  cancel(o: Order): void {
    if (!confirm(this.i18n.t('orders.cancelConfirm', { id: o.id.slice(0, 8).toUpperCase() }))) return;
    this.cancelling.set(o.id);
    this.api.post(`/api/carts/my-orders/${o.id}/cancel`, { reason: 'Cancelled by customer' }).subscribe({
      next: () => { this.cancelling.set(null); this.load(); },
      error: (e) => { this.cancelling.set(null); alert(e?.error?.message ?? this.i18n.t('orders.couldNotCancel')); },
    });
  }
}
