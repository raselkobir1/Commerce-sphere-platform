import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { InventoryItem, Paged } from '../../core/models';

@Component({
  selector: 'app-inventory',
  imports: [FormsModule],
  template: `
    <div class="page-head"><h1>Inventory</h1></div>

    <div class="card">
      @if (loading()) {
        <p class="muted">Loading…</p>
      } @else {
        <table>
          <thead>
            <tr><th>SKU</th><th>On hand</th><th>Reserved</th><th>Available</th><th>Status</th><th>Set new qty</th></tr>
          </thead>
          <tbody>
            @for (i of items(); track i.id) {
              <tr>
                <td>{{ i.sku }}</td>
                <td>{{ i.quantityOnHand }}</td>
                <td>{{ i.quantityReserved }}</td>
                <td>{{ i.quantityAvailable }}</td>
                <td>
                  @if (i.quantityAvailable <= i.reorderLevel) {
                    <span class="badge low">Low</span>
                  } @else {
                    <span class="badge on">OK</span>
                  }
                </td>
                <td style="white-space:nowrap">
                  <input class="input" type="number" min="0" style="width:90px; display:inline-block"
                         [(ngModel)]="draft[i.id]" [name]="'q' + i.id" />
                  <button class="btn btn-sm" (click)="adjust(i)">Update</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `,
})
export class InventoryPage implements OnInit {
  private api = inject(Api);

  items = signal<InventoryItem[]>([]);
  loading = signal(false);
  draft: Record<string, number> = {};

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
