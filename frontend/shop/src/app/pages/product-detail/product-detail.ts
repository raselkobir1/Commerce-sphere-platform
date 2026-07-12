import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { Auth } from '../../core/auth';
import { Cart } from '../../core/cart';
import { Products } from '../../core/products';
import { Toast } from '../../core/toast';
import { Product } from '../../core/models';
import { BdtPipe } from '../../core/bdt.pipe';
import { ratingFor, reviewsFor, stars } from '../../data/display';
import { I18n } from '../../core/i18n';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-product-detail',
  imports: [BdtPipe, FormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="container">
      @if (product(); as p) {
        <nav class="crumbs">
          <a routerLink="/">{{ 'common.home' | t }}</a> ›
          <a routerLink="/">{{ p.category }}</a> ›
          <span>{{ p.name }}</span>
        </nav>

        <div class="detail">
          <div class="gallery">
            <div class="main" [style.background-image]="p.imageUrl ? 'url(' + p.imageUrl + ')' : null">
              @if (!p.imageUrl) { <span>{{ 'product.noImage' | t }}</span> }
            </div>
          </div>

          <div>
            <span class="muted">{{ p.category }}</span>
            <h1>{{ p.name }}</h1>
            <div class="stars" style="margin-bottom:10px">{{ stars(p.id) }}
              <span>{{ rating(p.id) }} · {{ i18n.t('product.reviews', { n: reviews(p.id) }) }}</span>
            </div>
            <div class="price-lg">{{ p.price | bdt }}</div>

            <div style="margin:12px 0">
              @if (p.stock > 0) {
                <span class="pill in">{{ i18n.t('product.inStockAvailable', { n: p.stock }) }}</span>
              } @else {
                <span class="pill out">{{ 'product.outOfStockDot' | t }}</span>
              }
            </div>

            <p class="desc">{{ p.description }}</p>

            <div class="spec"><span class="k">{{ 'product.sku' | t }}</span><span>{{ p.sku }}</span></div>
            <div class="spec"><span class="k">{{ 'product.category' | t }}</span><span>{{ p.category }}</span></div>

            <div class="buy-row">
              <div class="qty">
                <button type="button" (click)="dec()">−</button>
                <input type="number" min="1" [max]="p.stock" [ngModel]="qty()" (ngModelChange)="setQty($event, p)" />
                <button type="button" (click)="inc(p)">+</button>
              </div>
              <button class="btn btn-primary" [disabled]="p.stock === 0" (click)="add(p, false)">{{ 'product.addToCart' | t }}</button>
              <button class="btn" [disabled]="p.stock === 0" (click)="add(p, true)">{{ 'product.buyNow' | t }}</button>
            </div>
          </div>
        </div>

        @if (related().length) {
          <h2 style="margin-top:46px">{{ i18n.t('product.moreIn', { category: p.category }) }}</h2>
          <div class="grid">
            @for (r of related(); track r.id) {
              <div class="card">
                <a class="thumb" [routerLink]="['/product', r.id]"
                   [style.background-image]="r.imageUrl ? 'url(' + r.imageUrl + ')' : null"></a>
                <div class="body">
                  <a class="name" [routerLink]="['/product', r.id]">{{ r.name }}</a>
                  <div class="price">{{ r.price | bdt }}</div>
                </div>
              </div>
            }
          </div>
        }
      } @else {
        <p class="muted">{{ 'common.loading' | t }}</p>
      }
    </div>
  `,
})
export class ProductDetailPage implements OnInit {
  private api = inject(Api);
  private auth = inject(Auth);
  private cart = inject(Cart);
  private products = inject(Products);
  private toast = inject(Toast);
  private router = inject(Router);
  i18n = inject(I18n);

  id = input.required<string>();

  product = signal<Product | null>(null);
  qty = signal(1);

  related = computed(() => {
    const p = this.product();
    if (!p) return [];
    return this.products.all().filter((x) => x.category === p.category && x.id !== p.id).slice(0, 4);
  });

  ngOnInit(): void {
    this.products.load();
    this.load();
  }

  private load(): void {
    this.qty.set(1);
    this.api.get<Product>(`/api/products/${this.id()}`).subscribe((p) => this.product.set(p));
  }

  stars(id: string): string { return stars(ratingFor(id)); }
  rating(id: string): number { return ratingFor(id); }
  reviews(id: string): number { return reviewsFor(id); }

  inc(p: Product): void { if (this.qty() < p.stock) this.qty.update((q) => q + 1); }
  dec(): void { if (this.qty() > 1) this.qty.update((q) => q - 1); }
  setQty(v: number, p: Product): void { this.qty.set(Math.min(Math.max(1, +v || 1), p.stock)); }

  add(p: Product, buyNow: boolean): void {
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/login']); return; }
    this.cart.add(p, this.qty()).subscribe(() => {
      this.toast.show(this.i18n.t('product.addedToCart', { name: p.name }));
      if (buyNow) this.router.navigate(['/cart']);
    });
  }
}
