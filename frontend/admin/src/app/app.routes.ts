import { Routes } from '@angular/router';
import { adminGuard } from './core/admin.guard';
import { Shell } from './shell/shell';
import { LoginPage } from './pages/login/login';
import { DashboardPage } from './pages/dashboard/dashboard';
import { ProductsPage } from './pages/products/products';
import { ProductFormPage } from './pages/products/product-form';
import { CategoriesPage } from './pages/categories/categories';
import { UsersPage } from './pages/users/users';
import { InventoryPage } from './pages/inventory/inventory';

export const routes: Routes = [
  { path: 'login', component: LoginPage },
  {
    path: '',
    component: Shell,
    canActivate: [adminGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardPage },
      { path: 'products', component: ProductsPage },
      { path: 'products/new', component: ProductFormPage },
      { path: 'products/:id', component: ProductFormPage },
      { path: 'categories', component: CategoriesPage },
      { path: 'users', component: UsersPage },
      { path: 'inventory', component: InventoryPage },
    ],
  },
  { path: '**', redirectTo: '' },
];
