import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { AuthTokenStore } from './auth-token.store';

/** Adds `Authorization: Bearer <token>` to calls to our own API, and only those. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthTokenStore).token();
  if (token === null || !request.url.startsWith('/api/')) {
    return next(request);
  }
  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
