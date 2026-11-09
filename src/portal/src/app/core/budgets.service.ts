import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Page } from './categories.service';

/** The schemas of bud-tack-docs 07-api/contracts/openapi/budgets.yaml. */
export interface Budget {
  readonly id: string;
  readonly categoryId: string;
  readonly amount: number;
  /** ISO date, `yyyy-MM-dd`. */
  readonly periodStart: string;
  /** ISO date, `yyyy-MM-dd`, after `periodStart`. */
  readonly periodEnd: string;
}

export interface NewBudget {
  readonly categoryId: string;
  readonly amount: number;
  readonly periodStart: string;
  readonly periodEnd: string;
}

/** Only the fields to change (BR-06). */
export type BudgetChanges = Partial<NewBudget>;

/** The budgets page size (07-api/rest-conventions.md, Pagination). */
export const BUDGETS_PAGE_SIZE = 20;

/** HU-07: the signed-in user's budgets. */
@Injectable({ providedIn: 'root' })
export class BudgetsService {
  private readonly http = inject(HttpClient);

  /** Newest period first; only one category's when `categoryId` is given. */
  list(page: number, categoryId: string | null = null): Observable<Page<Budget>> {
    let params = new HttpParams().set('page', page).set('limit', BUDGETS_PAGE_SIZE);
    if (categoryId) {
      params = params.set('categoryId', categoryId);
    }
    return this.http.get<Page<Budget>>('/api/v1/budgets', { params });
  }

  create(budget: NewBudget): Observable<Budget> {
    return this.http.post<Budget>('/api/v1/budgets', budget);
  }

  update(budgetId: string, changes: BudgetChanges): Observable<Budget> {
    return this.http.patch<Budget>(`/api/v1/budgets/${budgetId}`, changes);
  }

  delete(budgetId: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/budgets/${budgetId}`);
  }
}
