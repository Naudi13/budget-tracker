import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

import { ApiError } from '../../core/api-error';
import { HealthService } from '../../core/health.service';

type State =
  | { readonly kind: 'loading' }
  | { readonly kind: 'healthy' }
  | { readonly kind: 'database-down' }
  | { readonly kind: 'unreachable'; readonly message: string };

/**
 * Sprint 0 landing page: proves the portal reaches the API through the
 * same-origin proxy. Sign-in (HU-01) replaces it as the first screen.
 */
@Component({
  selector: 'bt-status-page',
  templateUrl: './status.page.html',
  styleUrl: './status.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusPage {
  private readonly health = inject(HealthService);

  protected readonly state = signal<State>({ kind: 'loading' });

  constructor() {
    this.refresh();
  }

  protected refresh(): void {
    this.state.set({ kind: 'loading' });
    this.health.check().subscribe({
      next: (body) =>
        this.state.set(body.status === 'Healthy' ? { kind: 'healthy' } : { kind: 'database-down' }),
      error: (failure: ApiError) =>
        this.state.set(
          failure.status === 503
            ? { kind: 'database-down' }
            : { kind: 'unreachable', message: failure.message },
        ),
    });
  }
}
