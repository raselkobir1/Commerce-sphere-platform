import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Auth } from '../core/auth';

// The signed-in layout: icon sidebar + top bar + the current page.
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand"><span class="mark">A</span>Admin<span>Sphere</span></div>

        <div class="nav-label">Menu</div>
        <nav class="nav">
          <a routerLink="/dashboard" routerLinkActive="active">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="7" height="9" rx="1.5"/><rect x="14" y="3" width="7" height="5" rx="1.5"/><rect x="14" y="12" width="7" height="9" rx="1.5"/><rect x="3" y="16" width="7" height="5" rx="1.5"/></svg>
            Dashboard
          </a>
          <a routerLink="/products" routerLinkActive="active">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M20 7 12 3 4 7v10l8 4 8-4z"/><path d="m4 7 8 4 8-4M12 11v10"/></svg>
            Products
          </a>
          <a routerLink="/categories" routerLinkActive="active">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/></svg>
            Categories
          </a>
          <a routerLink="/inventory" routerLinkActive="active">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M21 8v13H3V8M1 3h22v5H1zM10 12h4"/></svg>
            Inventory
          </a>
          <a routerLink="/users" routerLinkActive="active">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87M16 3.13A4 4 0 0 1 16 11"/></svg>
            Users
          </a>
        </nav>

        <div class="sidebar-foot">
          <div class="user-card">
            <span class="avatar sm">{{ initials() }}</span>
            <div>
              <div class="nm">{{ auth.user()?.firstName }} {{ auth.user()?.lastName }}</div>
              <div class="rl">{{ auth.user()?.role }}</div>
            </div>
          </div>
        </div>
      </aside>

      <div class="main">
        <header class="topbar">
          <div class="greet">
            Welcome back, {{ auth.user()?.firstName }} 👋
            <small>Here's what's happening in your store</small>
          </div>
          <div class="top-actions">
            <a class="linkout" href="http://localhost:4300" target="_blank" rel="noopener">View store ↗</a>
            <span class="avatar">{{ initials() }}</span>
            <button class="btn btn-sm" (click)="logout()">Sign out</button>
          </div>
        </header>
        <main class="content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class Shell {
  auth = inject(Auth);
  private router = inject(Router);

  initials = computed(() => {
    const u = this.auth.user();
    return ((u?.firstName?.[0] ?? '') + (u?.lastName?.[0] ?? '')).toUpperCase() || 'A';
  });

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
