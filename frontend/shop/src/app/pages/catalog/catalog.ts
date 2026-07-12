import {
  AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild,
  computed, effect, inject, signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { Feed, FeedQuery } from '../../core/feed';
import { Products } from '../../core/products';
import { Search } from '../../core/search';
import { Toast } from '../../core/toast';
import { Category, Product } from '../../core/models';
import { BannerCarousel } from '../../layout/banner-carousel';
import { ProductCard } from '../../layout/product-card';
import { I18n } from '../../core/i18n';
import { TranslatePipe } from '../../core/translate.pipe';
import { CatalogUi } from '../../core/catalog-ui';

@Component({
  selector: 'app-catalog',
  imports: [FormsModule, BannerCarousel, ProductCard, TranslatePipe],
  template: `
    <div class="container shop-layout" [class.filters-hidden]="!filtersOpen()" style="padding-top:18px">
      <!-- ───── Left dynamic category sidebar ───── -->
      @if (filtersOpen()) {
        <aside class="filters">
          <div class="group">
            <h3>{{ 'catalog.categories' | t }}</h3>
            <button class="cat-parent" [class.active]="!selected()" (click)="select(null)">
              <span>🛍️</span> {{ 'catalog.allProducts' | t }}
            </button>

            @for (node of tree(); track node.cat.id) {
              <button class="cat-parent" [class.active]="selected() === node.cat.name" (click)="select(node.cat.name)">
                <span>📂</span> {{ node.cat.name }}
              </button>
              @for (child of node.children; track child.cat.id) {
                <button class="cat-sub" [class.active]="selected() === child.cat.name" (click)="select(child.cat.name)">
                  {{ child.cat.name }}
                </button>
              }
            }
          </div>

          <div class="group">
            <h3>{{ 'catalog.maxPrice' | t }}</h3>
            <div class="price-row">
              <input class="input" type="number" min="0" [placeholder]="'catalog.any' | t" [ngModel]="maxPrice()"
                     (ngModelChange)="maxPrice.set($event ? +$event : null)" />
              <span class="muted">৳</span>
            </div>
          </div>

          <div class="group">
            <label class="check">
              <input type="checkbox" [ngModel]="inStockOnly()" (ngModelChange)="inStockOnly.set($event)" />
              {{ 'catalog.inStockOnly' | t }}
            </label>
          </div>

          <div class="group">
            <h3>{{ 'catalog.sort' | t }}</h3>
            <select class="input" [ngModel]="sort()" (ngModelChange)="sort.set($event)">
              <option value="featured">{{ 'catalog.sortFeatured' | t }}</option>
              <option value="price-asc">{{ 'catalog.sortPriceAsc' | t }}</option>
              <option value="price-desc">{{ 'catalog.sortPriceDesc' | t }}</option>
              <option value="name">{{ 'catalog.sortName' | t }}</option>
            </select>
          </div>

          <button class="btn btn-block btn-sm" (click)="reset()">{{ 'catalog.clearAllFilters' | t }}</button>
        </aside>
      }

      <!-- ───── Results: one flat, infinitely-scrolling grid ───── -->
      <main>
        <app-banner-carousel />

        <div class="results-bar">
          <div>
            <h1 style="font-size:22px;margin:0">{{ heading() }}</h1>
            <span class="muted">{{ i18n.t('catalog.productsCount', { n: feed.total() }) }}</span>
          </div>
        </div>

        @if (activeChips().length) {
          <div class="chips" style="margin-bottom:18px">
            @for (c of activeChips(); track c.key) {
              <span class="chip">{{ c.label }} <button (click)="c.clear()">×</button></span>
            }
          </div>
        }

        @if (feed.items().length) {
          <div class="grid">
            @for (p of feed.items(); track p.id) {
              <app-product-card [product]="p" (add)="add($event)" />
            }
          </div>
        } @else if (!feed.loading()) {
          <div class="empty">{{ 'catalog.noMatch' | t }}<br /><button class="btn-ghost" (click)="reset()">{{ 'catalog.clearFilters' | t }}</button></div>
        }

        <!-- Sentinel is always rendered so the IntersectionObserver can attach once. -->
        <div #sentinel class="feed-sentinel" aria-hidden="true"></div>

        @if (feed.loading()) {
          <p class="muted feed-status">{{ 'catalog.loadingMore' | t }}</p>
        } @else if (feed.items().length && !feed.hasMore()) {
          <p class="muted feed-status">{{ i18n.t('catalog.reachedEnd', { n: feed.total() }) }}</p>
        }
      </main>
    </div>
  `,
  styles: [
    `
      .feed-sentinel { height: 1px; width: 100%; }
      .feed-status { text-align: center; padding: 22px 0; }
    `,
  ],
})
export class CatalogPage implements OnInit, AfterViewInit, OnDestroy {
  feed = inject(Feed);
  products = inject(Products); // cached sample — powers the sidebar category tree only
  i18n = inject(I18n);
  private api = inject(Api);
  private auth = inject(Auth);
  private cart = inject(Cart);
  private search = inject(Search);
  private toast = inject(Toast);
  private router = inject(Router);

  private cats = signal<Category[]>([]);
  filtersOpen = inject(CatalogUi).filtersOpen;
  selected = signal<string | null>(null);
  maxPrice = signal<number | null>(null);
  inStockOnly = signal(false);
  sort = signal<'featured' | 'price-asc' | 'price-desc' | 'name'>('featured');

  @ViewChild('sentinel') private sentinel?: ElementRef<HTMLElement>;
  private observer?: IntersectionObserver;
  private sentinelVisible = false;
  private reloadTimer?: ReturnType<typeof setTimeout>;

  // 2-level tree of active categories for the sidebar (names only — counts don't scale to a
  // server-paged catalogue, so the grid header shows the real total instead).
  tree = computed(() => {
    const cats = this.cats().filter((c) => c.isActive);
    const order = (a: Category, b: Category) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name);
    const tops = cats.filter((c) => !c.parentId).sort(order);
    return tops.map((cat) => ({
      cat,
      children: cats.filter((c) => c.parentId === cat.id).sort(order).map((c) => ({ cat: c })),
    }));
  });

  heading = computed(() =>
    this.selected() ?? (this.search.term()
      ? this.i18n.t('catalog.resultsFor', { term: this.search.term() })
      : this.i18n.t('catalog.allProducts')));

  activeChips = computed(() => {
    const chips: { key: string; label: string; clear: () => void }[] = [];
    if (this.search.term()) chips.push({ key: 'q', label: `“${this.search.term()}”`, clear: () => this.search.term.set('') });
    if (this.selected()) chips.push({ key: 'c', label: this.selected()!, clear: () => this.selected.set(null) });
    if (this.maxPrice() != null) chips.push({ key: 'm', label: `≤ ৳${this.maxPrice()}`, clear: () => this.maxPrice.set(null) });
    if (this.inStockOnly()) chips.push({ key: 's', label: this.i18n.t('catalog.inStockChip'), clear: () => this.inStockOnly.set(false) });
    return chips;
  });

  constructor() {
    // Reload the feed whenever any filter / sort / search changes. Debounced so typing in the
    // search box (which updates on every keystroke) fires one request, not one per character.
    effect(() => {
      const query: FeedQuery = {
        category: this.categoryParam(),
        search: this.search.term().trim() || undefined,
        maxPrice: this.maxPrice(),
        inStockOnly: this.inStockOnly(),
        sortBy: this.sort(),
      };
      this.scheduleReload(query);
    });

    // Auto-fill: if the first page is short enough that the sentinel is still on screen, keep
    // loading until the viewport is covered or the catalogue is exhausted.
    effect(() => {
      const loading = this.feed.loading();
      if (!loading && this.sentinelVisible && this.feed.hasMore()) {
        queueMicrotask(() => this.maybeLoadMore());
      }
    });
  }

  ngOnInit(): void {
    this.products.load(); // for the sidebar tree
    this.api.get<Category[]>('/api/categories').subscribe((c) => this.cats.set(c));
  }

  ngAfterViewInit(): void {
    // rootMargin prefetches the next page ~one screen before the sentinel is actually reached.
    this.observer = new IntersectionObserver(
      (entries) => {
        this.sentinelVisible = entries[0]?.isIntersecting ?? false;
        if (this.sentinelVisible) this.maybeLoadMore();
      },
      { rootMargin: '600px 0px' },
    );
    if (this.sentinel) this.observer.observe(this.sentinel.nativeElement);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    if (this.reloadTimer) clearTimeout(this.reloadTimer);
  }

  private scheduleReload(query: FeedQuery): void {
    if (this.reloadTimer) clearTimeout(this.reloadTimer);
    this.reloadTimer = setTimeout(() => this.feed.setQuery(query), 250);
  }

  private maybeLoadMore(): void {
    if (!this.feed.hasMore() || this.feed.loading()) return;
    this.feed.loadMore();
  }

  // Comma-separated category names for the server (a parent selection includes its children).
  private categoryParam(): string | undefined {
    const sel = this.selected();
    if (!sel) return undefined;
    const cat = this.cats().find((c) => c.name === sel);
    if (!cat) return sel;
    const kids = this.cats().filter((c) => c.parentId === cat.id).map((c) => c.name);
    return [cat.name, ...kids].join(',');
  }

  select(name: string | null): void {
    this.selected.set(name);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  reset(): void {
    this.selected.set(null);
    this.maxPrice.set(null);
    this.inStockOnly.set(false);
    this.search.term.set('');
    this.sort.set('featured');
  }

  add(p: Product): void {
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/login']); return; }
    this.cart.add(p).subscribe(() => this.toast.show(this.i18n.t('product.addedToCart', { name: p.name })));
  }
}
