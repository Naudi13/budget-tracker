import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

/** The body of `GET /health` (`13-operations/observability.md`). */
export interface Health {
  readonly status: 'Healthy' | 'Degraded' | 'Unhealthy';
  readonly checks: Readonly<Record<string, string>>;
}

/** Same origin: `ng serve`'s proxy or the portal's nginx forwards `/health` to the API (ADR-006). */
@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);

  check(): Observable<Health> {
    return this.http.get<Health>('/health');
  }
}
