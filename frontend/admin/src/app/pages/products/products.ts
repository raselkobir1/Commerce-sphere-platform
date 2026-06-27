import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { Paged, Product } from '../../core/models';

@Component({
  selector: 'app-products',
  imports: [CurrencyPipe, FormsModule, RouterLink],
  template: `
    <div class="page-head">
      <div><h1>Products</h1><div class="sub">{{ products().length }} products in your catalog</div></div>
      <a class="btn btn-primary" routerLink="/products/new">+ New product</a>
    </div>

    <div class="toolbar">
      <div class="search">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/></svg>
        <input class="input" placeholder="Search products…" [(ngModel)]="search" (keyup.enter)="load()" />
      </div>
      <button class="btn" (click)="load()">Search</button>
    </div>

    <div class="card">
      @if (loading()) {
        <div class="empty">Loading…</div>
      } @else if (products().length === 0) {
        <div class="empty">No products found.</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr><th>Product</th><th>Category</th><th>Price</th><th>Stock</th><th>Status</th><th class="right">Actions</th></tr>
            </thead>
            <tbody>
              @for (p of products(); track p.id) {
                <tr>
                  <td><div class="cell-flex">
                    <span class="thumb" [style.background-image]="p.imageUrl ? 'url(' + p.imageUrl + ')' : null">{{ p.imageUrl ? '' : p.name.slice(0,1) }}</span>
                    <div><div class="cell-main">{{ p.name }}</div><div class="cell-sub">{{ p.sku }}</div></div>
                  </div></td>
                  <td><span class="chip">{{ p.category }}</span></td>
                  <td class="cell-main">{{ p.price | currency }}</td>
                  <td>{{ p.stock }}</td>
                  <td><span class="badge" [class.on]="p.isActive" [class.off]="!p.isActive">{{ p.isActive ? 'Active' : 'Inactive' }}</span></td>
                  <td class="right"><div class="actions">
                    <a class="btn btn-sm" [routerLink]="['/products', p.id]">Edit</a>
                    <button class="btn btn-sm" (click)="toggle(p)">{{ p.isActive ? 'Deactivate' : 'Activate' }}</button>
                  </div></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class ProductsPage implements OnInit {
  private api = inject(Api);

  products = signal<Product[]>([]);
  loading = signal(false);
  search = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api
      .get<Paged<Product>>('/api/products', { pageNumber: 1, pageSize: 50, searchTerm: this.search })
      .subscribe({
        next: (r) => {
          this.products.set(r.items);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  toggle(p: Product): void {
    const path = `/api/products/${p.id}/${p.isActive ? 'deactivate' : 'activate'}`;
    this.api.patch(path).subscribe(() => this.load());
  }
}
