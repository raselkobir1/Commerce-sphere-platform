// Auth contracts — mirror the Auth service DTOs (camelCase over the wire).

export type UserRole = 'Admin' | 'Customer';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  isActive: boolean;
  emailVerified: boolean;
  isActiveTwoFactor: boolean;
  twoFactorConfirmed: boolean;
  isOtpAuthEnable: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

// Login can resolve to tokens OR a step-up challenge. The controller returns one of three shapes
// under `data`; this discriminated union models all of them.
export type LoginOutcome =
  | { kind: 'tokens'; tokens: AuthTokens }
  | { kind: 'twoFactor'; challengeToken: string }
  | { kind: 'otp'; challengeToken: string };

export interface TwoFactorSetup {
  secretKey: string;
  qrCodeUri: string;
  manualEntrySegments: string[];
}

export interface Session {
  id: string;
  createdByIp: string;
  createdAt: string;
  expiresAt: string;
  isActive: boolean;
}

// ── Request payloads ────────────────────────────────────────────────────────
export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role?: UserRole;
}
export interface LoginRequest { email: string; password: string; }
export interface UpdateProfileRequest { firstName: string; lastName: string; }
export interface ChangePasswordRequest { currentPassword: string; newPassword: string; }
export interface ForgotPasswordRequest { email: string; }
export interface ResetPasswordRequest { token: string; newPassword: string; }
export interface ChallengeVerifyRequest { challengeToken: string; code: string; }
