import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Transaction } from './transactions.service';

/** How spending compares with the budgets in force today (BR-08). */
export interface BudgetProgress {
  readonly amount: number;
  readonly spent: number;
  /** Negative when over budget. */
  readonly available: number;
}

/** The schema of bud-tack-docs 07-api/contracts/openapi/dashboard.yaml. */
export interface Dashboard {
  readonly totalIncome: number;
  readonly totalExpenses: number;
  /** totalIncome minus totalExpenses, computed by the API on read (BR-03). */
  readonly netBalance: number;
  /** Null when no budget is in force today. */
  readonly budget: BudgetProgress | null;
  /** The five most recent transactions, newest first. */
  readonly recentTransactions: readonly Transaction[];
}

/** HU-08 and HU-11: the dashboard, global or for one category, read in one request. */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  get(categoryId: string | null = null): Observable<Dashboard> {
    const params = categoryId ? new HttpParams().set('categoryId', categoryId) : undefined;
    return this.http.get<Dashboard>('/api/v1/dashboard', { params });
  }
}
