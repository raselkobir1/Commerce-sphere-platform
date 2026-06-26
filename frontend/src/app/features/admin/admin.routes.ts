import { Routes } from '@angular/router';
import { AdminLayout } from '../../layouts/admin-layout/admin-layout';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminLayout,
    children: [
      { path: '', pathMatch: 'full', loadComponent: () => import('./dashboard/dashboard').then((m) => m.Dashboard) },
      {
        path: 'products',
        pathMatch: 'full',
        loadComponent: () => import('./products/product-list/product-list').then((m) => m.ProductList),
      },
      {
        path: 'products/new',
        loadComponent: () => import('./products/product-form/product-form').then((m) => m.ProductForm),
      },
      {
        path: 'products/:id/edit',
        loadComponent: () => import('./products/product-form/product-form').then((m) => m.ProductForm),
      },
      {
        path: 'inventory',
        loadComponent: () => import('./inventory/inventory-list/inventory-list').then((m) => m.InventoryList),
      },
      { path: 'users', loadComponent: () => import('./users/user-list/user-list').then((m) => m.UserList) },
      { path: 'account', loadChildren: () => import('../account/account.routes').then((m) => m.ACCOUNT_ROUTES) },
    ],
  },
];
