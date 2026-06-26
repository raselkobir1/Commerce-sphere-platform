import { CurrencyPipe } from '@angular/common';
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { Product } from '../../../core/models/product.models';
import { CartStore } from '../data/cart-store';
import { ProductApiService } from '../data/product-api.service';

@Component({
  selector: 'app-product-detail',
  imports: [CurrencyPipe, RouterLink, MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss',
})
export class ProductDetail implements OnInit {
  private readonly api = inject(ProductApiService);
  private readonly cart = inject(CartStore);

  @Input() id!: string;
  readonly product = signal<Product | null>(null);
  readonly loading = signal(false);
  readonly quantity = signal(1);

  ngOnInit(): void {
    this.loading.set(true);
    this.api.getById(this.id).subscribe({
      next: (p) => {
        this.product.set(p);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  setQuantity(value: number): void {
    this.quantity.set(Math.max(1, value));
  }

  addToCart(): void {
    const product = this.product();
    if (!product) return;
    this.cart.addToCart(product, this.quantity()).subscribe();
  }
}
