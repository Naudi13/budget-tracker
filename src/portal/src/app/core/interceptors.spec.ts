import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { ApiError } from './api-error';
import { authInterceptor } from './auth.interceptor';
import { AuthTokenStore } from './auth-token.store';
import { errorInterceptor } from './error.interceptor';

describe('HTTP interceptors', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let tokens: AuthTokenStore;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    tokens = TestBed.inject(AuthTokenStore);
  });

  afterEach(() => backend.verify());

  it('sends no Authorization header while there is no token', () => {
    http.get('/api/v1/transactions').subscribe();

    expect(backend.expectOne('/api/v1/transactions').request.headers.has('Authorization')).toBe(false);
  });

  it('sends the bearer token to the API', () => {
    tokens.set('abc.def.ghi');

    http.get('/api/v1/transactions').subscribe();

    expect(backend.expectOne('/api/v1/transactions').request.headers.get('Authorization')).toBe(
      'Bearer abc.def.ghi',
    );
  });

  it('never sends the token outside the API', () => {
    tokens.set('abc.def.ghi');

    http.get('/health').subscribe();
    http.get('https://example.com/api/v1/x').subscribe();

    expect(backend.expectOne('/health').request.headers.has('Authorization')).toBe(false);
    expect(backend.expectOne('https://example.com/api/v1/x').request.headers.has('Authorization')).toBe(false);
  });

  it('turns a failed call into an ApiError', async () => {
    const call = firstValueFrom(http.get('/api/v1/categories/9'));
    backend
      .expectOne('/api/v1/categories/9')
      .flush({ error: 'NOT_FOUND', message: 'The resource does not exist.' }, { status: 404, statusText: 'Not Found' });

    const failure = (await call.catch((e: unknown) => e)) as ApiError;

    expect(failure).toEqual({
      status: 404,
      error: 'NOT_FOUND',
      message: 'The resource does not exist.',
      details: [],
    });
  });
});

describe('AuthTokenStore', () => {
  beforeEach(() => sessionStorage.clear());

  it('keeps the token in sessionStorage, and clears it', () => {
    const store = TestBed.inject(AuthTokenStore);

    store.set('t1');
    expect(store.token()).toBe('t1');
    expect(sessionStorage.getItem('bud-tack.token')).toBe('t1');

    store.clear();
    expect(store.token()).toBeNull();
    expect(sessionStorage.getItem('bud-tack.token')).toBeNull();
  });

  it('picks up a token left by an earlier page load', () => {
    sessionStorage.setItem('bud-tack.token', 'from-before');
    TestBed.resetTestingModule();

    expect(TestBed.inject(AuthTokenStore).token()).toBe('from-before');
  });
});
