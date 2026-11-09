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
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { ApiError } from '../../core/api-error';
import { IdentityService } from '../../core/identity.service';
import { HISTORY_PAGE_SIZE, Transaction, TransactionsService, totalOf } from '../../core/transactions.service';

/** HU-05 and HU-06, 12-ux-ui/screens/transaction-history.md. */
@Component({
  selector: 'bt-history-page',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, CurrencyPipe],
  templateUrl: './history.page.html',
  styleUrl: './history.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(keydown.escape)': 'closeDialog()' },
})
export class HistoryPage {
  private readonly transactions = inject(TransactionsService);
  private readonly injector = inject(Injector);
  private readonly reasonField = viewChild<ElementRef<HTMLTextAreaElement>>('reasonField');
  private readonly heading = viewChild<ElementRef<HTMLHeadingElement>>('heading');

  protected readonly state = signal<'loading' | 'error' | 'ready'>('loading');
  protected readonly rows = signal<readonly Transaction[]>([]);
  protected readonly page = signal(1);
  protected readonly total = signal(0);
  protected readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / HISTORY_PAGE_SIZE)));
  protected readonly notice = signal<string | null>(null);

  protected readonly pending = signal<{ readonly row: Transaction; readonly failed: boolean } | null>(null);
  protected readonly busy = signal(false);
  private opener: HTMLElement | null = null;

  protected readonly totalOf = totalOf;
  protected readonly identity = inject(IdentityService);

  /** ADMIN only: the transactions in use, or the deleted ones with who, when and why. */
  protected readonly view = new FormControl<'active' | 'deleted'>('active', { nonNullable: true });
  protected readonly showingDeleted = signal(false);

  protected readonly reason = inject(NonNullableFormBuilder).control('', [
    Validators.required,
    (control) => (control.value.trim().length >= 3 ? null : { short: true }),
    Validators.maxLength(200),
  ]);
  protected readonly reasonAttempted = signal(false);

  constructor() {
    this.load(1);
    this.view.valueChanges.subscribe((view) => {
      this.notice.set(null);
      this.showingDeleted.set(view === 'deleted');
      this.load(1);
    });
  }

  protected reasonError(): string | null {
    if (!(this.reason.dirty || this.reasonAttempted()) || this.reason.valid) {
      return null;
    }
    return this.reason.hasError('maxlength') ? 'Usa máximo 200 caracteres.' : 'Escribe la razón (mínimo 3 caracteres).';
  }

  protected load(page: number): void {
    this.state.set('loading');
    this.transactions.list(page, this.showingDeleted()).subscribe({
      next: (result) => {
        // A page emptied by a delete: step back to the last page that has rows.
        if (result.data.length === 0 && result.meta.total > 0 && page > 1) {
          this.load(Math.ceil(result.meta.total / HISTORY_PAGE_SIZE));
          return;
        }
        this.rows.set(result.data);
        this.total.set(result.meta.total);
        this.page.set(result.meta.page);
        this.state.set('ready');
      },
      // The previous page is not shown as current (transaction-history.md).
      error: () => this.state.set('error'),
    });
  }

  protected typeOf(row: Transaction): string {
    return row.details[0]?.type === 'income' ? 'Ingreso' : 'Gasto';
  }

  protected askDelete(row: Transaction): void {
    this.notice.set(null);
    this.opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    this.pending.set({ row, failed: false });
    this.reason.reset('');
    this.reasonAttempted.set(false);
    afterNextRender(() => this.reasonField()?.nativeElement.focus(), { injector: this.injector });
  }

  protected closeDialog(): void {
    if (this.busy() || !this.pending()) {
      return;
    }
    this.pending.set(null);
    const opener = this.opener;
    this.opener = null;
    afterNextRender(() => opener?.focus(), { injector: this.injector });
  }

  protected confirmDelete(): void {
    const pending = this.pending();
    if (!pending) {
      return;
    }

    this.reasonAttempted.set(true);
    if (this.reason.invalid) {
      return;
    }

    this.busy.set(true);
    this.transactions.delete(pending.row.id, this.reason.value.trim()).subscribe({
      next: () => this.deleted(),
      error: (error: ApiError) => {
        // Already gone (deleted elsewhere): the list just catches up.
        if (error.status === 404) {
          this.deleted();
        } else {
          this.busy.set(false);
          this.pending.set({ row: pending.row, failed: true });
        }
      },
    });
  }

  private deleted(): void {
    this.busy.set(false);
    this.pending.set(null);
    this.opener = null;
    this.notice.set('Movimiento eliminado.');
    this.load(this.page());
    afterNextRender(() => this.heading()?.nativeElement.focus(), { injector: this.injector });
  }
}
