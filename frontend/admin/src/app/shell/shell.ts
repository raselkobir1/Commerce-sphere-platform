import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Auth } from '../core/auth';

// The signed-in layout: sidebar + top bar + the current page.
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="logo">Admin<span>Sphere</span></div>
        <nav>
          <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
          <a routerLink="/products" routerLinkActive="active">Products</a>
          <a routerLink="/categories" routerLinkActive="active">Categories</a>
          <a routerLink="/inventory" routerLinkActive="active">Inventory</a>
          <a routerLink="/users" routerLinkActive="active">Users</a>
        </nav>
      </aside>

      <div class="main">
        <header class="topbar">
          <span class="who">{{ auth.user()?.firstName }} {{ auth.user()?.lastName }}</span>
          <button class="btn btn-sm" (click)="logout()">Sign out</button>
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

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
