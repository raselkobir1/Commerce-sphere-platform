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
  category: string; // holds the sub-category name (see data/taxonomy.ts)
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

// Shipping address collected at checkout (frontend-only — the backend checkout takes no address).
export interface ShippingAddress {
  fullName: string;
  phone: string;
  line1: string;
  city: string;
  postcode: string;
}

// A placed order, kept in memory to render the confirmation page.
export interface PlacedOrder {
  reference: string;
  items: CartItem[];
  total: number;
  address: ShippingAddress;
  paymentMethod: 'COD';
  placedAt: Date;
}
