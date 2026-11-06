import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Where sign-in lands (HU-01). The KPI tiles and the recent list arrive with
 * HU-08 (12-ux-ui/screens/dashboard.md); until then it only says so.
 */
@Component({
  selector: 'bt-dashboard-page',
  template: `
    <h1>Panel</h1>
    <p>Ya iniciaste sesión. Aquí verás tus ingresos, tus gastos y tus movimientos recientes.</p>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {}
