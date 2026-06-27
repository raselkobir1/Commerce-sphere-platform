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
  stock: number;
}

export interface Category {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
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
