import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map, of, switchMap } from 'rxjs';

import { CategoriesService, CategoryType, Page } from './categories.service';

/** The schemas of bud-tack-docs 07-api/contracts/openapi/transactions.yaml. */
export interface TransactionDetail {
  readonly id: string;
  readonly subcategoryId: string;
  readonly amount: number;
  readonly type: CategoryType;
}

export interface Transaction {
  readonly id: string;
  /** ISO date, `yyyy-MM-dd`. */
  readonly transactionDate: string;
  readonly description: string;
  readonly details: readonly TransactionDetail[];
  /** Who recorded it; only an ADMIN gets it. */
  readonly recordedBy?: string;
  /** Who deleted it, when and why; only in an ADMIN's list of deleted ones. */
  readonly deletion?: DeletionRecord;
}

export interface DeletionRecord {
  readonly deletedAt: string;
  readonly deletedBy: string;
  readonly reason: string;
}

export interface NewTransaction {
  readonly transactionDate: string;
  readonly description: string;
  readonly details: readonly { readonly subcategoryId: string; readonly amount: number }[];
}

/** A subcategory the new record screen may offer: active, in an active category (BR-05). */
export interface SubcategoryOption {
  readonly id: string;
  readonly name: string;
  readonly categoryName: string;
  readonly type: CategoryType;
}

/** The history's page size (07-api/rest-conventions.md, Pagination). */
export const HISTORY_PAGE_SIZE = 20;

/** HU-04 to HU-06: a USER's own transactions, or every account's for an ADMIN. */
@Injectable({ providedIn: 'root' })
export class TransactionsService {
  private readonly http = inject(HttpClient);
  private readonly categories = inject(CategoriesService);

  record(transaction: NewTransaction): Observable<Transaction> {
    return this.http.post<Transaction>('/api/v1/transactions', transaction);
  }

  /** With `deleted`, only the deleted ones (ADMIN). */
  list(page: number, deleted = false): Observable<Page<Transaction>> {
    let params = new HttpParams().set('page', page).set('limit', HISTORY_PAGE_SIZE);
    if (deleted) {
      params = params.set('deleted', true);
    }
    return this.http.get<Page<Transaction>>('/api/v1/transactions', { params });
  }

  /** A soft delete, saying why (BR-04). */
  delete(transactionId: string, reason: string): Observable<void> {
    return this.http.post<void>(`/api/v1/transactions/${transactionId}/deletion`, { reason });
  }

  /** Every active subcategory of every active category, by category then name. */
  subcategoryOptions(): Observable<readonly SubcategoryOption[]> {
    return this.categories.listAll().pipe(
      map((categories) => categories.filter((c) => c.isActive)),
      switchMap((categories) =>
        categories.length === 0
          ? of([])
          : forkJoin(
              categories.map((category) =>
                this.categories.listAllSubcategories(category.id).pipe(
                  map((subcategories) =>
                    subcategories
                      .filter((s) => s.isActive)
                      .map(
                        (s): SubcategoryOption => ({
                          id: s.id,
                          name: s.name,
                          categoryName: category.name,
                          type: category.type,
                        }),
                      ),
                  ),
                ),
              ),
            ).pipe(map((groups) => groups.flat())),
      ),
    );
  }
}

/** The total of a transaction, to the cent. */
export function totalOf(transaction: Transaction): number {
  return Math.round(transaction.details.reduce((sum, d) => sum + d.amount * 100, 0)) / 100;
}
