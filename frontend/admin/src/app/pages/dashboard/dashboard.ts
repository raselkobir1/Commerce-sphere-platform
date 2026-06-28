import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { Perms } from '../../core/perms';
import { InventoryItem, Paged, Product, User } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, RouterLink],
  template: `
    <div class="page-head">
      <div><h1>Dashboard</h1><div class="sub">Store overview at a glance</div></div>
      @if (perms.can('products', 'create')) {
        <a class="btn btn-primary" routerLink="/products/new">+ New product</a>
      }
    </div>

    <!-- Stat cards -->
    <div class="stats">
      <div class="stat">
        <div class="tile indigo"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20 7 12 3 4 7v10l8 4 8-4z"/><path d="m4 7 8 4 8-4"/></svg></div>
        <div><div class="label">Total products</div><div class="num">{{ totalProducts() }}</div>
          <span class="trend flat">{{ activeProducts() }} active</span></div>
      </div>
      @if (perms.canView('users')) {
        <div class="stat">
          <div class="tile sky"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/></svg></div>
          <div><div class="label">Customers</div><div class="num">{{ customers() }}</div>
            <span class="trend up">registered</span></div>
        </div>
      }
      @if (perms.canView('inventory')) {
        <div class="stat">
          <div class="tile amber"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z"/><path d="M12 9v4M12 17h.01"/></svg></div>
          <div><div class="label">Low stock</div><div class="num">{{ lowStock().length }}</div>
            <span class="trend" [class.down]="lowStock().length" [class.flat]="!lowStock().length">needs restock</span></div>
        </div>
      }
      <div class="stat">
        <div class="tile green"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/></svg></div>
        <div><div class="label">Categories</div><div class="num">{{ categoryBars().length }}</div>
          <span class="trend flat">in catalog</span></div>
      </div>
    </div>

    <!-- Chart + low stock -->
    <div class="grid-2">
      <div class="card">
        <div class="card-head"><h2>Top categories</h2><span class="muted">by product count</span></div>
        @if (categoryBars().length) {
          <div class="barchart">
            @for (c of categoryBars(); track c.name) {
              <div class="barrow">
                <span class="bl">{{ c.name }}</span>
                <span class="bartrack"><span class="bf" [style.width.%]="c.pct"></span></span>
                <span class="bn">{{ c.count }}</span>
              </div>
            }
          </div>
        } @else { <div class="empty">No products yet.</div> }
      </div>

      @if (perms.canView('inventory')) {
        <div class="card">
          <div class="card-head"><h2>Low stock alerts</h2><a routerLink="/inventory" class="muted">View all</a></div>
          @if (lowStock().length) {
            @for (i of lowStock().slice(0, 6); track i.id) {
              <div class="list-row">
                <span class="thumb">{{ i.sku.slice(0, 2) }}</span>
                <div class="gr"><div class="t">{{ i.sku }}</div><div class="s">reorder at {{ i.reorderLevel }}</div></div>
                <span class="badge low">{{ i.quantityAvailable }} left</span>
              </div>
            }
          } @else { <div class="empty">✅ All items are well stocked.</div> }
        </div>
      }
    </div>

    <!-- Recent products -->
    <div class="card">
      <div class="card-head"><h2>Products</h2><a routerLink="/products" class="muted">Manage all</a></div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>Product</th><th>Category</th><th>Price</th><th>Stock</th><th>Status</th></tr></thead>
          <tbody>
            @for (p of preview(); track p.id) {
              <tr>
                <td><div class="cell-flex">
                  <span class="thumb" [style.background-image]="p.imageUrl ? 'url(' + p.imageUrl + ')' : null">{{ p.imageUrl ? '' : p.name.slice(0,1) }}</span>
                  <div><div class="cell-main">{{ p.name }}</div><div class="cell-sub">{{ p.sku }}</div></div>
                </div></td>
                <td><span class="chip">{{ p.category }}</span></td>
                <td class="cell-main">{{ p.price | currency }}</td>
                <td>{{ p.stock }}</td>
                <td><span class="badge" [class.on]="p.isActive" [class.off]="!p.isActive">{{ p.isActive ? 'Active' : 'Inactive' }}</span></td>
              </tr>
            } @empty { <tr><td colspan="5"><div class="empty">No products yet.</div></td></tr> }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class DashboardPage implements OnInit {
  private api = inject(Api);
  perms = inject(Perms);

  private products = signal<Product[]>([]);
  totalProducts = signal(0);
  customers = signal(0);
  lowStock = signal<InventoryItem[]>([]);

  activeProducts = computed(() => this.products().filter((p) => p.isActive).length);
  preview = computed(() => this.products().slice(0, 6));
  categoryBars = computed(() => {
    const counts = new Map<string, number>();
    for (const p of this.products()) counts.set(p.category, (counts.get(p.category) ?? 0) + 1);
    const rows = [...counts].map(([name, count]) => ({ name, count })).sort((a, b) => b.count - a.count).slice(0, 6);
    const max = Math.max(1, ...rows.map((r) => r.count));
    return rows.map((r) => ({ ...r, pct: Math.round((r.count / max) * 100) }));
  });

  ngOnInit(): void {
    // Read-only overview: these are background stat loads, so a failure should leave the tile
    // empty rather than alarm the admin with an error toast (toastError: false). An expired
    // token is handled globally by the auth interceptor.
    // Only load the stats this role can actually see — a Manager without users/inventory
    // permission never makes those calls (no 403), and the matching tiles stay hidden.
    const silent = { toastError: false };
    this.api.get<Paged<Product>>('/api/products', { pageNumber: 1, pageSize: 200 }, silent).subscribe({
      next: (r) => {
        this.products.set(r.items);
        this.totalProducts.set(r.totalRecords || r.items.length);
      },
      error: () => {},
    });
    if (this.perms.canView('users')) {
      this.api.get<Paged<User>>('/api/auth/users', { pageNumber: 1, pageSize: 1 }, silent)
        .subscribe({ next: (r) => this.customers.set(r.totalRecords), error: () => {} });
    }
    if (this.perms.canView('inventory')) {
      this.api.get<Paged<InventoryItem>>('/api/inventory', { pageNumber: 1, pageSize: 200 }, silent)
        .subscribe({ next: (r) => this.lowStock.set(r.items.filter((i) => i.quantityAvailable <= i.reorderLevel)), error: () => {} });
    }
  }
}
