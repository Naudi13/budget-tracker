import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { ApiError } from './api-error';
import { signedInGuard, signedOutGuard } from './auth.guards';
import { authInterceptor } from './auth.interceptor';
import { AuthTokenStore } from './auth-token.store';
import { errorInterceptor } from './error.interceptor';
import { IdentityService } from './identity.service';
import { unauthorizedInterceptor } from './unauthorized.interceptor';

describe('identity in the portal', () => {
  let backend: HttpTestingController;
  let tokens: AuthTokenStore;
  let router: Router;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor, errorInterceptor, unauthorizedInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    tokens = TestBed.inject(AuthTokenStore);
    router = TestBed.inject(Router);
  });

  afterEach(() => backend.verify());

  describe('IdentityService', () => {
    it('signs up with POST /api/v1/users', async () => {
      const call = firstValueFrom(
        TestBed.inject(IdentityService).signUp({ name: 'Ana', email: 'a@b.co', password: '12345678' }),
      );
      const request = backend.expectOne('/api/v1/users');
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual({ name: 'Ana', email: 'a@b.co', password: '12345678' });
      request.flush({ id: '1', name: 'Ana', email: 'a@b.co' }, { status: 201, statusText: 'Created' });

      expect(await call).toEqual({ id: '1', name: 'Ana', email: 'a@b.co' });
      expect(tokens.token()).toBeNull();
    });

    it('signs in with POST /api/v1/sessions and keeps the token', async () => {
      const identity = TestBed.inject(IdentityService);
      const call = firstValueFrom(identity.signIn({ email: 'a@b.co', password: '12345678' }));
      backend
        .expectOne('/api/v1/sessions')
        .flush({ token: 't.o.k', tokenType: 'Bearer', expiresIn: 3600 }, { status: 201, statusText: 'Created' });

      await call;
      expect(tokens.token()).toBe('t.o.k');
      expect(identity.signedIn()).toBe(true);
    });

    it('keeps no token when sign-in fails', async () => {
      const call = firstValueFrom(TestBed.inject(IdentityService).signIn({ email: 'a@b.co', password: 'x' }));
      backend
        .expectOne('/api/v1/sessions')
        .flush({ error: 'UNAUTHORIZED', message: 'The email or password is not correct.' }, { status: 401, statusText: 'Unauthorized' });

      const failure = (await call.catch((e: unknown) => e)) as ApiError;
      expect(failure.status).toBe(401);
      expect(tokens.token()).toBeNull();
    });

    it('signs out by discarding the token', () => {
      tokens.set('t.o.k');
      const identity = TestBed.inject(IdentityService);

      identity.signOut();

      expect(tokens.token()).toBeNull();
      expect(identity.signedIn()).toBe(false);
    });
  });

  describe('unauthorizedInterceptor', () => {
    it('discards the token and goes to sign-in on a 401 from the API', async () => {
      tokens.set('expired.t.ok');
      const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

      const call = firstValueFrom(TestBed.inject(HttpClient).get('/api/v1/transactions'));
      backend.expectOne('/api/v1/transactions').flush(
        { error: 'UNAUTHORIZED', message: 'Sign in to continue.' },
        { status: 401, statusText: 'Unauthorized' },
      );

      const failure = (await call.catch((e: unknown) => e)) as ApiError;
      expect(failure.error).toBe('UNAUTHORIZED');
      expect(tokens.token()).toBeNull();
      expect(navigate).toHaveBeenCalledWith(['/sign-in']);
    });

    it('leaves a wrong password on the sign-in screen alone', async () => {
      const navigate = vi.spyOn(router, 'navigate');

      const call = firstValueFrom(TestBed.inject(HttpClient).post('/api/v1/sessions', {}));
      backend.expectOne('/api/v1/sessions').flush({}, { status: 401, statusText: 'Unauthorized' });

      await call.catch(() => undefined);
      expect(navigate).not.toHaveBeenCalled();
    });

    it('ignores other failures', async () => {
      tokens.set('t.o.k');

      const call = firstValueFrom(TestBed.inject(HttpClient).get('/api/v1/x'));
      backend.expectOne('/api/v1/x').flush({}, { status: 500, statusText: 'Error' });

      await call.catch(() => undefined);
      expect(tokens.token()).toBe('t.o.k');
    });
  });

  describe('guards', () => {
    const run = (guard: typeof signedInGuard) =>
      TestBed.runInInjectionContext(() => guard({} as never, {} as never));

    it('lets a signed-in user through, and sends anyone else to sign-in', () => {
      expect((run(signedInGuard) as UrlTree).toString()).toBe('/sign-in');
      tokens.set('t.o.k');
      expect(run(signedInGuard)).toBe(true);
    });

    it('keeps a signed-in user away from sign-in and sign-up', () => {
      expect(run(signedOutGuard)).toBe(true);
      tokens.set('t.o.k');
      expect((run(signedOutGuard) as UrlTree).toString()).toBe('/dashboard');
    });
  });
});
