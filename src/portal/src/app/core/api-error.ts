import { HttpErrorResponse } from '@angular/common/http';

/** The codes of `07-api/rest-conventions.md`, plus NETWORK_ERROR for a request that never got an answer. */
export type ApiErrorCode =
  | 'VALIDATION_ERROR'
  | 'BUSINESS_RULE_VIOLATED'
  | 'CONFLICT'
  | 'NOT_FOUND'
  | 'UNAUTHORIZED'
  | 'INTERNAL_ERROR'
  | 'NETWORK_ERROR';

export interface ApiErrorDetail {
  readonly field: string;
  readonly message: string;
}

/** The `{ error, message, details }` body every non-2xx API response carries. */
export interface ApiError {
  readonly status: number;
  readonly error: ApiErrorCode;
  readonly message: string;
  readonly details: readonly ApiErrorDetail[];
}

const KNOWN_CODES: readonly string[] = [
  'VALIDATION_ERROR',
  'BUSINESS_RULE_VIOLATED',
  'CONFLICT',
  'NOT_FOUND',
  'UNAUTHORIZED',
  'INTERNAL_ERROR',
];

/**
 * Turns any failed HTTP call into an {@link ApiError}, so a screen handles one
 * shape whether the API answered with its error body, a proxy answered with
 * HTML, or the network dropped the request. The API's own `message` is in
 * English for developers; a screen shows its own Spanish text, chosen by
 * `status` and `error`, never that message.
 */
export function toApiError(response: HttpErrorResponse): ApiError {
  if (response.status === 0) {
    return {
      status: 0,
      error: 'NETWORK_ERROR',
      message: 'No pudimos conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.',
      details: [],
    };
  }

  const body: unknown = response.error;
  if (isErrorBody(body)) {
    return {
      status: response.status,
      error: body.error as ApiErrorCode,
      message: body.message,
      details: Array.isArray(body.details) ? body.details.filter(isDetail) : [],
    };
  }

  return {
    status: response.status,
    error: fallbackCode(response.status),
    message: 'Algo salió mal. Inténtalo de nuevo.',
    details: [],
  };
}

function isErrorBody(body: unknown): body is { error: string; message: string; details?: unknown } {
  if (typeof body !== 'object' || body === null) {
    return false;
  }
  const candidate = body as Record<string, unknown>;
  return (
    typeof candidate['error'] === 'string' &&
    KNOWN_CODES.includes(candidate['error']) &&
    typeof candidate['message'] === 'string'
  );
}

function isDetail(value: unknown): value is ApiErrorDetail {
  if (typeof value !== 'object' || value === null) {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return typeof candidate['field'] === 'string' && typeof candidate['message'] === 'string';
}

function fallbackCode(status: number): ApiErrorCode {
  switch (status) {
    case 400:
      return 'VALIDATION_ERROR';
    case 401:
      return 'UNAUTHORIZED';
    case 404:
      return 'NOT_FOUND';
    case 409:
      return 'CONFLICT';
    case 422:
      return 'BUSINESS_RULE_VIOLATED';
    default:
      return 'INTERNAL_ERROR';
  }
}
