import { UserRole } from '../models/auth.models';

// Single source of truth for where each role lands after login / when bounced from a denied route.
export function homePathForRole(role: UserRole | null): string {
  return role === 'Admin' ? '/admin' : '/shop';
}
