import { Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { AuthApiService } from '../../../core/auth/auth-api.service';
import { ProductApiService } from '../../storefront/data/product-api.service';
import { InventoryApiService } from '../inventory/inventory-api.service';

interface Tile {
  label: string;
  value: number | string;
  icon: string;
  link: string;
}

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, MatCardModule, MatIconModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly products = inject(ProductApiService);
  private readonly users = inject(AuthApiService);
  private readonly inventory = inject(InventoryApiService);

  readonly tiles = signal<Tile[]>([
    { label: 'Products', value: '—', icon: 'inventory_2', link: '/admin/products' },
    { label: 'Inventory items', value: '—', icon: 'warehouse', link: '/admin/inventory' },
    { label: 'Users', value: '—', icon: 'group', link: '/admin/users' },
  ]);

  ngOnInit(): void {
    this.products.list({ pageSize: 1 }).subscribe((r) => this.patch('Products', r.totalRecords));
    this.inventory.list(1, 1).subscribe((r) => this.patch('Inventory items', r.totalRecords));
    this.users.users(1, 1).subscribe((r) => this.patch('Users', r.totalRecords));
  }

  private patch(label: string, value: number): void {
    this.tiles.update((tiles) => tiles.map((t) => (t.label === label ? { ...t, value } : t)));
  }
}
