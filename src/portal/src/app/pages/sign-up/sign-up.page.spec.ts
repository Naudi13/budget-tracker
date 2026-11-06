import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { errorInterceptor } from '../../core/error.interceptor';
import { SignUpPage } from './sign-up.page';

describe('SignUpPage', () => {
  let fixture: ComponentFixture<SignUpPage>;
  let backend: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [SignUpPage],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(SignUpPage);
    fixture.detectChanges();
  });

  afterEach(() => backend.verify());

  const page = () => fixture.nativeElement as HTMLElement;
  const input = (id: string) => page().querySelector(`#${id}`) as HTMLInputElement;
  const submitButton = () => page().querySelector('button[type=submit]') as HTMLButtonElement;

  function type(id: string, value: string): void {
    input(id).value = value;
    input(id).dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function fill(name: string, email: string, password: string, repeat = password): void {
    type('sign-up-name', name);
    type('sign-up-email', email);
    type('sign-up-password', password);
    type('sign-up-repeat', repeat);
  }

  function submit(): void {
    page().querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  it('shows every message of sign-up.md and sends nothing for an invalid form', () => {
    fill(' ', 'nope', 'short', 'other');

    submit();

    const text = page().textContent;
    expect(text).toContain('Escribe tu nombre o el de tu negocio.');
    expect(text).toContain('Escribe un correo válido, por ejemplo ana@example.com.');
    expect(text).toContain('Usa al menos 8 caracteres.');
    expect(text).toContain('Las contraseñas no coinciden.');
    backend.expectNone('/api/v1/users');
  });

  it('shows no error when a field only loses focus, so the button does not move under the pointer', () => {
    input('sign-up-name').dispatchEvent(new Event('focus'));
    input('sign-up-name').dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(page().querySelectorAll('.field-error').length).toBe(0);
  });

  it('notices a mismatch when the password changes after the repeat', () => {
    fill('Ana', 'a@b.co', 'Str0ngPass!23');
    expect(page().textContent).not.toContain('no coinciden');

    type('sign-up-password', 'Str0ngPass!24');

    expect(page().textContent).toContain('Las contraseñas no coinciden.');
  });

  it('creates the account, says so while waiting, then goes to sign-in with the email', () => {
    fill('Ana Owner', 'Owner@Example.com', 'Str0ngPass!23');

    submit();

    expect(submitButton().disabled).toBe(true);
    expect(submitButton().textContent?.trim()).toBe('Creando…');
    const request = backend.expectOne('/api/v1/users');
    expect(request.request.body).toEqual({ name: 'Ana Owner', email: 'Owner@Example.com', password: 'Str0ngPass!23' });
    request.flush({ id: '1', name: 'Ana Owner', email: 'owner@example.com' }, { status: 201, statusText: 'Created' });

    expect(navigate).toHaveBeenCalledWith(['/sign-in'], { state: { email: 'owner@example.com', created: true } });
  });

  it('says the email is taken under Email, and keeps the form', () => {
    fill('Ana Owner', 'owner@example.com', 'Str0ngPass!23');

    submit();
    backend
      .expectOne('/api/v1/users')
      .flush({ error: 'CONFLICT', message: 'x' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    expect(page().querySelector('#sign-up-email-error')?.textContent).toContain(
      'Ya existe una cuenta con este correo.',
    );
    expect(input('sign-up-email').getAttribute('aria-invalid')).toBe('true');
    expect(input('sign-up-name').value).toBe('Ana Owner');
    expect(navigate).not.toHaveBeenCalled();

    type('sign-up-email', 'other@example.com');
    expect(page().querySelector('#sign-up-email-error')).toBeNull();
  });

  it('marks a field the API refused, in Spanish, never with the API English text', () => {
    fill('Ana Owner', 'owner@example.com', 'Str0ngPass!23');

    submit();
    backend.expectOne('/api/v1/users').flush(
      { error: 'VALIDATION_ERROR', message: 'x', details: [{ field: 'password', message: 'Use at most 128 characters.' }] },
      { status: 400, statusText: 'Bad Request' },
    );
    fixture.detectChanges();

    const error = page().querySelector('#sign-up-password-error')?.textContent ?? '';
    expect(error).toContain('Usa entre 8 y 128 caracteres.');
    expect(error).not.toContain('Use at most');
  });

  it('says how long a name may be when it is too long', () => {
    fill('a'.repeat(101), 'owner@example.com', 'Str0ngPass!23');

    submit();

    expect(page().querySelector('#sign-up-name-error')?.textContent).toContain('Usa máximo 100 caracteres.');
    backend.expectNone('/api/v1/users');
  });

  it('says how long a password may be when it is too long', () => {
    fill('Ana', 'owner@example.com', 'p'.repeat(129));

    submit();

    expect(page().querySelector('#sign-up-password-error')?.textContent).toContain('Usa máximo 128 caracteres.');
  });

  it('offers a retry when the server fails, without clearing the form', () => {
    fill('Ana Owner', 'owner@example.com', 'Str0ngPass!23');

    submit();
    backend.expectOne('/api/v1/users').flush({}, { status: 500, statusText: 'Error' });
    fixture.detectChanges();

    const alert = page().querySelector('[role=alert]') as HTMLElement;
    expect(alert.textContent).toContain('No pudimos crear la cuenta. Inténtalo de nuevo.');
    expect(input('sign-up-password').value).toBe('Str0ngPass!23');
    (alert.querySelector('button') as HTMLButtonElement).click();
    backend
      .expectOne('/api/v1/users')
      .flush({ id: '1', name: 'Ana Owner', email: 'owner@example.com' }, { status: 201, statusText: 'Created' });
    expect(navigate).toHaveBeenCalled();
  });
});
