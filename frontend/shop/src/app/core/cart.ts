import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, of, switchMap, tap } from 'rxjs';
import { Api } from './api';
import { Auth } from './auth';
import { Cart as CartModel, Product } from './models';

// Manages the shopping cart for the signed-in customer. A cart belongs to a user, so all of
// these actions assume the customer is logged in (the pages enforce that before calling them).
@Injectable({ providedIn: 'root' })
export class Cart {
  private api = inject(Api);
  private auth = inject(Auth);

  cart = signal<CartModel | null>(null);
  count = computed(() => this.cart()?.itemCount ?? 0);

  // Load the user's existing cart (called after login / on startup).
  load(): void {
    const userId = this.auth.user()?.id;
    if (!userId) return;
    this.api
      .get<CartModel>(`/api/carts/user/${userId}`)
      .pipe(catchError(() => of(null)))
      .subscribe((c) => this.cart.set(c));
  }

  clear(): void {
    this.cart.set(null);
  }

  add(product: Product, quantity = 1): Observable<CartModel> {
    return this.ensureCart().pipe(
      switchMap((c) =>
        this.api.post<CartModel>(`/api/carts/${c.id}/items`, {
          productId: product.id,
          sku: product.sku,
          productName: product.name,
          quantity,
          unitPrice: product.price,
        }),
      ),
      switchMap((c) => this.refresh(c.id)),
    );
  }

  updateQuantity(productId: string, quantity: number): Observable<CartModel | null> {
    const c = this.cart();
    if (!c) return of(null);
    return this.api
      .put<CartModel>(`/api/carts/${c.id}/items`, { productId, quantity })
      .pipe(switchMap(() => this.refresh(c.id)));
  }

  remove(productId: string): Observable<CartModel | null> {
    const c = this.cart();
    if (!c) return of(null);
    return this.api
      .delete<CartModel>(`/api/carts/${c.id}/items/${productId}`)
      .pipe(switchMap(() => this.refresh(c.id)));
  }

  checkout(): Observable<unknown> {
    const c = this.cart();
    const userId = this.auth.user()?.id;
    if (!c || !userId) return of(null);
    return this.api
      .post(`/api/carts/${c.id}/checkout`, { cartId: c.id, userId })
      .pipe(tap(() => this.cart.set(null)));
  }

  // Return the current cart, creating one if the user has none yet.
  private ensureCart(): Observable<CartModel> {
    const existing = this.cart();
    if (existing) return of(existing);

    const userId = this.auth.user()!.id;
    return this.api.get<CartModel>(`/api/carts/user/${userId}`).pipe(
      catchError(() => this.api.post<CartModel>('/api/carts', { userId })),
      tap((c) => this.cart.set(c)),
    );
  }

  private refresh(cartId: string): Observable<CartModel> {
    return this.api.get<CartModel>(`/api/carts/${cartId}`).pipe(tap((c) => this.cart.set(c)));
  }
}
