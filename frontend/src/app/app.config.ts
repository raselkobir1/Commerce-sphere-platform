import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { AuthService } from './core/auth/auth.service';
import { authTokenInterceptor } from './core/interceptors/auth-token.interceptor';
import { correlationIdInterceptor } from './core/interceptors/correlation-id.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { refreshInterceptor } from './core/interceptors/refresh.interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideAnimations(),
    provideHttpClient(
      // Order matters: error is outermost (final catch), refresh is innermost (first crack at 401).
      withInterceptors([
        errorInterceptor,
        correlationIdInterceptor,
        authTokenInterceptor,
        refreshInterceptor,
      ]),
    ),
    // Rehydrate the signed-in user from the stored token before the first route renders.
    provideAppInitializer(() => inject(AuthService).loadCurrentUser()),
  ],
};
