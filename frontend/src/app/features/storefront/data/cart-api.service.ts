import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/http/api.service';
import { AddCartItemRequest, Cart, UpdateCartItemRequest } from '../../../core/models/cart.models';

// Cart service endpoints.
@Injectable({ providedIn: 'root' })
export class CartApiService {
  private readonly api = inject(ApiService);

  create(userId: string): Observable<Cart> {
    return this.api.post<Cart>('/api/carts', { userId });
  }

  getByUser(userId: string): Observable<Cart> {
    return this.api.get<Cart>(`/api/carts/user/${userId}`);
  }

  getById(cartId: string): Observable<Cart> {
    return this.api.get<Cart>(`/api/carts/${cartId}`);
  }

  addItem(cartId: string, body: AddCartItemRequest): Observable<Cart> {
    return this.api.post<Cart>(`/api/carts/${cartId}/items`, body);
  }

  updateItem(cartId: string, body: UpdateCartItemRequest): Observable<Cart> {
    return this.api.put<Cart>(`/api/carts/${cartId}/items`, body);
  }

  removeItem(cartId: string, productId: string): Observable<Cart> {
    return this.api.delete<Cart>(`/api/carts/${cartId}/items/${productId}`);
  }

  checkout(cartId: string, userId: string): Observable<unknown> {
    return this.api.post<unknown>(`/api/carts/${cartId}/checkout`, { cartId, userId });
  }
}
