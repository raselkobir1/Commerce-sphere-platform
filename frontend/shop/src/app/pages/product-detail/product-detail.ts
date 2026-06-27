import { Component, OnInit, inject, input, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { Product } from '../../core/models';

@Component({
  selector: 'app-product-detail',
  imports: [CurrencyPipe, FormsModule, RouterLink],
  template: `
    <div class="container">
      @if (product(); as p) {
        <a routerLink="/" class="muted">← Back to shop</a>
        <div class="detail" style="margin-top:14px">
          <div class="thumb" [style.background-image]="p.imageUrl ? 'url(' + p.imageUrl + ')' : null">
            @if (!p.imageUrl) { <span>No image</span> }
          </div>
          <div>
            <span class="muted">{{ p.category }}</span>
            <h1 style="margin:6px 0 12px">{{ p.name }}</h1>
            <p class="price" style="font-size:26px; font-weight:700">{{ p.price | currency }}</p>
            <p style="margin:16px 0; line-height:1.6">{{ p.description }}</p>

            @if (p.stock > 0) {
              <div class="toolbar" style="align-items:center">
                <input class="qty" type="number" min="1" [max]="p.stock" [(ngModel)]="quantity" />
                <button class="btn btn-primary" (click)="addToCart(p)">Add to cart</button>
              </div>
              <p class="muted">{{ p.stock }} in stock</p>
            } @else {
              <p class="error">Out of stock</p>
            }

            @if (added()) {
              <p class="notice">Added to your cart.</p>
            }
          </div>
        </div>
      } @else {
        <p class="muted">Loading…</p>
      }
    </div>
  `,
})
export class ProductDetailPage implements OnInit {
  private api = inject(Api);
  private auth = inject(Auth);
  private cart = inject(Cart);
  private router = inject(Router);

  id = input.required<string>(); // bound from the :id route param

  product = signal<Product | null>(null);
  quantity = 1;
  added = signal(false);

  ngOnInit(): void {
    this.api.get<Product>(`/api/products/${this.id()}`).subscribe((p) => this.product.set(p));
  }

  addToCart(p: Product): void {
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.cart.add(p, Number(this.quantity)).subscribe(() => this.added.set(true));
  }
}
