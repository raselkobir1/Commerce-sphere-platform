import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { NotificationService } from '../../../../core/notifications/notification.service';
import { Product } from '../../../../core/models/product.models';
import { ProductApiService } from '../../../storefront/data/product-api.service';

@Component({
  selector: 'app-admin-product-list',
  imports: [
    CurrencyPipe,
    RouterLink,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
})
export class ProductList implements OnInit {
  private readonly api = inject(ProductApiService);
  private readonly notify = inject(NotificationService);

  readonly columns = ['name', 'sku', 'price', 'category', 'stock', 'status', 'actions'];
  readonly products = signal<Product[]>([]);
  readonly total = signal(0);
  readonly pageSize = signal(10);
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

  toggleActive(product: Product): void {
    const call = product.isActive ? this.api.deactivate(product.id) : this.api.activate(product.id);
    call.subscribe({
      next: () => {
        this.notify.success(`${product.name} ${product.isActive ? 'deactivated' : 'activated'}`);
        this.load();
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    this.api.list({ pageNumber: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe({
      next: (result) => {
        this.products.set(result.items);
        this.total.set(result.totalRecords);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
