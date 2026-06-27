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
      <h1>Products</h1>
      <a class="btn btn-primary" routerLink="/products/new">+ New product</a>
    </div>

    <div class="toolbar">
      <input class="input" placeholder="Search products…" [(ngModel)]="search" (keyup.enter)="load()" />
      <button class="btn" (click)="load()">Search</button>
    </div>

    <div class="card">
      @if (loading()) {
        <p class="muted">Loading…</p>
      } @else if (products().length === 0) {
        <p class="muted">No products found.</p>
      } @else {
        <table>
          <thead>
            <tr>
              <th>Name</th><th>SKU</th><th>Category</th><th>Price</th><th>Stock</th><th>Status</th><th></th>
            </tr>
          </thead>
          <tbody>
            @for (p of products(); track p.id) {
              <tr>
                <td>{{ p.name }}</td>
                <td class="muted">{{ p.sku }}</td>
                <td>{{ p.category }}</td>
                <td>{{ p.price | currency }}</td>
                <td>{{ p.stock }}</td>
                <td>
                  <span class="badge" [class.on]="p.isActive" [class.off]="!p.isActive">
                    {{ p.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </td>
                <td style="text-align:right; white-space:nowrap">
                  <a class="btn btn-sm" [routerLink]="['/products', p.id]">Edit</a>
                  <button class="btn btn-sm" (click)="toggle(p)">
                    {{ p.isActive ? 'Deactivate' : 'Activate' }}
                  </button>
                </td>
              </tr>
            }
          </tbody>
        </table>
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
