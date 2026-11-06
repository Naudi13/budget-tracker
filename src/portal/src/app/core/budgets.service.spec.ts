import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { BudgetsService } from './budgets.service';

describe('BudgetsService', () => {
  let service: BudgetsService;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(BudgetsService);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('lists 20 per page, by category only when one is given', () => {
    service.list(2).subscribe();
    service.list(1, 'c1').subscribe();

    const all = backend.expectOne((r) => r.url === '/api/v1/budgets' && !r.params.has('categoryId'));
    expect(all.request.params.get('page')).toBe('2');
    expect(all.request.params.get('limit')).toBe('20');
    all.flush({ data: [], meta: { page: 2, limit: 20, total: 0 } });
    backend
      .expectOne((r) => r.url === '/api/v1/budgets' && r.params.get('categoryId') === 'c1')
      .flush({ data: [], meta: { page: 1, limit: 20, total: 0 } });
  });

  it('sends each change to its endpoint', () => {
    const budget = { categoryId: 'c1', amount: 500, periodStart: '2026-11-01', periodEnd: '2026-11-30' };
    service.create(budget).subscribe();
    service.update('b1', { amount: 650 }).subscribe();
    service.delete('b1').subscribe();

    const calls = [
      ['POST', '/api/v1/budgets', budget],
      ['PATCH', '/api/v1/budgets/b1', { amount: 650 }],
      ['DELETE', '/api/v1/budgets/b1', null],
    ] as const;
    for (const [method, url, body] of calls) {
      const request = backend.expectOne((r) => r.method === method && r.url === url);
      expect(request.request.body).toEqual(body);
      request.flush(null);
    }
  });
});
