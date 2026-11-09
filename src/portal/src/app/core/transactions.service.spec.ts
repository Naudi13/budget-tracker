import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { TransactionsService, totalOf } from './transactions.service';

describe('TransactionsService', () => {
  let service: TransactionsService;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(TransactionsService);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  const pageOf = <T>(data: T[]) => ({ data, meta: { page: 1, limit: 100, total: data.length } });

  it('offers only active subcategories of active categories, with their type', async () => {
    const options = firstValueFrom(service.subcategoryOptions());

    backend.expectOne((r) => r.url === '/api/v1/categories').flush(
      pageOf([
        { id: 'c1', name: 'Oficina', type: 'expense', isActive: true },
        { id: 'c2', name: 'Viejos', type: 'expense', isActive: false },
        { id: 'c3', name: 'Ventas', type: 'income', isActive: true },
      ]),
    );
    backend.expectOne((r) => r.url === '/api/v1/categories/c1/subcategories').flush(
      pageOf([
        { id: 's1', categoryId: 'c1', name: 'Papelería', isActive: true },
        { id: 's2', categoryId: 'c1', name: 'Tinta', isActive: false },
      ]),
    );
    backend
      .expectOne((r) => r.url === '/api/v1/categories/c3/subcategories')
      .flush(pageOf([{ id: 's3', categoryId: 'c3', name: 'Tienda', isActive: true }]));

    expect(await options).toEqual([
      { id: 's1', name: 'Papelería', categoryName: 'Oficina', type: 'expense' },
      { id: 's3', name: 'Tienda', categoryName: 'Ventas', type: 'income' },
    ]);
  });

  it('offers nothing when there are no active categories', async () => {
    const options = firstValueFrom(service.subcategoryOptions());

    backend.expectOne((r) => r.url === '/api/v1/categories').flush(pageOf([]));

    expect(await options).toEqual([]);
  });

  it('records, lists 20 per page and deletes', () => {
    service.record({ transactionDate: '2026-11-06', description: 'x', details: [{ subcategoryId: 's1', amount: 1 }] }).subscribe();
    service.list(2).subscribe();
    service.delete('t1', 'Monto equivocado').subscribe();

    expect(backend.expectOne((r) => r.method === 'POST' && r.url === '/api/v1/transactions').request.body.description).toBe('x');
    const list = backend.expectOne((r) => r.method === 'GET' && r.url === '/api/v1/transactions');
    expect(list.request.params.get('page')).toBe('2');
    expect(list.request.params.get('limit')).toBe('20');
    backend.expectOne((r) => r.method === 'POST' && r.url === '/api/v1/transactions/t1/deletion');
  });

  it('adds a total to the cent', () => {
    const total = totalOf({
      id: 't',
      transactionDate: '2026-11-06',
      description: 'x',
      details: [
        { id: 'a', subcategoryId: 's', amount: 0.1, type: 'expense' },
        { id: 'b', subcategoryId: 's', amount: 0.2, type: 'expense' },
      ],
    });

    expect(total).toBe(0.3);
  });
});
