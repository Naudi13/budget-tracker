import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { AuthTokenStore } from '../../core/auth-token.store';
import { errorInterceptor } from '../../core/error.interceptor';
import { SignInPage } from './sign-in.page';

describe('SignInPage', () => {
  let fixture: ComponentFixture<SignInPage>;
  let backend: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  function setUp(state?: object): void {
    sessionStorage.clear();
    history.replaceState(state ?? null, '');
    TestBed.configureTestingModule({
      imports: [SignInPage],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(SignInPage);
    fixture.detectChanges();
  }

  afterEach(() => {
    backend.verify();
    history.replaceState(null, '');
  });

  const page = () => fixture.nativeElement as HTMLElement;
  const input = (id: string) => page().querySelector(`#${id}`) as HTMLInputElement;
  const submitButton = () => page().querySelector('button[type=submit]') as HTMLButtonElement;

  function type(id: string, value: string): void {
    input(id).value = value;
    input(id).dispatchEvent(new Event('input'));
  }

  function submit(): void {
    page().querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  it('starts empty, with labelled fields', () => {
    setUp();

    expect(input('sign-in-email').value).toBe('');
    expect(page().querySelector('label[for=sign-in-email]')?.textContent).toBe('Correo electrónico');
    expect(page().querySelector('label[for=sign-in-password]')?.textContent).toBe('Contraseña');
    expect(page().querySelector('[role=status]')).toBeNull();
  });

  it('shows both field messages and sends nothing when the form is empty', () => {
    setUp();

    submit();

    expect(page().textContent).toContain('Escribe tu correo, por ejemplo ana@example.com.');
    expect(page().textContent).toContain('Escribe tu contraseña.');
    expect(input('sign-in-email').getAttribute('aria-invalid')).toBe('true');
    backend.expectNone('/api/v1/sessions');
  });

  it('signs in, says so while waiting, then goes to the dashboard', () => {
    setUp();
    type('sign-in-email', 'owner@example.com');
    type('sign-in-password', 'Str0ngPass!23');

    submit();

    expect(submitButton().disabled).toBe(true);
    expect(submitButton().textContent?.trim()).toBe('Iniciando sesión…');
    const request = backend.expectOne('/api/v1/sessions');
    expect(request.request.body).toEqual({ email: 'owner@example.com', password: 'Str0ngPass!23' });
    request.flush({ token: 't.o.k', tokenType: 'Bearer', expiresIn: 3600 }, { status: 201, statusText: 'Created' });

    expect(TestBed.inject(AuthTokenStore).token()).toBe('t.o.k');
    expect(navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('says to wait after too many failed attempts (RNF-02)', () => {
    setUp();
    type('sign-in-email', 'owner@example.com');
    type('sign-in-password', 'right-password');

    submit();
    backend
      .expectOne('/api/v1/sessions')
      .flush({ error: 'TOO_MANY_REQUESTS', message: 'x' }, { status: 429, statusText: 'Too Many Requests' });
    fixture.detectChanges();

    expect(page().querySelector('[role=alert]')?.textContent).toContain('Demasiados intentos fallidos con este correo.');
    expect(input('sign-in-email').value).toBe('owner@example.com');
    expect(input('sign-in-password').value).toBe('');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('keeps the email and clears the password on a wrong password', () => {
    setUp();
    type('sign-in-email', 'owner@example.com');
    type('sign-in-password', 'wrong-password');

    submit();
    backend
      .expectOne('/api/v1/sessions')
      .flush({ error: 'UNAUTHORIZED', message: 'x' }, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    expect(page().querySelector('[role=alert]')?.textContent).toContain('El correo o la contraseña no son correctos.');
    expect(input('sign-in-email').value).toBe('owner@example.com');
    expect(input('sign-in-password').value).toBe('');
    expect(page().querySelector('#sign-in-password-error')).toBeNull();
    expect(submitButton().disabled).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('offers a retry when the server cannot be reached', () => {
    setUp();
    type('sign-in-email', 'owner@example.com');
    type('sign-in-password', 'Str0ngPass!23');

    submit();
    backend.expectOne('/api/v1/sessions').error(new ProgressEvent('error'));
    fixture.detectChanges();

    const alert = page().querySelector('[role=alert]') as HTMLElement;
    expect(alert.textContent).toContain('No pudimos iniciar tu sesión. Inténtalo de nuevo.');
    (alert.querySelector('button') as HTMLButtonElement).click();
    backend
      .expectOne('/api/v1/sessions')
      .flush({ token: 't', tokenType: 'Bearer', expiresIn: 1 }, { status: 201, statusText: 'Created' });
    expect(navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('confirms a password just saved', () => {
    setUp({ passwordSaved: true });

    expect(page().querySelector('[role=status]')?.textContent).toContain('Contraseña guardada. Inicia sesión.');
  });

  it('drops the welcome once an attempt fails, so only the error shows', () => {
    setUp({ passwordSaved: true });
    type('sign-in-email', 'new@example.com');
    type('sign-in-password', 'wrong-password');

    submit();
    backend
      .expectOne('/api/v1/sessions')
      .flush({ error: 'UNAUTHORIZED', message: 'x' }, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    expect(page().querySelector('[role=status]')).toBeNull();
    expect(page().querySelector('[role=alert]')?.textContent).toContain('El correo o la contraseña no son correctos.');
  });

  it('offers no sign-up, and says where to go for a forgotten password', () => {
    setUp();

    expect(page().querySelector('a')).toBeNull();
    expect(page().textContent).toContain('Pide a tu administrador un enlace para restablecerla.');
  });
});
