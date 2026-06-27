import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Auth } from '../core/auth';
import { Theme } from '../core/theme';

// The signed-in layout: icon sidebar + top bar (with the user/appearance menu) + the page.
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

        <div class="nav-label">Account</div>
        <nav class="nav">
          <a routerLink="/settings" routerLinkActive="active">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.9.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.9 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.9l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.9.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.9-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.9V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z"/></svg>
            Settings
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
            <div class="menu-wrap">
              <button class="menu-trigger" (click)="menuOpen.set(!menuOpen())">
                <span class="avatar sm">{{ initials() }}</span>
                <span class="nm">{{ auth.user()?.firstName }}</span>
                <span class="chev">▾</span>
              </button>

              @if (menuOpen()) {
                <div class="backdrop" (click)="menuOpen.set(false)"></div>
                <div class="menu">
                  <div class="menu-head">
                    <span class="avatar">{{ initials() }}</span>
                    <div>
                      <div class="nm">{{ auth.user()?.firstName }} {{ auth.user()?.lastName }}</div>
                      <div class="em">{{ auth.user()?.email }}</div>
                    </div>
                  </div>

                  <div class="menu-sec">
                    <div class="lbl">Theme</div>
                    <div class="seg">
                      <button [class.on]="theme.mode() === 'light'" (click)="theme.setMode('light')">☀️ Light</button>
                      <button [class.on]="theme.mode() === 'dark'" (click)="theme.setMode('dark')">🌙 Dark</button>
                    </div>
                    <div class="swatches">
                      @for (a of theme.accents; track a) {
                        <span class="swatch" [class.on]="theme.accent() === a"
                              [style.background]="theme.accentColor(a)" (click)="theme.setAccent(a)"></span>
                      }
                    </div>
                  </div>

                  <a class="menu-item" routerLink="/settings" (click)="menuOpen.set(false)">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="3"/><path d="M19 12a7 7 0 0 0-.1-1l2-1.6-2-3.4-2.4 1a7 7 0 0 0-1.7-1L14.5 2h-4l-.4 2.4a7 7 0 0 0-1.7 1l-2.4-1-2 3.4L4 11a7 7 0 0 0 0 2l-2 1.6 2 3.4 2.4-1a7 7 0 0 0 1.7 1l.4 2.4h4l.4-2.4a7 7 0 0 0 1.7-1l2.4 1 2-3.4-2-1.6c.1-.3.1-.7.1-1z"/></svg>
                    Account settings
                  </a>
                  <a class="menu-item" href="http://localhost:4300" target="_blank" rel="noopener" (click)="menuOpen.set(false)">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/><path d="M15 3h6v6M10 14 21 3"/></svg>
                    View store ↗
                  </a>
                  <button class="menu-item danger" (click)="logout()">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><path d="m16 17 5-5-5-5M21 12H9"/></svg>
                    Sign out
                  </button>
                </div>
              }
            </div>
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
  theme = inject(Theme);
  private router = inject(Router);

  menuOpen = signal(false);

  initials = computed(() => {
    const u = this.auth.user();
    return ((u?.firstName?.[0] ?? '') + (u?.lastName?.[0] ?? '')).toUpperCase() || 'A';
  });

  logout(): void {
    this.menuOpen.set(false);
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
