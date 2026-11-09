import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { Dashboard } from '../../core/dashboard.service';
import { errorInterceptor } from '../../core/error.interceptor';
import { provideSpanishLocale } from '../../core/locale';
import { Transaction } from '../../core/transactions.service';
import { DashboardPage } from './dashboard.page';

const row = (id: string, date: string, description: string, type: 'income' | 'expense', amount: number): Transaction => ({
  id,
  transactionDate: date,
  description,
  details: [{ id: `${id}-0`, subcategoryId: 's', amount, type }],
});

const empty: Dashboard = { totalIncome: 0, totalExpenses: 0, netBalance: 0, budget: null, recentTransactions: [] };

describe('DashboardPage', () => {
  let fixture: ComponentFixture<DashboardPage>;
  let backend: HttpTestingController;

  function create(state: object | null = null): void {
    history.replaceState(state, '');
    TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        ...provideSpanishLocale(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges();
    backend.expectOne((r) => r.url === '/api/v1/categories').flush({
      data: [
        { id: 'c1', name: 'Oficina', type: 'expense', isActive: true },
        { id: 'c2', name: 'Viejos', type: 'expense', isActive: false },
      ],
      meta: { page: 1, limit: 100, total: 2 },
    });
    fixture.detectChanges();
  }

  afterEach(() => {
    backend.verify();
    history.replaceState(null, '');
  });

  const page = () => fixture.nativeElement as HTMLElement;
  const answer = (dashboard: Dashboard) => {
    backend.expectOne('/api/v1/dashboard').flush(dashboard);
    fixture.detectChanges();
  };
  const tiles = () =>
    Array.from(page().querySelectorAll('.tile')).map(
      // The currency pipe separates "$" with a no-break space (160); compare with a plain one.
      (t) =>
        `${t.querySelector('.label')?.textContent?.trim()} ${t.querySelector('.figure')?.textContent?.trim()}`
          .split(String.fromCharCode(160))
          .join(' '),
    );

  it('shows skeleton tiles while the single request is in flight', () => {
    create();

    expect(page().querySelector('[aria-busy="true"]')).not.toBeNull();
    expect(page().querySelectorAll('.tile.skeleton').length).toBe(3);
    answer(empty);
  });

  it('shows three tiles at zero and invites to record the first transaction', () => {
    create();
    answer(empty);

    expect(tiles()).toEqual(['Ingresos $ 0,00', 'Gastos $ 0,00', 'Balance $ 0,00']);
    expect(page().textContent).toContain('Aún no tienes movimientos');
    expect(page().querySelector('.empty a')?.getAttribute('href')).toBe('/transactions/new');
    expect(page().textContent).not.toContain('Ver todo el historial');
  });

  it('shows the KPIs and the recent transactions, newest first, with a link to the history', () => {
    create();
    answer({
      totalIncome: 1000,
      totalExpenses: 350,
      netBalance: 650,
      budget: null, recentTransactions: [row('t2', '2026-11-06', 'Papel', 'expense', 350), row('t1', '2026-11-01', 'Venta', 'income', 1000)],
    });

    expect(tiles()).toEqual(['Ingresos $ 1.000,00', 'Gastos $ 350,00', 'Balance $ 650,00']);
    const rows = Array.from(page().querySelectorAll('.rows li')).map((li) => li.textContent?.replace(/\s+/g, ' ').trim());
    expect(rows[0]).toContain('6 nov');
    expect(rows[0]).toContain('Papel');
    expect(rows[0]).toContain('Gasto');
    expect(rows[1]).toContain('Ingreso');
    expect(page().querySelector('.recent-heading a')?.getAttribute('href')).toBe('/transactions');
  });

  it('shows a negative balance in red, keeping its minus sign', () => {
    create();
    answer({ totalIncome: 100, totalExpenses: 250.5, netBalance: -150.5, budget: null, recentTransactions: [] });

    const balance = page().querySelector('.balance .figure') as HTMLElement;
    expect(balance.classList).toContain('negative');
    expect(balance.textContent).toContain('-');
    expect(balance.textContent).toContain('150,50');
  });

  it('shows no partial figures when it cannot load, and retries', () => {
    create();
    backend.expectOne('/api/v1/dashboard').flush({}, { status: 500, statusText: 'Error' });
    fixture.detectChanges();

    expect(page().textContent).toContain('No pudimos cargar el panel.');
    expect(page().querySelector('.tile')).toBeNull();

    (page().querySelector('[role="alert"] button') as HTMLButtonElement).click();
    fixture.detectChanges();
    answer(empty);
    expect(tiles().length).toBe(3);
  });

  it('says the transaction was saved when coming back from a new record', () => {
    create({ notice: 'Movimiento guardado.' });
    answer(empty);

    expect(page().querySelector('[role="status"]')?.textContent).toContain('Movimiento guardado.');
  });

  it('has one primary action, recording a transaction', () => {
    create();
    answer(empty);

    const primaries = page().querySelectorAll('.btn-primary');
    expect(primaries.length).toBe(1);
    expect(primaries[0].getAttribute('href')).toBe('/transactions/new');
  });

  const band = () => (page().querySelector('.budget')?.textContent ?? '').replace(/\s+/g, ' ');

  it('compares spending with the budget', () => {
    create();
    answer({ ...empty, budget: { amount: 1000, spent: 400, available: 600 } });

    expect(band()).toContain('Presupuesto');
    expect(band()).toContain('600,00');
    expect(band()).not.toContain('Sobre el presupuesto');
  });

  it('says when spending is over budget, in words too', () => {
    create();
    answer({ ...empty, budget: { amount: 500, spent: 600, available: -100 } });

    expect(band()).toContain('Sobre el presupuesto');
    expect(page().querySelector('.budget .value.negative')).not.toBeNull();
  });

  it('says when no budget is in force', () => {
    create();
    answer(empty);

    expect(band()).toContain('Sin presupuesto vigente');
  });

  it('filters by an active category, and goes back to all', () => {
    create();
    answer(empty);
    const select = page().querySelector<HTMLSelectElement>('#dashboard-category')!;

    expect(Array.from(select.options).map((o) => o.textContent?.trim())).toEqual(['Todas', 'Oficina']);
    select.value = 'c1';
    select.dispatchEvent(new Event('change'));
    backend.expectOne((r) => r.url === '/api/v1/dashboard' && r.params.get('categoryId') === 'c1').flush(empty);
    fixture.detectChanges();

    select.value = '';
    select.dispatchEvent(new Event('change'));
    backend.expectOne((r) => r.url === '/api/v1/dashboard' && !r.params.has('categoryId')).flush(empty);
  });
});
