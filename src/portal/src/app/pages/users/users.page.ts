import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';

import { ApiError } from '../../core/api-error';
import { Role } from '../../core/auth-token.store';
import { User } from '../../core/identity.service';
import { USERS_PAGE_SIZE, UsersService } from '../../core/users.service';

const ROLE_NAMES: Record<Role, string> = { USER: 'Usuario', ADMIN: 'Administrador' };

/** HU-09, 12-ux-ui/screens/users.md. It only reads. */
@Component({
  selector: 'bt-users-page',
  templateUrl: './users.page.html',
  styleUrl: './users.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersPage {
  private readonly users = inject(UsersService);

  protected readonly state = signal<'loading' | 'error' | 'forbidden' | 'ready'>('loading');
  protected readonly rows = signal<readonly User[]>([]);
  protected readonly page = signal(1);
  protected readonly total = signal(0);
  protected readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / USERS_PAGE_SIZE)));

  constructor() {
    this.load(1);
  }

  protected load(page = this.page()): void {
    this.state.set('loading');
    this.users.list(page).subscribe({
      next: (result) => {
        this.rows.set(result.data);
        this.page.set(result.meta.page);
        this.total.set(result.meta.total);
        this.state.set('ready');
      },
      error: (error: ApiError) => this.state.set(error.status === 403 ? 'forbidden' : 'error'),
    });
  }

  protected roleName(user: User): string {
    return ROLE_NAMES[user.role];
  }
}
