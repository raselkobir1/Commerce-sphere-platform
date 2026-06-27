import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { Paged, Product } from '../../core/models';

@Component({
  selector: 'app-catalog',
  imports: [CurrencyPipe, FormsModule, RouterLink],
  template: `
    <div class="container">
      <h1>Shop our products</h1>

      <div class="toolbar">
        <input class="input" placeholder="Search…" [(ngModel)]="search" (keyup.enter)="load()" style="flex:1" />
        <select class="input" [(ngModel)]="category" (change)="load()">
          <option value="">All categories</option>
          @for (c of categories(); track c) {
            <option [value]="c">{{ c }}</option>
          }
        </select>
        <button class="btn" (click)="load()">Search</button>
      </div>

      @if (loading()) {
        <p class="muted">Loading products…</p>
      } @else if (products().length === 0) {
        <p class="muted">No products found.</p>
      } @else {
        <div class="grid">
          @for (p of products(); track p.id) {
            <div class="product-card">
              <a [routerLink]="['/product', p.id]" class="thumb"
                 [style.background-image]="p.imageUrl ? 'url(' + p.imageUrl + ')' : null">
                @if (!p.imageUrl) { <span>No image</span> }
              </a>
              <div class="body">
                <a class="name" [routerLink]="['/product', p.id]">{{ p.name }}</a>
                <span class="cat">{{ p.category }}</span>
                <span class="price">{{ p.price | currency }}</span>
                <button class="btn btn-primary btn-block" (click)="addToCart(p)">Add to cart</button>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class CatalogPage implements OnInit {
  private api = inject(Api);
  private auth = inject(Auth);
  private cart = inject(Cart);
  private router = inject(Router);

  products = signal<Product[]>([]);
  categories = signal<string[]>([]);

  search = '';
  category = '';
  loading = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api
      .get<Paged<Product>>('/api/products', {
        pageNumber: 1,
        pageSize: 100,
        searchTerm: this.search,
        category: this.category,
      })
      .subscribe({
        next: (r) => {
          const active = r.items.filter((p) => p.isActive);
          this.products.set(active);
          // Keep building the category list from everything we've seen.
          if (this.categories().length === 0 && !this.category) {
            this.categories.set([...new Set(active.map((p) => p.category))].sort());
          }
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  addToCart(p: Product): void {
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.cart.add(p).subscribe();
  }
}
