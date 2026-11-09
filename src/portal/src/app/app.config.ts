import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth.interceptor';
import { errorInterceptor } from './core/error.interceptor';
import { provideSpanishLocale } from './core/locale';
import { unauthorizedInterceptor } from './core/unauthorized.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    ...provideSpanishLocale(),
    // Requests pass left to right, responses right to left: the token goes on
    // first; a 401 is seen raw (sign the user out) before it becomes an ApiError.
    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor, errorInterceptor, unauthorizedInterceptor]),
    ),
  ],
};
