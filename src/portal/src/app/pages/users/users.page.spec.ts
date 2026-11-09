import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { errorInterceptor } from '../../core/error.interceptor';
import { User } from '../../core/identity.service';
import { UsersPage } from './users.page';

const admin: User = { id: 'u1', name: 'Admin', email: 'admin@example.com', role: 'ADMIN', status: 'ACTIVE' };
const ana: User = { id: 'u2', name: 'Ana', email: 'ana@example.com', role: 'USER', status: 'PENDING' };

describe('UsersPage', () => {
  let fixture: ComponentFixture<UsersPage>;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [UsersPage],
      providers: [provideRouter([]), provideHttpClient(withInterceptors([errorInterceptor])), provideHttpClientTesting()],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(UsersPage);
    fixture.detectChanges();
  });

  afterEach(() => backend.verify());

  const page = () => fixture.nativeElement as HTMLElement;
  const text = () => {
    fixture.detectChanges();
    return (page().textContent ?? '').replace(/\s+/g, ' ');
  };

  function respond(data: User[], pageNumber = 1, total = data.length): void {
    backend
      .expectOne((r) => r.method === 'GET' && r.url === '/api/v1/users' && r.params.get('page') === String(pageNumber) && r.params.get('limit') === '20')
      .flush({ data, meta: { page: pageNumber, limit: 20, total } });
    fixture.detectChanges();
  }

  function fail(status: number): void {
    backend.expectOne((r) => r.url === '/api/v1/users').flush(null, { status, statusText: 'Error' });
    fixture.detectChanges();
  }

  function click(label: string): void {
    fixture.detectChanges();
    const button = Array.from(page().querySelectorAll('button')).find((b) => b.textContent?.trim().startsWith(label))!;
    button.click();
    fixture.detectChanges();
  }

  function type(id: string, value: string): void {
    const field = page().querySelector<HTMLInputElement | HTMLSelectElement>(`#${id}`)!;
    field.value = value;
    field.dispatchEvent(new Event(field instanceof HTMLSelectElement ? 'change' : 'input'));
    fixture.detectChanges();
  }

  function submit(): void {
    page().querySelector('form.user-form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  function fill(): void {
    click('Nuevo usuario');
    type('user-name', ' Ana ');
    type('user-email', 'ana@example.com');
    type('user-role', 'USER');
    submit();
  }

  const shownLink = () => page().querySelector<HTMLInputElement>('.dialog input')?.value;

  it('shows a skeleton while loading', () => {
    expect(page().querySelector('[aria-busy="true"]')).not.toBeNull();
    respond([admin]);
  });

  it('lists name, email, role and state in Spanish', () => {
    respond([admin, ana]);

    const cells = Array.from(page().querySelectorAll('tbody tr')).map((tr) =>
      Array.from(tr.querySelectorAll('td'))
        .slice(0, 4)
        .map((td) => td.textContent?.trim()),
    );
    expect(cells).toEqual([
      ['Admin', 'admin@example.com', 'Administrador', 'Activa'],
      ['Ana', 'ana@example.com', 'Usuario', 'Pendiente'],
    ]);
  });

  it('pages twenty at a time', () => {
    respond([admin], 1, 21);

    expect(text()).toContain('Página 1 de 2');
    click('Siguiente');
    respond([ana], 2, 21);
    expect(text()).toContain('Página 2 de 2');
  });

  it('explains a 403', () => {
    fail(403);

    expect(text()).toContain('No tienes permiso para ver esta sección.');
    expect(page().querySelector('button')).toBeNull();
  });

  it('offers to retry any other failure', () => {
    fail(500);

    expect(text()).toContain('No pudimos cargar los usuarios.');
    click('Reintentar');
    respond([admin]);
    expect(text()).toContain('admin@example.com');
  });

  it('explains every field it cannot accept, without calling the API', () => {
    respond([admin]);
    click('Nuevo usuario');
    submit();

    const shown = text();
    expect(shown).toContain('Escribe el nombre.');
    expect(shown).toContain('Escribe un correo válido');
    expect(shown).toContain('Elige un rol.');
  });

  it('creates the account and shows its activation link once', () => {
    respond([admin]);
    fill();

    const request = backend.expectOne((r) => r.method === 'POST');
    expect(request.request.url).toBe('/api/v1/users');
    expect(request.request.body).toEqual({ name: 'Ana', email: 'ana@example.com', role: 'USER' });
    request.flush({ user: ana, activation: { token: 'abc', expiresAt: '2026-11-12T00:00:00Z' } });
    respond([admin, ana]);

    expect(text()).toContain('no se volverá a mostrar');
    expect(shownLink()).toBe(`${location.origin}/activate#token=abc`);
    click('Cerrar');
    expect(page().querySelector('.dialog')).toBeNull();
  });

  it('says an email is taken, keeping what was typed', () => {
    respond([admin]);
    fill();

    backend.expectOne((r) => r.method === 'POST').flush({ error: 'CONFLICT', message: 'x' }, { status: 409, statusText: 'Conflict' });

    expect(text()).toContain('Ya existe una cuenta con este correo.');
    expect(page().querySelector<HTMLInputElement>('#user-name')!.value).toBe(' Ana ');
  });

  it('generates a new activation link for a pending account', () => {
    respond([ana]);
    click('Generar enlace');

    backend.expectOne((r) => r.url === '/api/v1/users/u2/activation-links').flush({ token: 'new', expiresAt: 'x' });
    fixture.detectChanges();

    expect(shownLink()).toBe(`${location.origin}/activate#token=new`);
  });

  it('generates a reset link for an active account', () => {
    respond([admin]);
    click('Restablecer contraseña');

    backend.expectOne((r) => r.url === '/api/v1/users/u1/password-resets').flush({ token: 'r', expiresAt: 'x' });
    fixture.detectChanges();

    expect(shownLink()).toBe(`${location.origin}/reset-password#token=r`);
  });

  it('says when a link could not be generated', () => {
    respond([admin]);
    click('Restablecer contraseña');

    backend.expectOne((r) => r.url === '/api/v1/users/u1/password-resets').flush(null, { status: 500, statusText: 'Error' });

    expect(text()).toContain('No pudimos generar el enlace. Inténtalo de nuevo.');
  });
});
