// Shapes returned by the backend APIs (only the fields the shop app uses).

export interface User {
  id: string;
  firstName: string;
}

export interface AuthResult {
  accessToken: string;
  user: User;
}

export interface Product {
  id: string;
  name: string;
  description: string;
  sku: string;
  price: number;
  category: string;
  imageUrl?: string | null;
  isActive: boolean;
  stock: number;
}

export interface CartItem {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Cart {
  id: string;
  items: CartItem[];
  totalAmount: number;
  itemCount: number;
}

// Backend paginated list envelope (the shop only reads the items).
export interface Paged<T> {
  items: T[];
}
