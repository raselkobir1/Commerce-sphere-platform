import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/http/api.service';
import { PagedResult } from '../../../core/models/api-response';
import { InventoryItem } from '../../../core/models/inventory.models';

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  private readonly api = inject(ApiService);

  list(pageNumber = 1, pageSize = 20): Observable<PagedResult<InventoryItem>> {
    return this.api.get<PagedResult<InventoryItem>>('/api/inventory', { pageNumber, pageSize });
  }

  getByProduct(productId: string): Observable<InventoryItem> {
    return this.api.get<InventoryItem>(`/api/inventory/product/${productId}`);
  }

  adjustStock(productId: string, quantity: number): Observable<InventoryItem> {
    return this.api.post<InventoryItem>('/api/inventory/adjust', { productId, quantity });
  }

  receiveStock(productId: string, quantity: number): Observable<InventoryItem> {
    return this.api.post<InventoryItem>('/api/inventory/receive', { productId, quantity });
  }
}
