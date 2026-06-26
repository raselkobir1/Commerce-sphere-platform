// Mirrors the Cart service DTOs.
export interface CartItem {
  id: string;
  productId: string;
  sku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  addedAt: string;
}

export interface Cart {
  id: string;
  userId: string;
  status: string;
  items: CartItem[];
  totalAmount: number;
  itemCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface AddCartItemRequest {
  productId: string;
  sku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface UpdateCartItemRequest {
  productId: string;
  quantity: number;
}
