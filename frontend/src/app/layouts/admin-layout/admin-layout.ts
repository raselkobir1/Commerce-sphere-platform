import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

interface NavLink {
  label: string;
  icon: string;
  path: string;
}

// Admin shell: persistent sidebar nav + top bar with the signed-in user menu.
@Component({
  selector: 'app-admin-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
  ],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.scss',
})
export class AdminLayout {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly user = this.auth.user;

  readonly links: NavLink[] = [
    { label: 'Dashboard', icon: 'dashboard', path: '/admin' },
    { label: 'Products', icon: 'inventory_2', path: '/admin/products' },
    { label: 'Inventory', icon: 'warehouse', path: '/admin/inventory' },
    { label: 'Users', icon: 'group', path: '/admin/users' },
    { label: 'Account', icon: 'account_circle', path: '/admin/account' },
  ];

  logout(): void {
    this.auth.logout().subscribe(() => void this.router.navigateByUrl('/auth/login'));
  }
}
