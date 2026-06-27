import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { firstValueFrom, of, switchMap } from 'rxjs';
import { Auth } from './core/auth';
import { Perms } from './core/perms';
import { authInterceptor } from './core/auth.interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    // Restore the signed-in admin, then load their menu permissions, before the first page shows.
    provideAppInitializer(() => {
      const auth = inject(Auth);
      const perms = inject(Perms);
      return firstValueFrom(auth.restore().pipe(switchMap((u) => (u ? perms.load() : of(null)))));
    }),
  ],
};
