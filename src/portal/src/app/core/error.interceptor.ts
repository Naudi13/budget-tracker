import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

import { toApiError } from './api-error';

/** Every failed call reaches the screen as an `ApiError`, never as a raw `HttpErrorResponse`. */
export const errorInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((failure: unknown) =>
      throwError(() => (failure instanceof HttpErrorResponse ? toApiError(failure) : failure)),
    ),
  );
