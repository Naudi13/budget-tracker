import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import localeEsCo from '@angular/common/locales/es-CO';
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth.interceptor';
import { errorInterceptor } from './core/error.interceptor';
import { unauthorizedInterceptor } from './core/unauthorized.interceptor';

// The user sees Spanish (Colombia): dates, numbers and money in es-CO.
registerLocaleData(localeEsCo);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    { provide: LOCALE_ID, useValue: 'es-CO' },
    // Requests pass left to right, responses right to left: the token goes on
    // first; a 401 is seen raw (sign the user out) before it becomes an ApiError.
    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor, errorInterceptor, unauthorizedInterceptor]),
    ),
  ],
};
