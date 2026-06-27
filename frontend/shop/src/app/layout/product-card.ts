import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BdtPipe } from '../core/bdt.pipe';
import { Product } from '../core/models';
import { ratingFor, reviewsFor, stars } from '../data/display';

// A single product tile, reused by the home-page category blocks and the filtered grid.
@Component({
  selector: 'app-product-card',
  imports: [RouterLink, BdtPipe],
  template: `
    <div class="card">
      <a class="thumb" [routerLink]="['/product', product().id]"
         [style.background-image]="product().imageUrl ? 'url(' + product().imageUrl + ')' : null">
        @if (product().stock === 0) { <span class="tag out">Out of stock</span> }
        @else if (product().stock <= 10) { <span class="tag low">Only {{ product().stock }} left</span> }
        @else { <span class="tag">{{ product().category }}</span> }
      </a>
      <div class="body">
        <a class="name" [routerLink]="['/product', product().id]">{{ product().name }}</a>
        <div class="stars">{{ starsText() }} <span>{{ reviewsText() }}</span></div>
        <div class="price">{{ product().price | bdt }}</div>
        <button class="btn btn-primary btn-sm" [disabled]="product().stock === 0" (click)="add.emit(product())">
          Add to cart
        </button>
      </div>
    </div>
  `,
})
export class ProductCard {
  product = input.required<Product>();
  add = output<Product>();

  starsText(): string { return stars(ratingFor(this.product().id)); }
  reviewsText(): string { return `(${reviewsFor(this.product().id)})`; }
}
