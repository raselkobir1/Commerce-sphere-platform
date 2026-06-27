// Shapes returned by the backend APIs (only the fields the admin app uses).

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  isActive: boolean;
  createdAt: string;
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

export interface InventoryItem {
  id: string;
  productId: string;
  sku: string;
  quantityOnHand: number;
  quantityReserved: number;
  quantityAvailable: number;
  reorderLevel: number;
}

// Backend paginated list envelope.
export interface Paged<T> {
  items: T[];
  totalRecords: number;
}
