import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Api } from '../../core/api';
import { Order, Paged, User } from '../../core/models';

@Component({
  selector: 'app-orders',
  imports: [CurrencyPipe, DatePipe],
  template: `
    <div class="page-head">
      <div><h1>Orders</h1><div class="sub">{{ orders().length }} orders placed by customers</div></div>
    </div>

    <div class="card">
      @if (loading()) {
        <div class="empty">Loading…</div>
      } @else if (orders().length === 0) {
        <div class="empty">No orders yet.</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead><tr><th>Order</th><th>Customer</th><th>Items</th><th>Total</th><th>Placed</th><th>Status</th></tr></thead>
            <tbody>
              @for (o of orders(); track o.id) {
                <tr style="cursor:pointer" (click)="toggle(o.id)">
                  <td class="cell-main">#{{ o.id.slice(0, 8).toUpperCase() }}</td>
                  <td>
                    <div class="cell-main">{{ customerName(o.userId) }}</div>
                    <div class="cell-sub">{{ customerEmail(o.userId) }}</div>
                  </td>
                  <td>{{ o.itemCount }} item(s)</td>
                  <td class="cell-main">{{ o.totalAmount | currency: 'BDT' : '৳' }}</td>
                  <td class="muted">{{ (o.updatedAt || o.createdAt) | date: 'medium' }}</td>
                  <td><span class="badge on">{{ o.status }}</span></td>
                </tr>
                @if (expandedId() === o.id) {
                  <tr>
                    <td colspan="6" style="background:var(--thead)">
                      <div style="padding:6px 0">
                        @for (it of o.items; track it.id) {
                          <div style="display:flex; justify-content:space-between; padding:4px 0; max-width:560px">
                            <span>{{ it.productName }} <span class="muted">× {{ it.quantity }}</span></span>
                            <span class="cell-main">{{ it.lineTotal | currency: 'BDT' : '৳' }}</span>
                          </div>
                        }
                      </div>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class OrdersPage implements OnInit {
  private api = inject(Api);

  orders = signal<Order[]>([]);
  loading = signal(false);
  expandedId = signal<string | null>(null);
  private users = signal<Map<string, User>>(new Map());

  ngOnInit(): void {
    this.loading.set(true);
    this.api.get<Order[]>('/api/carts/orders').subscribe({
      next: (o) => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
    // Resolve customer names/emails by userId (admin-only listing).
    this.api.get<Paged<User>>('/api/auth/users', { pageNumber: 1, pageSize: 500 })
      .subscribe((r) => this.users.set(new Map(r.items.map((u) => [u.id, u]))));
  }

  toggle(id: string): void { this.expandedId.set(this.expandedId() === id ? null : id); }

  customerName(userId: string): string {
    const u = this.users().get(userId);
    return u ? `${u.firstName} ${u.lastName}` : 'Unknown customer';
  }
  customerEmail(userId: string): string {
    return this.users().get(userId)?.email ?? userId.slice(0, 8);
  }
}
