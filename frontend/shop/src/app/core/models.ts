// Shapes returned by the backend APIs (only the fields the shop app uses).

export interface User {
  id: string;
  firstName: string;
  lastName?: string;
  email?: string;
  emailVerified?: boolean;
  createdAt?: string;
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
  category: string; // the category name (managed via the admin Categories page)
  imageUrl?: string | null;
  isActive: boolean;
  stock: number;
}

// Storefront categories (managed in admin), with optional parent for a 2-level tree.
export interface Category {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  parentId?: string | null;
  sortOrder: number;
}

// Promotional banner shown in the home-page carousel (managed in admin).
export interface Banner {
  id: string;
  title: string;
  subtitle: string;
  imageUrl: string;
  linkUrl: string;
  isActive: boolean;
  sortOrder: number;
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

// One step in an order's status timeline (for tracking).
export interface OrderStatusEntry {
  status: string;
  note?: string | null;
  createdAt: string;
}

// A past order (checked-out / cancelled cart) for the customer's history.
export interface Order {
  id: string;
  status: string;
  items: CartItem[];
  totalAmount: number;
  itemCount: number;
  createdAt: string;
  updatedAt?: string | null;
  statusHistory?: OrderStatusEntry[];
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
