import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthTokenStore } from './auth-token.store';

/** Every screen but sign-in and sign-up: without a token, go to sign-in (sign-in.md). */
export const signedInGuard: CanActivateFn = () =>
  inject(AuthTokenStore).token() !== null || inject(Router).createUrlTree(['/sign-in']);

/** Sign-in and sign-up: a signed-in user has nothing to do there, so go to the dashboard. */
export const signedOutGuard: CanActivateFn = () =>
  inject(AuthTokenStore).token() === null || inject(Router).createUrlTree(['/dashboard']);
