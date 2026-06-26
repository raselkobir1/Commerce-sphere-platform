import { Routes } from '@angular/router';
import { StoreLayout } from '../../layouts/store-layout/store-layout';

export const STOREFRONT_ROUTES: Routes = [
  {
    path: '',
    component: StoreLayout,
    children: [
      { path: '', pathMatch: 'full', loadComponent: () => import('./catalog/catalog').then((m) => m.Catalog) },
      {
        path: 'products/:id',
        loadComponent: () => import('./product-detail/product-detail').then((m) => m.ProductDetail),
      },
      { path: 'cart', loadComponent: () => import('./cart-page/cart-page').then((m) => m.CartPage) },
      { path: 'checkout', loadComponent: () => import('./checkout/checkout').then((m) => m.Checkout) },
      { path: 'account', loadChildren: () => import('../account/account.routes').then((m) => m.ACCOUNT_ROUTES) },
    ],
  },
];
