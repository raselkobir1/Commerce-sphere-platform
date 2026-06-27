import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { CatalogPage } from './pages/catalog/catalog';
import { ProductDetailPage } from './pages/product-detail/product-detail';
import { CartPage } from './pages/cart/cart';
import { CheckoutPage } from './pages/checkout/checkout';
import { OrderSuccessPage } from './pages/order-success/order-success';
import { LoginPage } from './pages/login/login';
import { RegisterPage } from './pages/register/register';
import { SsoCallbackPage } from './pages/sso-callback/sso-callback';

export const routes: Routes = [
  { path: '', component: CatalogPage },
  { path: 'product/:id', component: ProductDetailPage },
  { path: 'cart', component: CartPage },
  { path: 'checkout', component: CheckoutPage, canActivate: [authGuard] },
  { path: 'order-success', component: OrderSuccessPage },
  { path: 'login', component: LoginPage },
  { path: 'register', component: RegisterPage },
  { path: 'sso-callback', component: SsoCallbackPage },
  { path: '**', redirectTo: '' },
];
