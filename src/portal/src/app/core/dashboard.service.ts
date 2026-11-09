import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Transaction } from './transactions.service';

/** The schema of bud-tack-docs 07-api/contracts/openapi/dashboard.yaml. */
export interface Dashboard {
  readonly totalIncome: number;
  readonly totalExpenses: number;
  /** totalIncome minus totalExpenses, computed by the API on read (BR-03). */
  readonly netBalance: number;
  /** The five most recent transactions, newest first. */
  readonly recentTransactions: readonly Transaction[];
}

/** HU-08: the signed-in user's dashboard, read in one request. */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  get(): Observable<Dashboard> {
    return this.http.get<Dashboard>('/api/v1/dashboard');
  }
}
