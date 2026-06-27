import { Component, OnInit, inject, signal } from '@angular/core';
import { Api } from '../../core/api';
import { Paged, Product } from '../../core/models';

// NOTE: The backend has no separate Category entity — a category is just a text field on a
// product. So this page lists the distinct categories currently in use (read-only). To get full
// category CRUD, the Product service would need a Category table + endpoints.
@Component({
  selector: 'app-categories',
  template: `
    <div class="page-head">
      <div><h1>Categories</h1><div class="sub">{{ rows().length }} categories in use</div></div>
    </div>

    <p class="muted" style="margin-bottom:16px">
      Categories come from the products themselves — set a product's category on its edit page.
    </p>

    <div class="card">
      @if (rows().length === 0) {
        <div class="empty">No categories yet.</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead><tr><th>Category</th><th class="right">Products</th></tr></thead>
            <tbody>
              @for (row of rows(); track row.name) {
                <tr>
                  <td><span class="chip admin">{{ row.name }}</span></td>
                  <td class="right cell-main">{{ row.count }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class CategoriesPage implements OnInit {
  private api = inject(Api);

  rows = signal<{ name: string; count: number }[]>([]);

  ngOnInit(): void {
    this.api.get<Paged<Product>>('/api/products', { pageNumber: 1, pageSize: 500 }).subscribe((r) => {
      const counts = new Map<string, number>();
      for (const p of r.items) {
        counts.set(p.category, (counts.get(p.category) ?? 0) + 1);
      }
      this.rows.set([...counts].map(([name, count]) => ({ name, count })).sort((a, b) => a.name.localeCompare(b.name)));
    });
  }
}
