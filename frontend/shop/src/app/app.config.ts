import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Auth } from './core/auth';
import { Cart } from './core/cart';
import { authInterceptor } from './core/auth.interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    // Restore the session, then load that user's cart, before the first page shows.
    provideAppInitializer(async () => {
      const auth = inject(Auth);
      const cart = inject(Cart);
      await firstValueFrom(auth.restore());
      cart.load();
    }),
  ],
};
