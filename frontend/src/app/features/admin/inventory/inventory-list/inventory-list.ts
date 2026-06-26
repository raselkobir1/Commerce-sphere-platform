import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { InventoryItem } from '../../../../core/models/inventory.models';
import { NotificationService } from '../../../../core/notifications/notification.service';
import { InventoryApiService } from '../inventory-api.service';

@Component({
  selector: 'app-inventory-list',
  imports: [MatTableModule, MatPaginatorModule, MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './inventory-list.html',
  styleUrl: './inventory-list.scss',
})
export class InventoryList implements OnInit {
  private readonly api = inject(InventoryApiService);
  private readonly notify = inject(NotificationService);

  readonly columns = ['sku', 'onHand', 'reserved', 'available', 'reorder', 'actions'];
  readonly items = signal<InventoryItem[]>([]);
  readonly total = signal(0);
  readonly pageSize = signal(20);
  readonly pageIndex = signal(0);
  readonly loading = signal(false);

  ngOnInit(): void {
    this.load();
  }

  onPage(e: PageEvent): void {
    this.pageIndex.set(e.pageIndex);
    this.pageSize.set(e.pageSize);
    this.load();
  }

  isLow(item: InventoryItem): boolean {
    return item.quantityAvailable <= item.reorderLevel;
  }

  receive(item: InventoryItem, raw: string): void {
    const qty = Number(raw);
    if (!Number.isFinite(qty) || qty <= 0) return;
    this.api.receiveStock(item.productId, qty).subscribe({
      next: () => {
        this.notify.success(`Received ${qty} units of ${item.sku}`);
        this.load();
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    this.api.list(this.pageIndex() + 1, this.pageSize()).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.total.set(result.totalRecords);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
