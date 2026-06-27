import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { InventoryItem, Paged } from '../../core/models';

@Component({
  selector: 'app-inventory',
  imports: [FormsModule],
  template: `
    <div class="page-head"><div><h1>Inventory</h1><div class="sub">Stock levels across {{ items().length }} SKUs</div></div></div>

    <div class="card">
      @if (loading()) {
        <div class="empty">Loading…</div>
      } @else if (items().length === 0) {
        <div class="empty">No inventory records yet.</div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr><th>SKU</th><th>On hand</th><th>Reserved</th><th>Available</th><th>Status</th><th class="right">Set new qty</th></tr>
            </thead>
            <tbody>
              @for (i of items(); track i.id) {
                <tr>
                  <td class="cell-main">{{ i.sku }}</td>
                  <td>
                    {{ i.quantityOnHand }}
                    <span class="stockbar"><i [class.low]="i.quantityAvailable <= i.reorderLevel" [style.width.%]="pct(i)"></i></span>
                  </td>
                  <td>{{ i.quantityReserved }}</td>
                  <td class="cell-main">{{ i.quantityAvailable }}</td>
                  <td>
                    @if (i.quantityAvailable <= i.reorderLevel) {
                      <span class="badge low">Low</span>
                    } @else {
                      <span class="badge on">OK</span>
                    }
                  </td>
                  <td class="right">
                    <div class="actions">
                      <input class="input" type="number" min="0" style="width:84px"
                             [(ngModel)]="draft[i.id]" [name]="'q' + i.id" />
                      <button class="btn btn-sm btn-primary" (click)="adjust(i)">Update</button>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class InventoryPage implements OnInit {
  private api = inject(Api);

  items = signal<InventoryItem[]>([]);
  loading = signal(false);
  draft: Record<string, number> = {};

  // Fill ratio of the stock bar (available vs on-hand).
  pct(i: InventoryItem): number {
    return i.quantityOnHand > 0 ? Math.min(100, Math.round((i.quantityAvailable / i.quantityOnHand) * 100)) : 0;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.get<Paged<InventoryItem>>('/api/inventory', { pageNumber: 1, pageSize: 200 }).subscribe({
      next: (r) => {
        this.items.set(r.items);
        for (const i of r.items) this.draft[i.id] = i.quantityOnHand;
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  adjust(i: InventoryItem): void {
    this.api
      .post('/api/inventory/adjust', { productId: i.productId, sku: i.sku, newQuantity: Number(this.draft[i.id]) })
      .subscribe(() => this.load());
  }
}
