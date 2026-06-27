import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { Products } from '../../core/products';
import { Search } from '../../core/search';
import { Toast } from '../../core/toast';
import { Product } from '../../core/models';
import { BdtPipe } from '../../core/bdt.pipe';
import { TAXONOMY, parentOf } from '../../data/taxonomy';
import { ratingFor, reviewsFor, stars } from '../../data/display';

@Component({
  selector: 'app-catalog',
  imports: [BdtPipe, FormsModule, RouterLink],
  template: `
    <div class="container shop-layout">
      <!-- ───── Left filter sidebar ───── -->
      <aside class="filters">
        <div class="group">
          <h3>Categories</h3>
          <button class="cat-parent" [class.active]="!parent() && !sub()" (click)="selectAll()">
            <span>🛍️</span> All products
          </button>
          @for (cat of taxonomy; track cat.name) {
            <button class="cat-parent" [class.active]="parent() === cat.name && !sub()" (click)="selectParent(cat.name)">
              <span>{{ cat.icon }}</span> {{ cat.name }}
            </button>
            @if (parent() === cat.name) {
              @for (s of cat.subs; track s) {
                <button class="cat-sub" [class.active]="sub() === s" (click)="selectSub(s)">
                  {{ s }} <span class="muted">({{ countFor(s) }})</span>
                </button>
              }
            }
          }
        </div>

        <div class="group">
          <h3>Max price</h3>
          <div class="price-row">
            <input class="input" type="number" min="0" placeholder="Any" [ngModel]="maxPrice()"
                   (ngModelChange)="maxPrice.set($event ? +$event : null)" />
            <span class="muted">USD</span>
          </div>
        </div>

        <div class="group">
          <label class="check">
            <input type="checkbox" [ngModel]="inStockOnly()" (ngModelChange)="inStockOnly.set($event)" />
            In stock only
          </label>
        </div>

        <button class="btn btn-block btn-sm" (click)="reset()">Clear all filters</button>
      </aside>

      <!-- ───── Results ───── -->
      <main>
        <div class="results-bar">
          <div>
            <h1 style="font-size:22px;margin:0">{{ heading() }}</h1>
            <span class="muted">{{ filtered().length }} product(s)</span>
          </div>
          <select class="input" [ngModel]="sort()" (ngModelChange)="sort.set($event)">
            <option value="featured">Sort: Featured</option>
            <option value="price-asc">Price: Low to High</option>
            <option value="price-desc">Price: High to Low</option>
            <option value="name">Name: A–Z</option>
          </select>
        </div>

        @if (activeChips().length) {
          <div class="chips" style="margin-bottom:18px">
            @for (c of activeChips(); track c.key) {
              <span class="chip">{{ c.label }} <button (click)="c.clear()">×</button></span>
            }
          </div>
        }

        @if (products.loading()) {
          <p class="muted">Loading products…</p>
        } @else if (filtered().length === 0) {
          <div class="empty">No products match your filters.<br /><button class="btn-ghost" (click)="reset()">Clear filters</button></div>
        } @else {
          <div class="grid">
            @for (p of filtered(); track p.id) {
              <div class="card">
                <a class="thumb" [routerLink]="['/product', p.id]"
                   [style.background-image]="p.imageUrl ? 'url(' + p.imageUrl + ')' : null">
                  @if (p.stock === 0) { <span class="tag out">Out of stock</span> }
                  @else if (p.stock <= 10) { <span class="tag low">Only {{ p.stock }} left</span> }
                  @else { <span class="tag">{{ p.category }}</span> }
                </a>
                <div class="body">
                  <a class="name" [routerLink]="['/product', p.id]">{{ p.name }}</a>
                  <div class="stars">{{ stars(p.id) }} <span>{{ reviews(p.id) }}</span></div>
                  <div class="price">{{ p.price | bdt }}</div>
                  <button class="btn btn-primary btn-sm" [disabled]="p.stock === 0" (click)="add(p)">
                    Add to cart
                  </button>
                </div>
              </div>
            }
          </div>
        }
      </main>
    </div>
  `,
})
export class CatalogPage implements OnInit {
  products = inject(Products);
  private auth = inject(Auth);
  private cart = inject(Cart);
  private search = inject(Search);
  private toast = inject(Toast);
  private router = inject(Router);

  taxonomy = TAXONOMY;

  parent = signal<string | null>(null);
  sub = signal<string | null>(null);
  maxPrice = signal<number | null>(null);
  inStockOnly = signal(false);
  sort = signal<'featured' | 'price-asc' | 'price-desc' | 'name'>('featured');

  // The list after search + category + price + stock filters and sorting.
  filtered = computed(() => {
    const term = this.search.term().trim().toLowerCase();
    const parent = this.parent();
    const sub = this.sub();
    const max = this.maxPrice();

    let list = this.products.all().filter((p) => {
      if (term && !(p.name + ' ' + p.description + ' ' + p.category).toLowerCase().includes(term)) return false;
      if (sub && p.category !== sub) return false;
      if (parent && parentOf(p.category) !== parent) return false;
      if (max != null && p.price > max) return false;
      if (this.inStockOnly() && p.stock === 0) return false;
      return true;
    });

    const by = this.sort();
    list = [...list].sort((a, b) => {
      if (by === 'price-asc') return a.price - b.price;
      if (by === 'price-desc') return b.price - a.price;
      if (by === 'name') return a.name.localeCompare(b.name);
      return 0;
    });
    return list;
  });

  heading = computed(() => this.sub() ?? this.parent() ?? (this.search.term() ? `Results for “${this.search.term()}”` : 'All products'));

  activeChips = computed(() => {
    const chips: { key: string; label: string; clear: () => void }[] = [];
    if (this.search.term()) chips.push({ key: 'q', label: `“${this.search.term()}”`, clear: () => this.search.term.set('') });
    if (this.parent()) chips.push({ key: 'p', label: this.parent()!, clear: () => this.selectAll() });
    if (this.sub()) chips.push({ key: 's', label: this.sub()!, clear: () => this.sub.set(null) });
    if (this.maxPrice() != null) chips.push({ key: 'm', label: `≤ $${this.maxPrice()}`, clear: () => this.maxPrice.set(null) });
    return chips;
  });

  ngOnInit(): void {
    this.products.load();
  }

  countFor(sub: string): number {
    return this.products.all().filter((p) => p.category === sub).length;
  }

  selectAll(): void { this.parent.set(null); this.sub.set(null); }
  selectParent(name: string): void { this.parent.set(name); this.sub.set(null); }
  selectSub(s: string): void { this.parent.set(parentOf(s)); this.sub.set(s); }

  reset(): void {
    this.selectAll();
    this.maxPrice.set(null);
    this.inStockOnly.set(false);
    this.search.term.set('');
    this.sort.set('featured');
  }

  stars(id: string): string { return stars(ratingFor(id)); }
  reviews(id: string): string { return `(${reviewsFor(id)})`; }

  add(p: Product): void {
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/login']); return; }
    this.cart.add(p).subscribe(() => this.toast.show(`${p.name} added to cart`));
  }
}
