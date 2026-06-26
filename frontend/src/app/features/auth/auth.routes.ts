import { Routes } from '@angular/router';
import { AuthLayout } from '../../layouts/auth-layout/auth-layout';

// All unauthenticated pages, rendered inside the centered AuthLayout card.
export const AUTH_ROUTES: Routes = [
  {
    path: '',
    component: AuthLayout,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'login' },
      { path: 'login', loadComponent: () => import('./login/login').then((m) => m.Login) },
      { path: 'register', loadComponent: () => import('./register/register').then((m) => m.Register) },
      {
        path: 'forgot-password',
        loadComponent: () => import('./forgot-password/forgot-password').then((m) => m.ForgotPassword),
      },
      {
        path: 'reset-password',
        loadComponent: () => import('./reset-password/reset-password').then((m) => m.ResetPassword),
      },
      {
        path: 'verify-email',
        loadComponent: () => import('./verify-email/verify-email').then((m) => m.VerifyEmail),
      },
      {
        path: 'sso/callback',
        loadComponent: () => import('./sso-callback/sso-callback').then((m) => m.SsoCallback),
      },
    ],
  },
];
