import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { errorInterceptor } from '../../core/error.interceptor';
import { provideSpanishLocale } from '../../core/locale';
import { NewRecordPage } from './new-record.page';
import { todayIso } from '../money';

const pageOf = <T>(data: T[]) => ({ data, meta: { page: 1, limit: 100, total: data.length } });

describe('NewRecordPage', () => {
  let fixture: ComponentFixture<NewRecordPage>;
  let backend: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [NewRecordPage],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        ...provideSpanishLocale(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(NewRecordPage);
  });

  afterEach(() => backend.verify());

  const page = () => fixture.nativeElement as HTMLElement;
  const text = () => {
    fixture.detectChanges();
    return page().textContent ?? '';
  };

  /** Answers the subcategory options: Papelería (expense) and Tienda (income). */
  function loadOptions(withAny = true): void {
    fixture.detectChanges();
    if (!withAny) {
      backend.expectOne((r) => r.url === '/api/v1/categories').flush(pageOf([]));
      fixture.detectChanges();
      return;
    }
    backend.expectOne((r) => r.url === '/api/v1/categories').flush(
      pageOf([
        { id: 'c1', name: 'Oficina', type: 'expense', isActive: true },
        { id: 'c2', name: 'Ventas', type: 'income', isActive: true },
      ]),
    );
    backend
      .expectOne((r) => r.url === '/api/v1/categories/c1/subcategories')
      .flush(pageOf([{ id: 's1', categoryId: 'c1', name: 'Papelería', isActive: true }]));
    backend
      .expectOne((r) => r.url === '/api/v1/categories/c2/subcategories')
      .flush(pageOf([{ id: 's2', categoryId: 'c2', name: 'Tienda', isActive: true }]));
    fixture.detectChanges();
  }

  function set(selector: string, value: string): void {
    const element = page().querySelector(selector) as HTMLInputElement | HTMLSelectElement;
    element.value = value;
    element.dispatchEvent(new Event(element instanceof HTMLSelectElement ? 'change' : 'input'));
    fixture.detectChanges();
  }

  function click(label: string): void {
    const button = Array.from(page().querySelectorAll('button')).find((b) => b.textContent?.trim().startsWith(label))!;
    button.click();
    fixture.detectChanges();
  }

  function submit(): void {
    page().querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  it('starts with today, an empty description and one empty line', () => {
    loadOptions();

    expect((page().querySelector('#record-date') as HTMLInputElement).value).toBe(todayIso());
    expect((page().querySelector('#record-description') as HTMLInputElement).value).toBe('');
    expect(page().querySelectorAll('.line').length).toBe(1);
    expect(Array.from(page().querySelectorAll('optgroup')).map((g) => g.label)).toEqual([
      'Oficina (gasto)',
      'Ventas (ingreso)',
    ]);
  });

  it('asks for a category first when there is no subcategory', () => {
    loadOptions(false);

    expect(text()).toContain('necesitas al menos una subcategoría activa');
    expect(page().querySelector('form')).toBeNull();
  });

  it('offers a retry when the subcategories cannot load', () => {
    fixture.detectChanges();
    backend.expectOne((r) => r.url === '/api/v1/categories').flush({}, { status: 500, statusText: 'Error' });

    expect(text()).toContain('No pudimos cargar tus subcategorías.');
    click('Reintentar');
    backend.expectOne((r) => r.url === '/api/v1/categories').flush(pageOf([]));
  });

  it('shows every message of new-record.md and sends nothing', () => {
    loadOptions();
    set('#record-date', '');

    submit();

    const t = text();
    expect(t).toContain('Elige la fecha en que se movió el dinero.');
    expect(t).toContain("Describe el movimiento, por ejemplo 'Útiles de oficina'.");
    expect(t).toContain('Elige una subcategoría. Las inactivas no aparecen.');
    expect(t).toContain('Escribe un monto mayor que cero.');
  });

  it('refuses an amount of zero, and one with three decimals', () => {
    loadOptions();

    set('#line-amount-0', '0');
    expect(text()).toContain('Escribe un monto mayor que cero.');
    set('#line-amount-0', '10,005');
    expect(text()).toContain('Usa máximo dos decimales.');
  });

  it('warns on the line that mixes income into an expense (BR-02), before saving', () => {
    loadOptions();
    set('#line-sub-0', 's1');
    click('Agregar línea');

    set('#line-sub-1', 's2');

    expect(page().querySelector('#line-sub-error-1')?.textContent).toContain(
      'Este movimiento ya tiene líneas de gasto. Quítalas para agregar un ingreso, o guárdalas como otro movimiento.',
    );
    submit();
  });

  it('records two lines, adds them up, and goes back to the dashboard with a notice', () => {
    loadOptions();
    set('#record-description', ' Papel para la impresora ');
    set('#line-sub-0', 's1');
    set('#line-amount-0', '85,50');
    click('Agregar línea');
    set('#line-sub-1', 's1');
    set('#line-amount-1', '0.01');

    expect(text()).toContain('85,51');
    submit();

    const request = backend.expectOne((r) => r.method === 'POST' && r.url === '/api/v1/transactions');
    expect(request.request.body).toEqual({
      transactionDate: todayIso(),
      description: 'Papel para la impresora',
      details: [
        { subcategoryId: 's1', amount: 85.5 },
        { subcategoryId: 's1', amount: 0.01 },
      ],
    });
    expect(text()).toContain('Guardando…');
    request.flush({ id: 't1', transactionDate: todayIso(), description: 'x', details: [] });

    expect(navigate).toHaveBeenCalledWith(['/dashboard'], { state: { notice: 'Movimiento guardado.' } });
  });

  it('removes a line', () => {
    loadOptions();
    click('Agregar línea');
    expect(page().querySelectorAll('.line').length).toBe(2);

    click('Quitar');

    expect(page().querySelectorAll('.line').length).toBe(1);
  });

  it('keeps the form and offers a retry when the save fails', () => {
    loadOptions();
    set('#record-description', 'Papel');
    set('#line-sub-0', 's1');
    set('#line-amount-0', '10');
    submit();

    backend.expectOne((r) => r.method === 'POST').flush({}, { status: 500, statusText: 'Error' });

    expect(text()).toContain('No pudimos guardar el registro. Tus datos siguen aquí, inténtalo de nuevo.');
    expect((page().querySelector('#record-description') as HTMLInputElement).value).toBe('Papel');
    click('Reintentar');
    backend.expectOne((r) => r.method === 'POST').flush({ id: 't1', transactionDate: todayIso(), description: 'x', details: [] });
    expect(navigate).toHaveBeenCalled();
  });

  it('marks the line the API refused as inactive (422), and reloads the options', () => {
    loadOptions();
    set('#record-description', 'Papel');
    set('#line-sub-0', 's1');
    set('#line-amount-0', '10');
    submit();

    backend.expectOne((r) => r.method === 'POST').flush(
      {
        error: 'BUSINESS_RULE_VIOLATED',
        message: 'x',
        details: [{ field: 'details[0].subcategoryId', message: 'is inactive, or its category is' }],
      },
      { status: 422, statusText: 'Unprocessable' },
    );
    fixture.detectChanges();

    expect(page().querySelector('#line-sub-error-0')?.textContent).toContain('Las inactivas no aparecen.');
    // The options reload quietly: the form and what was typed stay on screen.
    backend.expectOne((r) => r.url === '/api/v1/categories').flush(pageOf([]));
    fixture.detectChanges();
    expect((page().querySelector('#record-description') as HTMLInputElement | null)?.value).toBe('Papel');
  });
});
