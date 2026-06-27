// Shapes returned by the backend APIs (only the fields the admin app uses).

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  // Security flags (from /api/auth/me) — used by the Settings page.
  emailVerified?: boolean;
  isActiveTwoFactor?: boolean;
  twoFactorConfirmed?: boolean;
  isOtpAuthEnable?: boolean;
}

export interface AuthResult {
  accessToken: string;
  user: User;
}

// TOTP enrollment details returned by /api/auth/2fa/setup.
export interface TwoFactorSetup {
  secretKey: string;
  qrCodeUri: string;
  manualEntrySegments: string[];
}

// A login session (refresh token) from /api/auth/sessions.
export interface Session {
  id: string;
  createdByIp: string;
  createdAt: string;
  expiresAt: string;
  isActive: boolean;
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
  isPublished: boolean;
  stock: number;
}

export interface Role {
  id: string;
  name: string;
  description: string;
  isSystem: boolean;
  isDefault: boolean;
  createdAt: string;
}

export interface Menu {
  id: string;
  key: string;
  label: string;
  route: string;
  icon: string;
  sortOrder: number;
  parentId?: string | null;
}

// A menu + the signed-in role's CRUD flags for it (from /api/auth/me/permissions and the matrix).
export interface MenuPermission {
  menuId: string;
  menuKey: string;
  label: string;
  route: string;
  icon: string;
  sortOrder: number;
  parentId?: string | null;
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

export interface Category {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  parentId?: string | null;
  sortOrder: number;
}

export interface Banner {
  id: string;
  title: string;
  subtitle: string;
  imageUrl: string;
  linkUrl: string;
  isActive: boolean;
  sortOrder: number;
  createdAt: string;
  updatedAt?: string | null;
}

export interface OrderItem {
  id: string;
  productId: string;
  sku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Order {
  id: string;
  userId: string;
  status: string;
  items: OrderItem[];
  totalAmount: number;
  itemCount: number;
  createdAt: string;
  updatedAt?: string | null;
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
