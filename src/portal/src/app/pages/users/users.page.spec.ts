import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { errorInterceptor } from '../../core/error.interceptor';
import { User } from '../../core/identity.service';
import { UsersPage } from './users.page';

const admin: User = { id: 'u1', name: 'Admin', email: 'admin@example.com', role: 'ADMIN' };
const ana: User = { id: 'u2', name: 'Ana', email: 'ana@example.com', role: 'USER' };

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
      .expectOne((r) => r.url === '/api/v1/users' && r.params.get('page') === String(pageNumber) && r.params.get('limit') === '20')
      .flush({ data, meta: { page: pageNumber, limit: 20, total } });
    fixture.detectChanges();
  }

  function fail(status: number): void {
    backend.expectOne((r) => r.url === '/api/v1/users').flush(null, { status, statusText: 'Error' });
    fixture.detectChanges();
  }

  function click(label: string): void {
    const button = Array.from(page().querySelectorAll('button')).find((b) => b.textContent?.trim() === label)!;
    button.click();
    fixture.detectChanges();
  }

  it('shows a skeleton while loading', () => {
    expect(page().querySelector('[aria-busy="true"]')).not.toBeNull();
    respond([admin]);
  });

  it('lists name, email and role in Spanish', () => {
    respond([admin, ana]);

    const cells = Array.from(page().querySelectorAll('tbody tr')).map((tr) =>
      Array.from(tr.querySelectorAll('td')).map((td) => td.textContent?.trim()),
    );
    expect(cells).toEqual([
      ['Admin', 'admin@example.com', 'Administrador'],
      ['Ana', 'ana@example.com', 'Usuario'],
    ]);
  });

  it('pages twenty at a time', () => {
    respond([admin], 1, 21);

    expect(text()).toContain('Página 1 de 2');
    click('Siguiente');
    respond([ana], 2, 21);
    expect(text()).toContain('Página 2 de 2');
  });

  it('explains a 403 and offers no retry', () => {
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
});
