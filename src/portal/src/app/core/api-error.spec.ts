import { HttpErrorResponse } from '@angular/common/http';

import { toApiError } from './api-error';

function failure(status: number, error: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error, url: '/api/v1/anything' });
}

describe('toApiError', () => {
  it('keeps the body the API sends', () => {
    const result = toApiError(
      failure(400, {
        error: 'VALIDATION_ERROR',
        message: 'The request is malformed.',
        details: [{ field: 'email', message: 'is required' }],
      }),
    );

    expect(result).toEqual({
      status: 400,
      error: 'VALIDATION_ERROR',
      message: 'The request is malformed.',
      details: [{ field: 'email', message: 'is required' }],
    });
  });

  it('drops details that are not field and message pairs', () => {
    const result = toApiError(
      failure(400, { error: 'VALIDATION_ERROR', message: 'Bad.', details: [{ field: 'a' }, 'b', null] }),
    );

    expect(result.details).toEqual([]);
  });

  it('accepts a body without details', () => {
    const result = toApiError(failure(404, { error: 'NOT_FOUND', message: 'The resource does not exist.' }));

    expect(result.error).toBe('NOT_FOUND');
    expect(result.details).toEqual([]);
  });

  it('reports a request that never got an answer as a network error', () => {
    const result = toApiError(failure(0, new ProgressEvent('error')));

    expect(result.status).toBe(0);
    expect(result.error).toBe('NETWORK_ERROR');
  });

  it.each([
    [400, 'VALIDATION_ERROR'],
    [401, 'UNAUTHORIZED'],
    [404, 'NOT_FOUND'],
    [409, 'CONFLICT'],
    [422, 'BUSINESS_RULE_VIOLATED'],
    [502, 'INTERNAL_ERROR'],
  ])('maps a %i without the error body to %s', (status, code) => {
    const result = toApiError(failure(status, '<html>Bad Gateway</html>'));

    expect(result.error).toBe(code);
    expect(result.message).toBe('Something went wrong. Try again.');
  });

  it('ignores a body whose error code is not one of ours', () => {
    const result = toApiError(failure(500, { error: 'SOMETHING_ELSE', message: 'x' }));

    expect(result.error).toBe('INTERNAL_ERROR');
    expect(result.message).toBe('Something went wrong. Try again.');
  });
});
