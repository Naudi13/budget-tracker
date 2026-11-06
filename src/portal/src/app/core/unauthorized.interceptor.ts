import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthTokenStore } from './auth-token.store';

/**
 * A `401` from our API means the token is missing, expired or no longer
 * valid: discard it and go to sign-in (sign-in.md). The sign-in call itself is
 * left alone, since its `401` only means a wrong email or password.
 */
export const unauthorizedInterceptor: HttpInterceptorFn = (request, next) => {
  const tokens = inject(AuthTokenStore);
  const router = inject(Router);

  return next(request).pipe(
    catchError((failure: unknown) => {
      if (
        failure instanceof HttpErrorResponse &&
        failure.status === 401 &&
        request.url.startsWith('/api/') &&
        request.url !== '/api/v1/sessions'
      ) {
        tokens.clear();
        void router.navigate(['/sign-in']);
      }
      return throwError(() => failure);
    }),
  );
};
