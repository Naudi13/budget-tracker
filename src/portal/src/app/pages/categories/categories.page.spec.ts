import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { errorInterceptor } from '../../core/error.interceptor';
import { Category, Subcategory } from '../../core/categories.service';
import { CategoriesPage } from './categories.page';

const office: Category = { id: 'c1', name: 'Oficina', type: 'expense', isActive: true };
const sales: Category = { id: 'c2', name: 'Ventas', type: 'income', isActive: true };
const paper: Subcategory = { id: 's1', categoryId: 'c1', name: 'Papelería', isActive: true };

describe('CategoriesPage', () => {
  let fixture: ComponentFixture<CategoriesPage>;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CategoriesPage],
      providers: [provideHttpClient(withInterceptors([errorInterceptor])), provideHttpClientTesting()],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CategoriesPage);
  });

  afterEach(() => backend.verify());

  const page = () => fixture.nativeElement as HTMLElement;
  const text = () => {
    fixture.detectChanges();
    return page().textContent ?? '';
  };
  const button = (label: string, index = 0) =>
    Array.from(page().querySelectorAll('button')).filter((b) => b.textContent?.trim().startsWith(label))[index];

  function click(label: string, index = 0): void {
    fixture.detectChanges();
    button(label, index).click();
    fixture.detectChanges();
  }

  function type(selector: string, value: string): void {
    const input = page().querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function submit(selector: string): void {
    page().querySelector(selector)!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  const pageOf = <T>(data: T[]) => ({ data, meta: { page: 1, limit: 100, total: data.length } });

  /** Answers the category list and each category's subcategories. */
  function load(categories: Category[], subcategories: Record<string, Subcategory[]> = {}): void {
    fixture.detectChanges();
    backend.expectOne((r) => r.url === '/api/v1/categories').flush(pageOf(categories));
    for (const category of categories) {
      backend
        .expectOne((r) => r.url === `/api/v1/categories/${category.id}/subcategories`)
        .flush(pageOf(subcategories[category.id] ?? []));
    }
    fixture.detectChanges();
  }

  const expectCall = (method: string, url: string): TestRequest =>
    backend.expectOne((r) => r.method === method && r.url === url);

  it('shows a skeleton while loading', () => {
    expect(page().querySelector('[aria-busy="true"]')).toBeNull();
    fixture.detectChanges();

    expect(page().querySelector('[aria-busy="true"]')).not.toBeNull();
    backend.expectOne('/api/v1/categories?page=1&limit=100').flush(pageOf([]));
  });

  it('offers the first category when there is none', () => {
    load([]);

    expect(text()).toContain('Aún no tienes categorías');
    expect(page().querySelectorAll('.btn-primary').length).toBe(1);
  });

  it('says the list could not load, and retries', () => {
    fixture.detectChanges();
    backend.expectOne((r) => r.url === '/api/v1/categories').flush({}, { status: 500, statusText: 'Error' });

    expect(text()).toContain('No pudimos cargar las categorías.');
    click('Reintentar');
    backend.expectOne((r) => r.url === '/api/v1/categories').flush(pageOf([]));
    expect(text()).toContain('Aún no tienes categorías');
  });

  it('groups categories by type, each with its subcategories and its state', () => {
    load([office, { ...sales, isActive: false }], { c1: [paper] });

    const sections = Array.from(page().querySelectorAll('section h2')).map((h) => h.textContent);
    expect(sections).toEqual(['Ingresos', 'Gastos']);
    expect(text()).toContain('Papelería');
    expect(text()).toContain('Inactiva');
  });

  it('creates a category and lists it', () => {
    load([]);
    click('Nueva categoría');

    type('#new-category-name', ' Oficina ');
    (page().querySelector('input[value="expense"]') as HTMLInputElement).click();
    submit('form.new-category');

    const request = expectCall('POST', '/api/v1/categories');
    expect(request.request.body).toEqual({ name: 'Oficina', type: 'expense' });
    request.flush(office);

    expect(text()).toContain('Oficina creada.');
    expect(page().querySelector('form.new-category')).toBeNull();
    expect(page().querySelector('section[aria-label="Gastos"]')?.textContent).toContain('Oficina');
  });

  it('asks for a name and a type before sending anything', () => {
    load([]);
    click('Nueva categoría');

    submit('form.new-category');

    expect(text()).toContain('Escribe un nombre.');
    expect(text()).toContain('Elige ingreso o gasto.');
  });

  it('says a name is taken in that type', () => {
    load([office]);
    click('Nueva categoría');
    type('#new-category-name', 'Oficina');
    (page().querySelector('input[value="expense"]') as HTMLInputElement).click();
    submit('form.new-category');

    expectCall('POST', '/api/v1/categories').flush({ error: 'CONFLICT', message: 'x' }, { status: 409, statusText: 'Conflict' });

    expect(text()).toContain('Ya existe una categoría de gasto llamada Oficina.');
  });

  it('adds a subcategory, and says when its name is taken in the category', () => {
    load([office]);

    type('#sub-c1', 'Papelería');
    submit('form.add-subcategory');
    expectCall('POST', '/api/v1/categories/c1/subcategories').flush(paper);
    expect(text()).toContain('Papelería');

    type('#sub-c1', 'papelería');
    submit('form.add-subcategory');
    expectCall('POST', '/api/v1/categories/c1/subcategories').flush(
      { error: 'CONFLICT', message: 'x' },
      { status: 409, statusText: 'Conflict' },
    );
    expect(text()).toContain('Esta categoría ya tiene una subcategoría llamada papelería.');
  });

  it('refuses an empty subcategory name without calling the API', () => {
    load([office]);

    submit('form.add-subcategory');

    expect(text()).toContain('Escribe un nombre.');
  });

  it('deactivates and reactivates a category', () => {
    load([office]);

    click('Desactivar');
    expectCall('PATCH', '/api/v1/categories/c1').flush({ ...office, isActive: false });
    expect(text()).toContain('Inactiva');

    click('Activar');
    expectCall('PATCH', '/api/v1/categories/c1').flush(office);
    expect(text()).not.toContain('Inactiva');
  });

  it('deletes an unused category after confirming', () => {
    load([office]);

    click('Eliminar');
    expect(page().querySelector('[role="alertdialog"]')?.textContent).toContain('¿Eliminar Oficina?');
    click('Eliminar', 1);
    expectCall('DELETE', '/api/v1/categories/c1').flush(null, { status: 204, statusText: 'No Content' });

    expect(text()).toContain('Oficina eliminada.');
    expect(page().querySelector('[role="alertdialog"]')).toBeNull();
    expect(text()).toContain('Aún no tienes categorías');
  });

  it('puts focus back on the button that opened the dialog', async () => {
    load([office]);
    const opener = button('Eliminar');
    opener.focus();

    click('Eliminar');
    click('Cancelar');
    await fixture.whenStable();

    expect(document.activeElement).toBe(opener);
  });

  it('changes nothing when the delete is cancelled, or Escape is pressed', () => {
    load([office]);

    click('Eliminar');
    click('Cancelar');
    expect(page().querySelector('[role="alertdialog"]')).toBeNull();

    click('Eliminar');
    page().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();
    expect(page().querySelector('[role="alertdialog"]')).toBeNull();
  });

  it('blocks deleting a category in use, and deactivates it instead (BR-05)', () => {
    load([office], { c1: [paper] });

    click('Eliminar');
    click('Eliminar', 2);
    expectCall('DELETE', '/api/v1/categories/c1').flush(
      { error: 'CONFLICT', message: 'x' },
      { status: 409, statusText: 'Conflict' },
    );
    fixture.detectChanges();

    expect(page().querySelector('[role="alertdialog"]')?.textContent).toContain(
      'Oficina está en uso: tiene movimientos, subcategorías o presupuestos. Mejor desactívala.',
    );
    click('Desactivar', 2);
    expectCall('PATCH', '/api/v1/categories/c1').flush({ ...office, isActive: false });
    fixture.detectChanges();

    expect(page().querySelector('[role="alertdialog"]')).toBeNull();
    expect(text()).toContain('Inactiva');
  });

  it('deletes a subcategory', () => {
    load([office], { c1: [paper] });

    click('Eliminar', 1);
    expect(page().querySelector('[role="alertdialog"]')?.textContent).toContain('¿Eliminar Papelería?');
    click('Eliminar', 2);
    expectCall('DELETE', '/api/v1/categories/c1/subcategories/s1').flush(null, { status: 204, statusText: 'No Content' });

    expect(text()).toContain('Papelería eliminada.');
  });

  it('offers a retry when a delete fails for another reason', () => {
    load([office]);

    click('Eliminar');
    click('Eliminar', 1);
    expectCall('DELETE', '/api/v1/categories/c1').flush({}, { status: 500, statusText: 'Error' });
    fixture.detectChanges();

    expect(page().querySelector('[role="alertdialog"]')?.textContent).toContain('No pudimos eliminar Oficina.');
    click('Reintentar');
    expectCall('DELETE', '/api/v1/categories/c1').flush(null, { status: 204, statusText: 'No Content' });
    expect(text()).toContain('Oficina eliminada.');
  });

  it('reloads when a category is gone', () => {
    load([office]);

    click('Eliminar');
    click('Eliminar', 1);
    expectCall('DELETE', '/api/v1/categories/c1').flush({}, { status: 404, statusText: 'Not Found' });

    backend.expectOne((r) => r.url === '/api/v1/categories').flush(pageOf([]));
    expect(text()).toContain('Aún no tienes categorías');
  });

  it('says when a change could not be saved', () => {
    load([office], { c1: [paper] });

    click('Desactivar', 1);
    expectCall('PATCH', '/api/v1/categories/c1/subcategories/s1').flush({}, { status: 500, statusText: 'Error' });

    expect(text()).toContain('No pudimos guardar el cambio. Inténtalo de nuevo.');
  });
});
