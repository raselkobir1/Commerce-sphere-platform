import { Injectable, computed, inject, signal } from '@angular/core';
import { Api } from './api';
import { Paged, Product } from './models';

// Loads the active catalogue once and caches it. The catalogue page filters/sorts in memory
// (the demo store is small), and other pages look products up by id (e.g. cart images).
@Injectable({ providedIn: 'root' })
export class Products {
  private api = inject(Api);

  all = signal<Product[]>([]);
  loading = signal(false);
  loaded = computed(() => this.all().length > 0);

  load(): void {
    if (this.loaded() || this.loading()) return;
    this.loading.set(true);
    this.api.get<Paged<Product>>('/api/products', { pageNumber: 1, pageSize: 200 }).subscribe({
      next: (r) => {
        this.all.set(r.items.filter((p) => p.isActive));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  byId(id: string): Product | undefined {
    return this.all().find((p) => p.id === id);
  }
}
