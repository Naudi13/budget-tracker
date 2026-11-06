import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { IdentityService } from './core/identity.service';

/** The layout every screen shares: a header and one main region. */
@Component({
  selector: 'bt-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly identity = inject(IdentityService);
  private readonly router = inject(Router);

  /** Client-side only: the token is discarded (HU-01, no DELETE /sessions in V1). */
  protected signOut(): void {
    this.identity.signOut();
    void this.router.navigate(['/sign-in']);
  }
}
