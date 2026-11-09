import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { Budget } from '../../core/budgets.service';
import { Category } from '../../core/categories.service';
import { errorInterceptor } from '../../core/error.interceptor';
import { provideSpanishLocale } from '../../core/locale';
import { BudgetsPage } from './budgets.page';

const office: Category = { id: 'c1', name: 'Oficina', type: 'expense', isActive: true };
const travel: Category = { id: 'c2', name: 'Viajes', type: 'expense', isActive: true };
const old: Category = { id: 'c3', name: 'Viejos', type: 'expense', isActive: false };
const november: Budget = { id: 'b1', categoryId: 'c1', amount: 500000, periodStart: '2026-11-01', periodEnd: '2026-11-30' };
const retired: Budget = { id: 'b2', categoryId: 'c3', amount: 85.5, periodStart: '2026-10-01', periodEnd: '2026-10-31' };

describe('BudgetsPage', () => {
  let fixture: ComponentFixture<BudgetsPage>;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [BudgetsPage],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        ...provideSpanishLocale(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(BudgetsPage);
  });

  afterEach(() => backend.verify());

  const page = () => fixture.nativeElement as HTMLElement;
  const text = () => {
    fixture.detectChanges();
    return (page().textContent ?? '').replace(/\s+/g, ' ');
  };

  function categories(data: Category[] = [office, travel, old]): void {
    backend.expectOne((r) => r.url === '/api/v1/categories').flush({ data, meta: { page: 1, limit: 100, total: data.length } });
  }

  function budgets(data: Budget[], options: { page?: number; total?: number; categoryId?: string } = {}): void {
    const pageNumber = options.page ?? 1;
    backend
      .expectOne(
        (r) =>
          r.url === '/api/v1/budgets' &&
          r.params.get('page') === String(pageNumber) &&
          r.params.get('categoryId') === (options.categoryId ?? null),
      )
      .flush({ data, meta: { page: pageNumber, limit: 20, total: options.total ?? data.length } });
    fixture.detectChanges();
  }

  function open(data: Budget[], options?: { page?: number; total?: number }): void {
    fixture.detectChanges();
    categories();
    budgets(data, options);
  }

  function click(label: string, index = 0): void {
    fixture.detectChanges();
    const buttons = Array.from(page().querySelectorAll('button')).filter((b) => b.textContent?.trim().startsWith(label));
    buttons[index].click();
    fixture.detectChanges();
  }

  function type(id: string, value: string): void {
    const field = page().querySelector<HTMLInputElement | HTMLSelectElement>(`#${id}`)!;
    field.value = value;
    field.dispatchEvent(new Event(field instanceof HTMLSelectElement ? 'change' : 'input'));
    fixture.detectChanges();
  }

  function fill(category: string, amount: string, start: string, end: string): void {
    type('budget-category', category);
    type('budget-amount', amount);
    type('budget-start', start);
    type('budget-end', end);
  }

  function submit(): void {
    page().querySelector<HTMLFormElement>('form.budget-form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  const saved = (): TestRequest => backend.expectOne((r) => r.method === 'POST' || r.method === 'PATCH');

  it('shows a skeleton while loading', () => {
    fixture.detectChanges();
    expect(page().querySelector('[aria-busy="true"]')).not.toBeNull();
    categories();
    budgets([]);
  });

  it('invites to set the first budget when there is none', () => {
    open([]);

    expect(text()).toContain('Aún no tienes presupuestos');
    expect(page().querySelector('#budget-filter')).toBeNull();
  });

  it('lists category, amount and both dates, never shifting a date', () => {
    open([november, retired]);

    const cells = Array.from(page().querySelectorAll('tbody tr')).map((tr) =>
      Array.from(tr.querySelectorAll('td'))
        .slice(0, 4)
        .map((td) => td.textContent?.trim()),
    );
    const space = String.fromCharCode(160);
    expect(cells[0]).toEqual(['Oficina', `$${space}500.000,00`, '01/11/2026', '30/11/2026']);
    // An inactive category keeps its name in the list.
    expect(cells[1][0]).toBe('Viejos');
  });

  it('offers to retry when the budgets cannot be loaded', () => {
    fixture.detectChanges();
    categories();
    backend.expectOne((r) => r.url === '/api/v1/budgets').flush(null, { status: 500, statusText: 'Error' });

    expect(text()).toContain('No pudimos cargar los presupuestos.');
    click('Reintentar');
    categories();
    budgets([november]);
    expect(text()).toContain('Oficina');
  });

  it('filters by category, offering inactive ones too', () => {
    open([november, retired]);

    const options = Array.from(page().querySelectorAll('#budget-filter option')).map((o) => o.textContent?.trim());
    expect(options).toEqual(['Todas', 'Oficina', 'Viajes', 'Viejos']);
    type('budget-filter', 'c2');
    budgets([], { categoryId: 'c2' });
    expect(text()).toContain('Esta categoría no tiene presupuestos.');
  });

  it('pages twenty at a time', () => {
    open([november], { total: 21 });

    expect(text()).toContain('Página 1 de 2');
    click('Siguiente');
    budgets([retired], { page: 2, total: 21 });
    expect(text()).toContain('Página 2 de 2');
  });

  it('offers only active categories in the form', () => {
    open([]);
    click('Nuevo presupuesto');

    const options = Array.from(page().querySelectorAll('#budget-category option')).map((o) => o.textContent?.trim());
    expect(options).toEqual(['Elige una categoría', 'Oficina', 'Viajes']);
  });

  it('explains every field it cannot accept, without calling the API', () => {
    open([]);
    click('Nuevo presupuesto');
    type('budget-amount', '0');
    submit();

    const shown = text();
    expect(shown).toContain('Elige una categoría. Las inactivas no aparecen.');
    expect(shown).toContain('Escribe un monto mayor que cero.');
    expect(shown).toContain('Elige el primer día del periodo.');
    expect(shown).toContain('La fecha final debe ser posterior a la inicial.');
  });

  it('refuses a period that ends on the day it starts or before', () => {
    open([]);
    click('Nuevo presupuesto');
    fill('c1', '500', '2026-11-30', '2026-11-30');
    submit();

    expect(text()).toContain('La fecha final debe ser posterior a la inicial.');
  });

  it('creates a budget, amount typed with a comma, and shows the banner', () => {
    open([]);
    click('Nuevo presupuesto');
    fill('c1', '500000,50', '2026-11-01', '2026-11-30');
    submit();

    const request = saved();
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ categoryId: 'c1', amount: 500000.5, periodStart: '2026-11-01', periodEnd: '2026-11-30' });
    request.flush({ ...november, amount: 500000.5 });
    budgets([november]);

    expect(text()).toContain('Presupuesto guardado.');
    expect(page().querySelector('form.budget-form')).toBeNull();
  });

  it('shows the period message when the API answers 422, keeping the form', () => {
    open([]);
    click('Nuevo presupuesto');
    fill('c1', '500', '2026-11-01', '2026-11-30');
    submit();

    saved().flush(
      { error: 'BUSINESS_RULE_VIOLATED', message: 'x', details: [{ field: 'periodEnd', message: 'must be after periodStart' }] },
      { status: 422, statusText: 'Unprocessable' },
    );

    expect(text()).toContain('La fecha final debe ser posterior a la inicial.');
    expect(page().querySelector<HTMLInputElement>('#budget-amount')!.value).toBe('500');
  });

  it('stops offering a category that went inactive meanwhile', () => {
    open([]);
    click('Nuevo presupuesto');
    fill('c2', '500', '2026-11-01', '2026-11-30');
    submit();

    saved().flush(
      { error: 'BUSINESS_RULE_VIOLATED', message: 'x', details: [{ field: 'categoryId', message: 'is inactive' }] },
      { status: 422, statusText: 'Unprocessable' },
    );
    categories([office, { ...travel, isActive: false }, old]);

    expect(text()).toContain('Elige una categoría. Las inactivas no aparecen.');
    const options = Array.from(page().querySelectorAll('#budget-category option')).map((o) => o.textContent?.trim());
    expect(options).toEqual(['Elige una categoría', 'Oficina']);
  });

  it('keeps what was typed when the save fails', () => {
    open([]);
    click('Nuevo presupuesto');
    fill('c1', '500', '2026-11-01', '2026-11-30');
    submit();

    saved().flush(null, { status: 500, statusText: 'Error' });

    expect(text()).toContain('No pudimos guardar el presupuesto. Inténtalo de nuevo.');
    expect(page().querySelector<HTMLInputElement>('#budget-start')!.value).toBe('2026-11-01');
  });

  it('edits a budget, sending only what changed', () => {
    open([november]);
    click('Editar');

    expect(page().querySelector<HTMLInputElement>('#budget-amount')!.value).toBe('500000');
    type('budget-amount', '650');
    submit();

    const request = saved();
    expect(request.request.method).toBe('PATCH');
    expect(request.request.url).toBe('/api/v1/budgets/b1');
    expect(request.request.body).toEqual({ amount: 650 });
    request.flush({ ...november, amount: 650 });
    budgets([{ ...november, amount: 650 }]);
    expect(text()).toContain('Presupuesto guardado.');
  });

  it('asks for another category when editing one whose category is inactive', () => {
    open([retired]);
    click('Editar');

    expect(page().querySelector<HTMLInputElement>('#budget-amount')!.value).toBe('85,50');
    submit();

    expect(text()).toContain('Elige una categoría. Las inactivas no aparecen.');
  });

  it('asks before deleting, names the category, and lets you cancel', () => {
    open([november]);
    click('Eliminar');

    expect(text()).toContain("¿Eliminar el presupuesto de 'Oficina'? No se puede deshacer.");
    click('Cancelar');
    expect(page().querySelector('[role="alertdialog"]')).toBeNull();
  });

  it('deletes after confirming, and offers to retry when it fails', () => {
    open([november]);
    click('Eliminar');
    click('Eliminar', 1);

    backend.expectOne((r) => r.method === 'DELETE').flush(null, { status: 500, statusText: 'Error' });
    expect(text()).toContain('No pudimos eliminar el presupuesto. Inténtalo de nuevo.');

    click('Reintentar');
    backend.expectOne((r) => r.method === 'DELETE' && r.url === '/api/v1/budgets/b1').flush(null);
    budgets([]);
    expect(text()).toContain('Presupuesto eliminado.');
  });
});
