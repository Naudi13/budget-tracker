import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';

import { ApiError } from '../../core/api-error';
import { Role } from '../../core/auth-token.store';
import { User } from '../../core/identity.service';
import { AccountLink, USERS_PAGE_SIZE, UsersService } from '../../core/users.service';
import { emailShape, notBlank } from '../form-rules';

const ROLE_NAMES: Record<Role, string> = { USER: 'Usuario', ADMIN: 'Administrador' };
const STATUS_NAMES: Record<User['status'], string> = { PENDING: 'Pendiente', ACTIVE: 'Activa' };

type Field = 'name' | 'email' | 'role';

/** The link on screen, once: it cannot be shown again (ADR-010). */
interface ShownLink {
  readonly url: string;
  readonly name: string;
}

/** HU-09 and HU-10, 12-ux-ui/screens/users.md. */
@Component({
  selector: 'bt-users-page',
  imports: [ReactiveFormsModule],
  templateUrl: './users.page.html',
  styleUrl: './users.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(keydown.escape)': 'closeLink()' },
})
export class UsersPage {
  private readonly users = inject(UsersService);
  private readonly injector = inject(Injector);
  private readonly firstField = viewChild<ElementRef<HTMLInputElement>>('firstField');
  private readonly copyButton = viewChild<ElementRef<HTMLButtonElement>>('copyButton');

  protected readonly state = signal<'loading' | 'error' | 'forbidden' | 'ready'>('loading');
  protected readonly rows = signal<readonly User[]>([]);
  protected readonly page = signal(1);
  protected readonly total = signal(0);
  protected readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / USERS_PAGE_SIZE)));

  protected readonly formOpen = signal(false);
  protected readonly attempted = signal(false);
  protected readonly saving = signal(false);
  protected readonly emailTaken = signal(false);
  protected readonly failure = signal<string | null>(null);
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [notBlank, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.pattern(emailShape), Validators.maxLength(254)]],
    role: this.emptyRole(),
  });

  protected readonly link = signal<ShownLink | null>(null);
  protected readonly copied = signal(false);

  constructor() {
    this.load(1);
    this.form.controls.email.valueChanges.subscribe(() => this.emailTaken.set(false));
  }

  private emptyRole() {
    return inject(NonNullableFormBuilder).control<Role | ''>('', Validators.required);
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

  protected statusName(user: User): string {
    return STATUS_NAMES[user.status];
  }

  // --- New account ---------------------------------------------------------

  protected openForm(): void {
    this.failure.set(null);
    this.attempted.set(false);
    this.emailTaken.set(false);
    this.form.reset();
    this.formOpen.set(true);
    afterNextRender(() => this.firstField()?.nativeElement.focus(), { injector: this.injector });
  }

  protected cancelForm(): void {
    this.formOpen.set(false);
  }

  protected showError(field: Field): boolean {
    const control = this.form.controls[field];
    return control.invalid && (control.dirty || this.attempted());
  }

  protected nameMessage(): string {
    return this.form.controls.name.hasError('maxlength') ? 'Usa máximo 100 caracteres.' : 'Escribe el nombre.';
  }

  protected save(): void {
    this.attempted.set(true);
    this.failure.set(null);
    if (this.form.invalid) {
      return;
    }

    const { name, email, role } = this.form.getRawValue();
    this.saving.set(true);
    this.users.create({ name: name.trim(), email: email.trim(), role: role as Role }).subscribe({
      next: (created) => {
        this.saving.set(false);
        this.formOpen.set(false);
        this.load(1);
        this.show('activate', created.activation, created.user.name);
      },
      error: (error: ApiError) => {
        this.saving.set(false);
        if (error.status === 409) {
          this.emailTaken.set(true);
        } else {
          this.failure.set('No pudimos crear el usuario. Inténtalo de nuevo.');
        }
      },
    });
  }

  // --- Links ---------------------------------------------------------------

  protected generate(user: User): void {
    this.failure.set(null);
    const pending = user.status === 'PENDING';
    const request: Observable<AccountLink> = pending
      ? this.users.createActivationLink(user.id)
      : this.users.createPasswordReset(user.id);

    request.subscribe({
      next: (link) => this.show(pending ? 'activate' : 'reset-password', link, user.name),
      error: (error: ApiError) => {
        // The account changed meanwhile: the list catches up.
        if (error.status === 404 || error.status === 409) {
          this.load();
        } else {
          this.failure.set('No pudimos generar el enlace. Inténtalo de nuevo.');
        }
      },
    });
  }

  private show(path: 'activate' | 'reset-password', link: AccountLink, name: string): void {
    this.copied.set(false);
    this.link.set({ url: `${location.origin}/${path}#token=${link.token}`, name });
    afterNextRender(() => this.copyButton()?.nativeElement.focus(), { injector: this.injector });
  }

  protected copy(url: string): void {
    void navigator.clipboard.writeText(url).then(() => this.copied.set(true));
  }

  protected closeLink(): void {
    this.link.set(null);
  }
}
