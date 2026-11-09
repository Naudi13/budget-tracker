import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { CategoriesService, Category } from '../../core/categories.service';
import { Dashboard, DashboardService } from '../../core/dashboard.service';
import { Transaction, totalOf } from '../../core/transactions.service';
import { DashboardState } from '../new-record/new-record.page';
import { IdentityService } from '../../core/identity.service';

/**
 * HU-08 and HU-11, 12-ux-ui/screens/dashboard.md: the three KPIs, the budget
 * band and the five most recent transactions, read in one request, globally or
 * for one category, and the entry point to Nuevo
 * registro, Historial and Configuración. Where sign-in lands (HU-01).
 */
@Component({
  selector: 'bt-dashboard-page',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {
  private readonly dashboards = inject(DashboardService);
  private readonly categories = inject(CategoriesService);
  protected readonly identity = inject(IdentityService);

  protected readonly state = signal<'loading' | 'error' | 'ready'>('loading');
  protected readonly dashboard = signal<Dashboard | null>(null);
  /** "Movimiento guardado." after a new record (new-record.md, Populated state). */
  protected readonly notice: string | null;
  protected readonly totalOf = totalOf;
  /** Empty means every category. */
  protected readonly filter = new FormControl('', { nonNullable: true });
  protected readonly options = signal<readonly Category[]>([]);

  constructor() {
    const state = (inject(Router).currentNavigation()?.extras.state ?? history.state ?? {}) as DashboardState;
    this.notice = state.notice ?? null;
    this.load();
    this.filter.valueChanges.subscribe(() => this.load());
    // Without the list the dashboard still works, globally.
    this.categories.listAll().subscribe({ next: (all) => this.options.set(all.filter((c) => c.isActive)) });
  }

  protected load(): void {
    this.state.set('loading');
    this.dashboards.get(this.filter.value || null).subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
        this.state.set('ready');
      },
      // No partial figures are shown (dashboard.md, Error state).
      error: () => {
        this.dashboard.set(null);
        this.state.set('error');
      },
    });
  }

  protected typeOf(row: Transaction): string {
    return row.details[0]?.type === 'income' ? 'Ingreso' : 'Gasto';
  }
}
