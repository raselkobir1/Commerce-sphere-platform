import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, of, switchMap, tap, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/notifications/notification.service';
import { Cart } from '../../../core/models/cart.models';
import { Product } from '../../../core/models/product.models';
import { CartApiService } from './cart-api.service';

// Holds the signed-in user's active cart as a signal and exposes high-level cart operations.
// The navbar badge reads `itemCount()`; pages read `cart()`.
@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly cartApi = inject(CartApiService);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotificationService);

  private readonly _cart = signal<Cart | null>(null);
  readonly cart = this._cart.asReadonly();
  readonly itemCount = computed(() => this._cart()?.itemCount ?? 0);
  readonly total = computed(() => this._cart()?.totalAmount ?? 0);

  private get userId(): string | null {
    return this.auth.user()?.id ?? null;
  }

  // Loads the user's active cart, creating one if none exists yet.
  ensureCart(): Observable<Cart | null> {
    const userId = this.userId;
    if (!userId) return of(null);
    if (this._cart()) return of(this._cart());

    return this.cartApi.getByUser(userId).pipe(
      catchError((err: HttpErrorResponse) => (err.status === 404 ? this.cartApi.create(userId) : throwError(() => err))),
      tap((cart) => this._cart.set(cart)),
    );
  }

  refresh(): Observable<Cart | null> {
    this._cart.set(null);
    return this.ensureCart();
  }

  addToCart(product: Product, quantity = 1): Observable<Cart | null> {
    return this.ensureCart().pipe(
      switchMap((cart) => {
        if (!cart) return of(null);
        return this.cartApi
          .addItem(cart.id, {
            productId: product.id,
            sku: product.sku,
            productName: product.name,
            quantity,
            unitPrice: product.price,
          })
          .pipe(
            tap((updated) => {
              this._cart.set(updated);
              this.notify.success(`${product.name} added to cart`);
            }),
          );
      }),
    );
  }

  updateQuantity(productId: string, quantity: number): Observable<Cart | null> {
    const cart = this._cart();
    if (!cart) return of(null);
    return this.cartApi.updateItem(cart.id, { productId, quantity }).pipe(tap((updated) => this._cart.set(updated)));
  }

  removeItem(productId: string): Observable<Cart | null> {
    const cart = this._cart();
    if (!cart) return of(null);
    return this.cartApi.removeItem(cart.id, productId).pipe(tap((updated) => this._cart.set(updated)));
  }

  checkout(): Observable<unknown> {
    const cart = this._cart();
    const userId = this.userId;
    if (!cart || !userId) return of(null);
    return this.cartApi.checkout(cart.id, userId).pipe(
      tap(() => {
        this._cart.set(null);
        this.notify.success('Order placed! Your checkout is being processed.');
      }),
    );
  }

  clearLocal(): void {
    this._cart.set(null);
  }
}
