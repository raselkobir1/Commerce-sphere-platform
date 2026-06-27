import { Component, OnInit, computed, inject, signal } from '@angular/core';
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
      <div><h1>Products</h1><div class="sub">Tick products, then click Publish / Unpublish · new products start as drafts</div></div>
      <a class="btn btn-primary" routerLink="/products/new">+ New product</a>
    </div>

    <div class="toolbar">
      <div class="search">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m21 21-4.3-4.3"/></svg>
        <input class="input" placeholder="Search products…" [(ngModel)]="search" (keyup.enter)="runSearch()" />
      </div>
      <button class="btn" (click)="runSearch()">Search</button>
      <span style="flex:1"></span>
      <button class="btn btn-primary" [disabled]="toPublish().length === 0" (click)="apply(true)">
        Publish @if (toPublish().length) { ({{ toPublish().length }}) }
      </button>
      <button class="btn" [disabled]="toUnpublish().length === 0" (click)="apply(false)">
        Unpublish @if (toUnpublish().length) { ({{ toUnpublish().length }}) }
      </button>
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
              <tr>
                <th style="width:36px"><input type="checkbox" [checked]="allChecked()" (change)="checkAll($event)" /></th>
                <th>Product</th><th>Category</th><th>Price</th><th>Stock</th><th>Store status</th><th class="right">Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (p of products(); track p.id) {
                <tr>
                  <td><input type="checkbox" [checked]="checked().has(p.id)" (change)="toggleRow(p.id)" /></td>
                  <td><div class="cell-flex">
                    <span class="thumb" [style.background-image]="p.imageUrl ? 'url(' + p.imageUrl + ')' : null">{{ p.imageUrl ? '' : p.name.slice(0,1) }}</span>
                    <div><div class="cell-main">{{ p.name }}</div><div class="cell-sub">{{ p.sku }}</div></div>
                  </div></td>
                  <td><span class="chip">{{ p.category }}</span></td>
                  <td class="cell-main">{{ p.price | currency }}</td>
                  <td>{{ p.stock }}</td>
                  <td>
                    @if (p.isPublished) { <span class="badge on">Published</span> } @else { <span class="badge low">Draft</span> }
                    @if (!p.isActive) { <span class="badge off" style="margin-left:6px">Inactive</span> }
                  </td>
                  <td class="right"><div class="actions">
                    <a class="btn btn-sm" [routerLink]="['/products', p.id]">Edit</a>
                    <button class="btn btn-sm" (click)="toggleActive(p)">{{ p.isActive ? 'Deactivate' : 'Activate' }}</button>
                  </div></td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div class="pager">
          <span class="muted">{{ rangeStart() }}–{{ rangeEnd() }} of {{ total() }} products</span>
          <div class="actions">
            <select class="input" [ngModel]="pageSize()" (ngModelChange)="changePageSize($event)">
              <option [ngValue]="10">10 / page</option>
              <option [ngValue]="20">20 / page</option>
              <option [ngValue]="50">50 / page</option>
            </select>
            <button class="btn btn-sm" [disabled]="pageNumber() <= 1" (click)="goTo(pageNumber() - 1)">‹ Prev</button>
            <span style="align-self:center">Page {{ pageNumber() }} of {{ totalPages() }}</span>
            <button class="btn btn-sm" [disabled]="pageNumber() >= totalPages()" (click)="goTo(pageNumber() + 1)">Next ›</button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`.pager { display:flex; align-items:center; justify-content:space-between; gap:12px; padding:14px 20px; border-top:1px solid var(--line); flex-wrap:wrap; }`],
})
export class ProductsPage implements OnInit {
  private api = inject(Api);

  products = signal<Product[]>([]);
  loading = signal(false);
  search = '';

  // Pagination — default page 1, 20 per page.
  pageNumber = signal(1);
  pageSize = signal(20);
  total = signal(0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  rangeStart = computed(() => (this.total() === 0 ? 0 : (this.pageNumber() - 1) * this.pageSize() + 1));
  rangeEnd = computed(() => Math.min(this.pageNumber() * this.pageSize(), this.total()));

  // Local checkbox selection (no API call on toggle); seeded from each product's published state.
  checked = signal<Set<string>>(new Set());
  allChecked = computed(() => this.products().length > 0 && this.products().every((p) => this.checked().has(p.id)));
  toPublish = computed(() => this.products().filter((p) => this.checked().has(p.id) && !p.isPublished).map((p) => p.id));
  toUnpublish = computed(() => this.products().filter((p) => !this.checked().has(p.id) && p.isPublished).map((p) => p.id));

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.api.get<Paged<Product>>('/api/products', {
      pageNumber: this.pageNumber(), pageSize: this.pageSize(), searchTerm: this.search,
    }).subscribe({
      next: (r) => {
        this.products.set(r.items);
        this.total.set(r.totalRecords);
        this.checked.set(new Set(r.items.filter((p) => p.isPublished).map((p) => p.id)));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  runSearch(): void { this.pageNumber.set(1); this.load(); }

  goTo(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.pageNumber.set(page);
    this.load();
  }

  changePageSize(size: number): void {
    this.pageSize.set(+size);
    this.pageNumber.set(1);
    this.load();
  }

  toggleRow(id: string): void {
    const s = new Set(this.checked());
    s.has(id) ? s.delete(id) : s.add(id);
    this.checked.set(s);
  }

  checkAll(e: Event): void {
    this.checked.set((e.target as HTMLInputElement).checked ? new Set(this.products().map((p) => p.id)) : new Set());
  }

  apply(publish: boolean): void {
    const ids = publish ? this.toPublish() : this.toUnpublish();
    if (ids.length === 0) return;
    this.api.post('/api/products/publish', { productIds: ids, published: publish }).subscribe(() => this.load());
  }

  toggleActive(p: Product): void {
    this.api.patch(`/api/products/${p.id}/${p.isActive ? 'deactivate' : 'activate'}`).subscribe(() => this.load());
  }
}
