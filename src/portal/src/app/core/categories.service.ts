import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { EMPTY, Observable, expand, map, reduce } from 'rxjs';

/** The schemas of bud-tack-docs 07-api/contracts/openapi/categories.yaml. */
export type CategoryType = 'income' | 'expense';

export interface Category {
  readonly id: string;
  readonly name: string;
  readonly type: CategoryType;
  readonly isActive: boolean;
}

export interface Subcategory {
  readonly id: string;
  readonly categoryId: string;
  readonly name: string;
  readonly isActive: boolean;
}

export interface Page<T> {
  readonly data: readonly T[];
  readonly meta: { readonly page: number; readonly limit: number; readonly total: number };
}

/** The largest page the API serves (07-api/rest-conventions.md, Pagination). */
const PAGE_SIZE = 100;

/** HU-02 and HU-03: the signed-in user's categories and subcategories. */
@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private readonly http = inject(HttpClient);

  /** Every category, page after page, in the API's order: by type, then name. */
  listAll(): Observable<readonly Category[]> {
    return this.all<Category>('/api/v1/categories');
  }

  create(name: string, type: CategoryType): Observable<Category> {
    return this.http.post<Category>('/api/v1/categories', { name, type });
  }

  delete(categoryId: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/categories/${categoryId}`);
  }

  rename(categoryId: string, name: string): Observable<Category> {
    return this.http.patch<Category>(`/api/v1/categories/${categoryId}`, { name });
  }

  setActive(categoryId: string, isActive: boolean): Observable<Category> {
    return this.http.patch<Category>(`/api/v1/categories/${categoryId}`, { isActive });
  }

  /** Every subcategory of one category, by name. */
  listAllSubcategories(categoryId: string): Observable<readonly Subcategory[]> {
    return this.all<Subcategory>(`/api/v1/categories/${categoryId}/subcategories`);
  }

  createSubcategory(categoryId: string, name: string): Observable<Subcategory> {
    return this.http.post<Subcategory>(`/api/v1/categories/${categoryId}/subcategories`, { name });
  }

  deleteSubcategory(categoryId: string, subcategoryId: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/categories/${categoryId}/subcategories/${subcategoryId}`);
  }

  setSubcategoryActive(categoryId: string, subcategoryId: string, isActive: boolean): Observable<Subcategory> {
    return this.http.patch<Subcategory>(`/api/v1/categories/${categoryId}/subcategories/${subcategoryId}`, {
      isActive,
    });
  }

  private all<T>(url: string): Observable<readonly T[]> {
    const page = (number: number) =>
      this.http.get<Page<T>>(url, {
        params: new HttpParams().set('page', number).set('limit', PAGE_SIZE),
      });

    return page(1).pipe(
      expand((current) =>
        current.meta.page * current.meta.limit < current.meta.total ? page(current.meta.page + 1) : EMPTY,
      ),
      map((current) => current.data),
      reduce((all: readonly T[], data) => [...all, ...data], []),
    );
  }
}
