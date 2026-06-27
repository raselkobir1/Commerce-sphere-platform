import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { Api } from './api';
import { MenuPermission } from './models';

// Holds the signed-in user's accessible menus + CRUD flags. Drives the dynamic sidebar, route
// guards, and action-button visibility.
@Injectable({ providedIn: 'root' })
export class Perms {
  private api = inject(Api);

  menus = signal<MenuPermission[]>([]);
  loaded = signal(false);

  load(): Observable<MenuPermission[]> {
    return this.api
      .get<MenuPermission[]>('/api/auth/me/permissions')
      .pipe(tap((m) => { this.menus.set(m); this.loaded.set(true); }));
  }

  clear(): void {
    this.menus.set([]);
    this.loaded.set(false);
  }

  // Can the user see this menu at all?
  canView(menuKey: string): boolean {
    return this.menus().some((m) => m.menuKey === menuKey && m.canView);
  }

  // Can the user perform a specific action on this menu?
  can(menuKey: string, action: 'create' | 'edit' | 'delete'): boolean {
    const m = this.menus().find((x) => x.menuKey === menuKey);
    if (!m) return false;
    return action === 'create' ? m.canCreate : action === 'edit' ? m.canEdit : m.canDelete;
  }
}
