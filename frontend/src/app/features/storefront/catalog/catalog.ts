import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Product } from '../../../core/models/product.models';
import { CartStore } from '../data/cart-store';
import { ProductApiService } from '../data/product-api.service';

@Component({
  selector: 'app-catalog',
  imports: [
    CurrencyPipe,
    RouterLink,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
  ],
  templateUrl: './catalog.html',
  styleUrl: './catalog.scss',
})
export class Catalog implements OnInit {
  private readonly api = inject(ProductApiService);
  private readonly cart = inject(CartStore);

  readonly products = signal<Product[]>([]);
  readonly total = signal(0);
  readonly pageSize = signal(12);
  readonly pageIndex = signal(0);
  readonly loading = signal(false);
  readonly search = new FormControl('', { nonNullable: true });

  ngOnInit(): void {
    this.load();
    this.search.valueChanges.pipe(debounceTime(350), distinctUntilChanged()).subscribe(() => {
      this.pageIndex.set(0);
      this.load();
    });
  }

  onPage(e: PageEvent): void {
    this.pageIndex.set(e.pageIndex);
    this.pageSize.set(e.pageSize);
    this.load();
  }

  addToCart(product: Product): void {
    this.cart.addToCart(product, 1).subscribe();
  }

  private load(): void {
    this.loading.set(true);
    this.api
      .list({ pageNumber: this.pageIndex() + 1, pageSize: this.pageSize(), searchTerm: this.search.value })
      .subscribe({
        next: (result) => {
          this.products.set(result.items);
          this.total.set(result.totalRecords);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
}
