import { Component, OnInit, inject } from '@angular/core';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CartStore } from '../../features/storefront/data/cart-store';

// Customer storefront shell: top navbar with catalog link, cart (with live item badge), user menu.
@Component({
  selector: 'app-store-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatBadgeModule,
    MatMenuModule,
  ],
  templateUrl: './store-layout.html',
  styleUrl: './store-layout.scss',
})
export class StoreLayout implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly cart = inject(CartStore);
  private readonly router = inject(Router);

  readonly user = this.auth.user;
  readonly isAdmin = this.auth.isAdmin;
  readonly itemCount = this.cart.itemCount;

  ngOnInit(): void {
    // Warm the cart so the badge reflects the existing active cart on load.
    this.cart.ensureCart().subscribe({ error: () => void 0 });
  }

  logout(): void {
    this.cart.clearLocal();
    this.auth.logout().subscribe(() => void this.router.navigateByUrl('/auth/login'));
  }
}
