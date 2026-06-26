import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { CartStore } from '../data/cart-store';

@Component({
  selector: 'app-checkout',
  imports: [CurrencyPipe, RouterLink, MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
})
export class Checkout implements OnInit {
  private readonly cartStore = inject(CartStore);
  private readonly router = inject(Router);

  readonly cart = this.cartStore.cart;
  readonly total = this.cartStore.total;
  readonly placing = signal(false);
  readonly placed = signal(false);

  ngOnInit(): void {
    this.cartStore.ensureCart().subscribe({ error: () => void 0 });
  }

  placeOrder(): void {
    if (this.placing()) return;
    this.placing.set(true);
    this.cartStore.checkout().subscribe({
      next: () => {
        this.placing.set(false);
        this.placed.set(true);
      },
      error: () => this.placing.set(false),
    });
  }

  backToShop(): void {
    void this.router.navigateByUrl('/shop');
  }
}
