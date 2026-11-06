import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { CategoriesService, Category } from './categories.service';

const category = (n: number): Category => ({ id: `c${n}`, name: `C${n}`, type: 'expense', isActive: true });

describe('CategoriesService', () => {
  let service: CategoriesService;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(CategoriesService);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('reads every page of categories, 100 at a time', async () => {
    const all = firstValueFrom(service.listAll());

    const first = backend.expectOne((r) => r.url === '/api/v1/categories' && r.params.get('page') === '1');
    expect(first.request.params.get('limit')).toBe('100');
    first.flush({ data: [category(1), category(2)], meta: { page: 1, limit: 2, total: 3 } });
    backend
      .expectOne((r) => r.url === '/api/v1/categories' && r.params.get('page') === '2')
      .flush({ data: [category(3)], meta: { page: 2, limit: 2, total: 3 } });

    expect((await all).map((c) => c.id)).toEqual(['c1', 'c2', 'c3']);
  });

  it('stops after one page when everything fits', async () => {
    const all = firstValueFrom(service.listAllSubcategories('c1'));

    backend
      .expectOne((r) => r.url === '/api/v1/categories/c1/subcategories')
      .flush({ data: [], meta: { page: 1, limit: 100, total: 0 } });

    expect(await all).toEqual([]);
  });

  it('sends each change to its endpoint', () => {
    service.create('Oficina', 'expense').subscribe();
    service.delete('c1').subscribe();
    service.setActive('c1', false).subscribe();
    service.createSubcategory('c1', 'Papelería').subscribe();
    service.deleteSubcategory('c1', 's1').subscribe();
    service.setSubcategoryActive('c1', 's1', true).subscribe();

    const calls = [
      ['POST', '/api/v1/categories', { name: 'Oficina', type: 'expense' }],
      ['DELETE', '/api/v1/categories/c1', null],
      ['PATCH', '/api/v1/categories/c1', { isActive: false }],
      ['POST', '/api/v1/categories/c1/subcategories', { name: 'Papelería' }],
      ['DELETE', '/api/v1/categories/c1/subcategories/s1', null],
      ['PATCH', '/api/v1/categories/c1/subcategories/s1', { isActive: true }],
    ] as const;
    for (const [method, url, body] of calls) {
      const request = backend.expectOne((r) => r.method === method && r.url === url);
      expect(request.request.body).toEqual(body);
      request.flush(null);
    }
  });
});
