import { Routes } from '@angular/router';
import { adminGuard, canView } from './core/admin.guard';
import { Shell } from './shell/shell';
import { LoginPage } from './pages/login/login';
import { DashboardPage } from './pages/dashboard/dashboard';
import { ProductsPage } from './pages/products/products';
import { ProductFormPage } from './pages/products/product-form';
import { CategoriesPage } from './pages/categories/categories';
import { BannersPage } from './pages/banners/banners';
import { UsersPage } from './pages/users/users';
import { InventoryPage } from './pages/inventory/inventory';
import { SettingsPage } from './pages/settings/settings';
import { RolesPage } from './pages/roles/roles';
import { MenusPage } from './pages/menus/menus';
import { PermissionsPage } from './pages/permissions/permissions';
import { OrdersPage } from './pages/orders/orders';

export const routes: Routes = [
  { path: 'login', component: LoginPage },
  {
    path: '',
    component: Shell,
    canActivate: [adminGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardPage, canActivate: [canView('dashboard')] },
      { path: 'products', component: ProductsPage, canActivate: [canView('products')] },
      { path: 'products/new', component: ProductFormPage, canActivate: [canView('products')] },
      { path: 'products/:id', component: ProductFormPage, canActivate: [canView('products')] },
      { path: 'categories', component: CategoriesPage, canActivate: [canView('categories')] },
      { path: 'banners', component: BannersPage, canActivate: [canView('banners')] },
      { path: 'inventory', component: InventoryPage, canActivate: [canView('inventory')] },
      { path: 'orders', component: OrdersPage, canActivate: [canView('orders')] },
      { path: 'users', component: UsersPage, canActivate: [canView('users')] },
      { path: 'roles', component: RolesPage, canActivate: [canView('roles')] },
      { path: 'menus', component: MenusPage, canActivate: [canView('menus')] },
      { path: 'permissions', component: PermissionsPage, canActivate: [canView('permissions')] },
      { path: 'settings', component: SettingsPage, canActivate: [canView('settings')] },
    ],
  },
  { path: '**', redirectTo: '' },
];
