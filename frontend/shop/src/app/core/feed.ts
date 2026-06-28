import { Injectable, computed, inject, signal } from '@angular/core';
import { Api } from './api';
import { Paged, Product } from './models';

// Filters that shape the catalogue feed query. All optional; empty means "everything".
export interface FeedQuery {
  category?: string; // single name or comma-separated (parent + children)
  search?: string;
  maxPrice?: number | null;
  inStockOnly?: boolean;
  sortBy?: string;
}

// Server-paged product feed that powers the storefront's infinite scroll. Unlike the cached
// `Products` service (used for cart images / related products), this never holds the whole
// catalogue — it fetches one page at a time and appends, so it scales to 100K+ products.
@Injectable({ providedIn: 'root' })
export class Feed {
  private api = inject(Api);
  private readonly pageSize = 24;

  private page = 0;
  private query: FeedQuery = {};

  items = signal<Product[]>([]);
  total = signal(0);
  loading = signal(false);

  hasMore = computed(() => this.items().length < this.total());

  // Apply a new filter set: clear what's loaded and fetch page 1.
  setQuery(query: FeedQuery): void {
    this.query = query;
    this.page = 0;
    this.items.set([]);
    this.total.set(0);
    this.loadMore();
  }

  // Fetch the next page and append. No-op while a request is in flight or all rows are loaded.
  loadMore(): void {
    if (this.loading()) return;
    if (this.page > 0 && !this.hasMore()) return;

    this.loading.set(true);
    const next = this.page + 1;

    this.api
      .get<Paged<Product>>('/api/products', {
        pageNumber: next,
        pageSize: this.pageSize,
        publishedOnly: true,
        category: this.query.category,
        searchTerm: this.query.search,
        maxPrice: this.query.maxPrice ?? undefined,
        inStockOnly: this.query.inStockOnly ? true : undefined,
        sortBy: this.query.sortBy,
      })
      .subscribe({
        next: (r) => {
          this.page = next;
          this.items.update((cur) => [...cur, ...r.items]);
          this.total.set(r.totalRecords);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
}
