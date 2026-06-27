import { Component, OnInit, inject, signal } from '@angular/core';
import { Api } from '../../core/api';
import { InventoryItem, Paged, Product, User } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  template: `
    <div class="page-head"><h1>Dashboard</h1></div>

    <div class="stat-grid">
      <div class="card stat">
        <div class="label">Products</div>
        <div class="num">{{ products() }}</div>
      </div>
      <div class="card stat">
        <div class="label">Customers</div>
        <div class="num">{{ users() }}</div>
      </div>
      <div class="card stat">
        <div class="label">Low stock items</div>
        <div class="num" style="color:#d98e04">{{ lowStock() }}</div>
      </div>
    </div>
  `,
})
export class DashboardPage implements OnInit {
  private api = inject(Api);

  products = signal(0);
  users = signal(0);
  lowStock = signal(0);

  ngOnInit(): void {
    this.api
      .get<Paged<Product>>('/api/products', { pageNumber: 1, pageSize: 1 })
      .subscribe((r) => this.products.set(r.totalRecords));

    this.api
      .get<Paged<User>>('/api/auth/users', { pageNumber: 1, pageSize: 1 })
      .subscribe((r) => this.users.set(r.totalRecords));

    this.api
      .get<Paged<InventoryItem>>('/api/inventory', { pageNumber: 1, pageSize: 200 })
      .subscribe((r) => this.lowStock.set(r.items.filter((i) => i.quantityAvailable <= i.reorderLevel).length));
  }
}
