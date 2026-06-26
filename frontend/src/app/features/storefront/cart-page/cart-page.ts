import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { CartStore } from '../data/cart-store';

@Component({
  selector: 'app-cart-page',
  imports: [CurrencyPipe, RouterLink, MatButtonModule, MatIconModule],
  templateUrl: './cart-page.html',
  styleUrl: './cart-page.scss',
})
export class CartPage implements OnInit {
  private readonly cartStore = inject(CartStore);

  readonly cart = this.cartStore.cart;
  readonly total = this.cartStore.total;

  ngOnInit(): void {
    this.cartStore.ensureCart().subscribe({ error: () => void 0 });
  }

  changeQty(productId: string, quantity: number): void {
    if (quantity < 1) return;
    this.cartStore.updateQuantity(productId, quantity).subscribe();
  }

  remove(productId: string): void {
    this.cartStore.removeItem(productId).subscribe();
  }
}
