import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';

import { errorInterceptor } from '../../core/error.interceptor';
import { ActivatePage } from './activate.page';

const INVALID = 'Este enlace no es válido o ya venció. Pide uno nuevo a tu administrador.';

describe('ActivatePage', () => {
  let fixture: ComponentFixture<ActivatePage>;
  let backend: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  function setUp(fragment: string | null = 'token=abc', mode = 'activate'): void {
    TestBed.configureTestingModule({
      imports: [ActivatePage],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { fragment, data: { mode } } } },
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(ActivatePage);
    fixture.detectChanges();
  }

  afterEach(() => backend.verify());

  const page = () => fixture.nativeElement as HTMLElement;
  const text = () => {
    fixture.detectChanges();
    return page().textContent ?? '';
  };

  function type(id: string, value: string): void {
    const input = page().querySelector<HTMLInputElement>(`#${id}`)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function submit(): void {
    page().querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  function fill(password = 'Str0ngPass!23', repeat = password): void {
    type('activate-password', password);
    type('activate-repeat', repeat);
    submit();
  }

  it('names what it is for', () => {
    setUp('token=abc', 'reset');

    expect(page().querySelector('h1')?.textContent).toBe('Restablece tu contraseña');
  });

  it('saves the password with the token and goes to sign-in', () => {
    setUp();
    fill();

    const request = backend.expectOne('/api/v1/account-links/confirm');
    expect(request.request.body).toEqual({ token: 'abc', password: 'Str0ngPass!23' });
    request.flush(null, { status: 204, statusText: 'No Content' });

    expect(navigate).toHaveBeenCalledWith(['/sign-in'], { state: { passwordSaved: true } });
  });

  it('asks for 8 characters and matching passwords without calling the API', () => {
    setUp();
    fill('short', 'other');

    expect(text()).toContain('Usa al menos 8 caracteres.');
    expect(text()).toContain('Las contraseñas no coinciden.');
  });

  it('says a link without a token is not valid, and offers no form', () => {
    setUp(null);

    expect(text()).toContain(INVALID);
    expect(page().querySelector('form')).toBeNull();
  });

  it('says when the API refuses the token', () => {
    setUp();
    fill();

    backend
      .expectOne('/api/v1/account-links/confirm')
      .flush(
        { error: 'VALIDATION_ERROR', message: 'x', details: [{ field: 'token', message: 'y' }] },
        { status: 400, statusText: 'Bad Request' },
      );

    expect(text()).toContain(INVALID);
  });

  it('keeps the form when the API fails for another reason', () => {
    setUp();
    fill();

    backend.expectOne('/api/v1/account-links/confirm').flush(null, { status: 500, statusText: 'Error' });

    expect(text()).toContain('No pudimos guardar tu contraseña. Inténtalo de nuevo.');
    expect(page().querySelector<HTMLInputElement>('#activate-password')!.value).toBe('Str0ngPass!23');
  });
});
