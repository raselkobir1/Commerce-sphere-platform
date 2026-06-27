import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { Products } from '../../core/products';
import { Search } from '../../core/search';
import { Toast } from '../../core/toast';
import { Category, Product } from '../../core/models';
import { BannerCarousel } from '../../layout/banner-carousel';
import { ProductCard } from '../../layout/product-card';

@Component({
  selector: 'app-catalog',
  imports: [FormsModule, BannerCarousel, ProductCard],
  template: `
    <div class="container" style="padding-top:18px">
      <app-banner-carousel />
    </div>

    <div class="container shop-layout">
      <!-- ───── Left dynamic category sidebar ───── -->
      <aside class="filters">
        <div class="group">
          <h3>Categories</h3>
          <button class="cat-parent" [class.active]="!selected()" (click)="select(null)">
            <span>🛍️</span> All products <span class="muted">({{ totalCount() }})</span>
          </button>

          @for (node of tree(); track node.cat.id) {
            <button class="cat-parent" [class.active]="selected() === node.cat.name" (click)="select(node.cat.name)">
              <span>📂</span> {{ node.cat.name }} <span class="muted">({{ node.count }})</span>
            </button>
            @for (child of node.children; track child.cat.id) {
              <button class="cat-sub" [class.active]="selected() === child.cat.name" (click)="select(child.cat.name)">
                {{ child.cat.name }} <span class="muted">({{ child.count }})</span>
              </button>
            }
          }
        </div>

        <div class="group">
          <h3>Max price</h3>
          <div class="price-row">
            <input class="input" type="number" min="0" placeholder="Any" [ngModel]="maxPrice()"
                   (ngModelChange)="maxPrice.set($event ? +$event : null)" />
            <span class="muted">৳</span>
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
        @if (products.loading()) {
          <p class="muted">Loading products…</p>
        } @else if (hasFilter()) {
          <!-- Filtered / searched view: one flat, sortable grid -->
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

          @if (filtered().length === 0) {
            <div class="empty">No products match your filters.<br /><button class="btn-ghost" (click)="reset()">Clear filters</button></div>
          } @else {
            <div class="grid">
              @for (p of filtered(); track p.id) {
                <app-product-card [product]="p" (add)="add($event)" />
              }
            </div>
          }
        } @else {
          <!-- Default home view: a block per category -->
          @if (byCategory().length === 0) {
            <div class="empty">No products available yet.</div>
          } @else {
            @for (block of byCategory(); track block.name) {
              <section class="cat-block">
                <div class="cat-block-head">
                  <h2>{{ block.name }} <span class="muted">({{ block.products.length }})</span></h2>
                  @if (block.products.length > previewLimit) {
                    <button class="link-btn" (click)="select(block.name)">View all →</button>
                  }
                </div>
                <div class="grid">
                  @for (p of block.products.slice(0, previewLimit); track p.id) {
                    <app-product-card [product]="p" (add)="add($event)" />
                  }
                </div>
              </section>
            }
          }
        }
      </main>
    </div>
  `,
})
export class CatalogPage implements OnInit {
  products = inject(Products);
  private api = inject(Api);
  private auth = inject(Auth);
  private cart = inject(Cart);
  private search = inject(Search);
  private toast = inject(Toast);
  private router = inject(Router);

  private cats = signal<Category[]>([]);
  selected = signal<string | null>(null); // selected category name (parent or child)
  maxPrice = signal<number | null>(null);
  inStockOnly = signal(false);
  sort = signal<'featured' | 'price-asc' | 'price-desc' | 'name'>('featured');

  previewLimit = 5; // products shown per category block before "View all"

  totalCount = computed(() => this.products.all().length);

  // True when the shopper has narrowed the catalogue (category, search, price, or stock).
  // When false the home page shows the category-wise blocks instead of one flat grid.
  hasFilter = computed(() =>
    !!this.selected() || !!this.search.term().trim() || this.maxPrice() != null || this.inStockOnly());

  // Default home layout: one block per active category (parents then their children, in tree
  // order), each holding the products whose category name matches. Unmatched products go last.
  byCategory = computed(() => {
    const cats = this.cats().filter((c) => c.isActive);
    const order = (a: Category, b: Category) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name);
    const all = this.products.all();
    const blocks: { name: string; products: Product[] }[] = [];

    for (const top of cats.filter((c) => !c.parentId).sort(order)) {
      const own = all.filter((p) => p.category === top.name);
      if (own.length) blocks.push({ name: top.name, products: own });
      for (const kid of cats.filter((c) => c.parentId === top.id).sort(order)) {
        const kp = all.filter((p) => p.category === kid.name);
        if (kp.length) blocks.push({ name: kid.name, products: kp });
      }
    }

    const known = new Set(cats.map((c) => c.name));
    const other = all.filter((p) => !known.has(p.category));
    if (other.length) blocks.push({ name: 'Other', products: other });
    return blocks;
  });

  // 2-level tree of active categories with product counts.
  tree = computed(() => {
    const cats = this.cats().filter((c) => c.isActive);
    const order = (a: Category, b: Category) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name);
    const tops = cats.filter((c) => !c.parentId).sort(order);
    return tops.map((cat) => {
      const children = cats.filter((c) => c.parentId === cat.id).sort(order).map((c) => ({ cat: c, count: this.countOf(c) }));
      const own = this.products.all().filter((p) => p.category === cat.name).length;
      const count = own + children.reduce((s, c) => s + c.count, 0);
      return { cat, count, children };
    });
  });

  // The list after category + search + price + stock filters and sorting.
  filtered = computed(() => {
    const term = this.search.term().trim().toLowerCase();
    const allowed = this.allowedNames();
    const max = this.maxPrice();

    let list = this.products.all().filter((p) => {
      if (term && !(p.name + ' ' + p.description + ' ' + p.category).toLowerCase().includes(term)) return false;
      if (allowed && !allowed.includes(p.category)) return false;
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

  heading = computed(() => this.selected() ?? (this.search.term() ? `Results for “${this.search.term()}”` : 'All products'));

  activeChips = computed(() => {
    const chips: { key: string; label: string; clear: () => void }[] = [];
    if (this.search.term()) chips.push({ key: 'q', label: `“${this.search.term()}”`, clear: () => this.search.term.set('') });
    if (this.selected()) chips.push({ key: 'c', label: this.selected()!, clear: () => this.selected.set(null) });
    if (this.maxPrice() != null) chips.push({ key: 'm', label: `≤ ৳${this.maxPrice()}`, clear: () => this.maxPrice.set(null) });
    return chips;
  });

  ngOnInit(): void {
    this.products.load();
    this.api.get<Category[]>('/api/categories').subscribe((c) => this.cats.set(c));
  }

  private countOf(c: Category): number {
    return this.products.all().filter((p) => p.category === c.name).length;
  }

  // Category names a product may match for the current selection (a parent includes its children).
  private allowedNames(): string[] | null {
    const sel = this.selected();
    if (!sel) return null;
    const cat = this.cats().find((c) => c.name === sel);
    if (!cat) return [sel];
    const kids = this.cats().filter((c) => c.parentId === cat.id).map((c) => c.name);
    return [cat.name, ...kids];
  }

  select(name: string | null): void { this.selected.set(name); }

  reset(): void {
    this.selected.set(null);
    this.maxPrice.set(null);
    this.inStockOnly.set(false);
    this.search.term.set('');
    this.sort.set('featured');
  }

  add(p: Product): void {
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/login']); return; }
    this.cart.add(p).subscribe(() => this.toast.show(`${p.name} added to cart`));
  }
}
