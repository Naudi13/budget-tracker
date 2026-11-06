import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { errorInterceptor } from '../../core/error.interceptor';
import { provideSpanishLocale } from '../../core/locale';
import { Transaction } from '../../core/transactions.service';
import { HistoryPage } from './history.page';

const row = (id: string, date: string, description: string, type: 'income' | 'expense', ...amounts: number[]): Transaction => ({
  id,
  transactionDate: date,
  description,
  details: amounts.map((amount, i) => ({ id: `${id}-${i}`, subcategoryId: 's', amount, type })),
});

const paper = row('t1', '2026-11-06', 'Papel', 'expense', 85.5, 0.01);
const sale = row('t2', '2026-10-20', 'Venta', 'income', 1200);

describe('HistoryPage', () => {
  let fixture: ComponentFixture<HistoryPage>;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HistoryPage],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        ...provideSpanishLocale(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(HistoryPage);
  });

  afterEach(() => backend.verify());

  const page = () => fixture.nativeElement as HTMLElement;
  const text = () => {
    fixture.detectChanges();
    return page().textContent ?? '';
  };

  function answer(data: Transaction[], pageNumber = 1, total = data.length): void {
    fixture.detectChanges();
    backend
      .expectOne((r) => r.url === '/api/v1/transactions' && r.params.get('page') === String(pageNumber))
      .flush({ data, meta: { page: pageNumber, limit: 20, total } });
    fixture.detectChanges();
  }

  function click(label: string, index = 0): void {
    fixture.detectChanges();
    Array.from(page().querySelectorAll('button')).filter((b) => b.textContent?.trim().startsWith(label))[index].click();
    fixture.detectChanges();
  }

  it('shows a skeleton while loading', () => {
    fixture.detectChanges();
    expect(page().querySelector('[aria-busy="true"]')).not.toBeNull();
    answer([]);
  });

  it('invites to record the first transaction when there is none', () => {
    answer([]);

    expect(text()).toContain('Aún no has registrado movimientos');
    expect(page().querySelector('a.btn-primary')?.getAttribute('href')).toBe('/transactions/new');
  });

  it('lists date, description, type and total, never shifting the date', () => {
    answer([paper, sale]);

    const cells = Array.from(page().querySelectorAll('tbody tr')).map((tr) =>
      Array.from(tr.querySelectorAll('td')).slice(0, 4).map((td) => td.textContent?.trim().replace(/\s+/g, ' ')),
    );
    expect(cells[0][0]).toContain('6');
    expect(cells[0][0]).toContain('nov');
    expect(cells[0].slice(1, 3)).toEqual(['Papel', 'Gasto']);
    expect(cells[0][3]).toContain('85,51');
    expect(cells[1][2]).toBe('Ingreso');
    expect(cells[1][3]).toContain('1.200,00');
  });

  it('says the history could not load, and retries the same page', () => {
    fixture.detectChanges();
    backend.expectOne((r) => r.url === '/api/v1/transactions').flush({}, { status: 500, statusText: 'Error' });

    expect(text()).toContain('No pudimos cargar el historial.');
    click('Reintentar');
    answer([paper]);
    expect(text()).toContain('Papel');
  });

  it('moves between pages of 20', () => {
    answer([paper], 1, 45);
    expect(text()).toContain('Página 1 de 3');

    click('Siguiente');
    answer([sale], 2, 45);

    expect(text()).toContain('Página 2 de 3');
    expect(text()).toContain('Venta');
  });

  it('deletes after confirming, says so, and reloads', () => {
    answer([paper, sale]);

    click('Eliminar');
    expect(page().querySelector('[role="alertdialog"]')?.textContent).toContain(
      '¿Eliminar este movimiento? Saldrá del historial y de los totales del panel.',
    );
    click('Eliminar', 2);
    backend.expectOne((r) => r.method === 'DELETE' && r.url === '/api/v1/transactions/t1').flush(null, { status: 204, statusText: 'No Content' });
    answer([sale]);

    expect(text()).toContain('Movimiento eliminado.');
    expect(page().querySelector('[role="alertdialog"]')).toBeNull();
    expect(text()).not.toContain('Papel');
  });

  it('changes nothing when cancelled, or on Escape', () => {
    answer([paper]);

    click('Eliminar');
    click('Cancelar');
    expect(page().querySelector('[role="alertdialog"]')).toBeNull();

    click('Eliminar');
    page().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();
    expect(page().querySelector('[role="alertdialog"]')).toBeNull();
  });

  it('keeps the dialog open with a retry when the delete fails', () => {
    answer([paper]);

    click('Eliminar');
    click('Eliminar', 1);
    backend.expectOne((r) => r.method === 'DELETE').flush({}, { status: 500, statusText: 'Error' });
    fixture.detectChanges();

    expect(page().querySelector('[role="alertdialog"]')?.textContent).toContain(
      'No pudimos eliminar el movimiento. Inténtalo de nuevo.',
    );
    click('Reintentar');
    backend.expectOne((r) => r.method === 'DELETE').flush(null, { status: 204, statusText: 'No Content' });
    answer([]);
    expect(text()).toContain('Movimiento eliminado.');
  });

  it('steps back a page when a delete empties the last one', () => {
    answer([sale], 1, 21);
    click('Siguiente');
    answer([paper], 2, 21);

    click('Eliminar');
    click('Eliminar', 1);
    backend.expectOne((r) => r.method === 'DELETE').flush(null, { status: 204, statusText: 'No Content' });
    answer([], 2, 20);
    answer([sale], 1, 20);

    expect(text()).toContain('Venta');
  });
});
