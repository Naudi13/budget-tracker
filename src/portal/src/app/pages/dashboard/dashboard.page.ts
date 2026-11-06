import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { DashboardState } from '../new-record/new-record.page';

/**
 * Where sign-in lands (HU-01) and the entry point to the rest (dashboard.md):
 * Nuevo registro, Historial and Configuración. The KPI tiles and the recent
 * list arrive with HU-08.
 */
@Component({
  selector: 'bt-dashboard-page',
  imports: [RouterLink],
  template: `
    <h1>Panel</h1>
    @if (notice) {
      <p class="banner" role="status">{{ notice }}</p>
    }
    <p>Aquí verás tus ingresos, tus gastos y tus movimientos recientes.</p>
    <nav class="shortcuts" aria-label="Accesos">
      <a class="btn-primary" routerLink="/transactions/new">Registrar un movimiento</a>
      <a routerLink="/transactions">Ver el historial</a>
      <a routerLink="/settings/categories">Configurar tus categorías</a>
    </nav>
  `,
  styles: `
    .banner {
      padding: calc(var(--space-unit) * 3) calc(var(--space-unit) * 4);
      border: 1px solid currentColor;
      border-radius: var(--space-unit);
      color: var(--color-success);
      background: #eaf8f6;
    }
    .shortcuts {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: calc(var(--space-unit) * 5);
    }
    .btn-primary {
      text-decoration: none;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {
  /** "Movimiento guardado." after a new record (new-record.md, Populated state). */
  protected readonly notice: string | null;

  constructor() {
    const state = (inject(Router).currentNavigation()?.extras.state ?? history.state ?? {}) as DashboardState;
    this.notice = state.notice ?? null;
  }
}
