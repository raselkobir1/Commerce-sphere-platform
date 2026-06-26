import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/http/api.service';
import { PagedResult } from '../../../core/models/api-response';
import { CreateProductRequest, Product, UpdateProductRequest } from '../../../core/models/product.models';

// Product service endpoints (browse for customers, CRUD for admins).
@Injectable({ providedIn: 'root' })
export class ProductApiService {
  private readonly api = inject(ApiService);

  list(opts: { pageNumber?: number; pageSize?: number; category?: string; searchTerm?: string } = {}): Observable<PagedResult<Product>> {
    return this.api.get<PagedResult<Product>>('/api/products', {
      pageNumber: opts.pageNumber ?? 1,
      pageSize: opts.pageSize ?? 12,
      category: opts.category,
      searchTerm: opts.searchTerm,
    });
  }

  getById(id: string): Observable<Product> {
    return this.api.get<Product>(`/api/products/${id}`);
  }

  create(body: CreateProductRequest): Observable<Product> {
    return this.api.post<Product>('/api/products', body);
  }

  update(id: string, body: UpdateProductRequest): Observable<Product> {
    return this.api.put<Product>(`/api/products/${id}`, body);
  }

  activate(id: string): Observable<void> {
    return this.api.patch<void>(`/api/products/${id}/activate`);
  }

  deactivate(id: string): Observable<void> {
    return this.api.patch<void>(`/api/products/${id}/deactivate`);
  }
}
