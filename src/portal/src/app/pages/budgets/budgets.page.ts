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
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { ApiError } from '../../core/api-error';
import { BUDGETS_PAGE_SIZE, Budget, BudgetChanges, BudgetsService, NewBudget } from '../../core/budgets.service';
import { CategoriesService, Category } from '../../core/categories.service';
import { parseAmount } from '../money';

type Field = 'categoryId' | 'amount' | 'periodStart' | 'periodEnd';

/** The messages of budgets.md, Validation rules. */
const MESSAGES: Record<Field, string> = {
  categoryId: 'Elige una categoría. Las inactivas no aparecen.',
  amount: 'Escribe un monto mayor que cero.',
  periodStart: 'Elige el primer día del periodo.',
  periodEnd: 'La fecha final debe ser posterior a la inicial.',
};

/** HU-07, 12-ux-ui/screens/budgets.md. */
@Component({
  selector: 'bt-budgets-page',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './budgets.page.html',
  styleUrl: './budgets.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(keydown.escape)': 'closeDialog()' },
})
export class BudgetsPage {
  private readonly budgets = inject(BudgetsService);
  private readonly categories = inject(CategoriesService);
  private readonly forms = inject(NonNullableFormBuilder);
  private readonly injector = inject(Injector);
  private readonly heading = viewChild<ElementRef<HTMLHeadingElement>>('heading');
  private readonly firstField = viewChild<ElementRef<HTMLSelectElement>>('firstField');
  private readonly dialogAction = viewChild<ElementRef<HTMLButtonElement>>('dialogAction');

  protected readonly state = signal<'loading' | 'error' | 'ready'>('loading');
  protected readonly rows = signal<readonly Budget[]>([]);
  protected readonly page = signal(1);
  protected readonly total = signal(0);
  protected readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / BUDGETS_PAGE_SIZE)));

  /** Every category, inactive ones too: a budget keeps its category's name. */
  protected readonly allCategories = signal<readonly Category[]>([]);
  /** BR-05: only active categories can get a budget. */
  protected readonly activeCategories = computed(() => this.allCategories().filter((c) => c.isActive));
  protected readonly filter = new FormControl('', { nonNullable: true });

  protected readonly notice = signal<string | null>(null);

  // The form: new (`editing` null) or edit (`editing` the budget).
  protected readonly formOpen = signal(false);
  protected readonly editing = signal<Budget | null>(null);
  protected readonly attempted = signal(false);
  protected readonly saving = signal(false);
  protected readonly failure = signal(false);
  /** Messages from a 400 or 422 the browser had not caught. */
  protected readonly serverErrors = signal<Partial<Record<Field, string>>>({});
  protected readonly form = this.forms.group({
    categoryId: '',
    amount: '',
    periodStart: '',
    periodEnd: '',
  });

  protected readonly pending = signal<{ readonly budget: Budget; readonly failed: boolean } | null>(null);
  protected readonly busy = signal(false);
  private opener: HTMLElement | null = null;

  constructor() {
    this.load(1);
    this.filter.valueChanges.subscribe(() => this.loadBudgets(1));
    this.form.valueChanges.subscribe(() => this.serverErrors.set({}));
  }

  /** Categories and the first page together, so every row has its category's name. */
  protected load(page = this.page()): void {
    this.state.set('loading');
    forkJoin([this.categories.listAll(), this.budgets.list(page, this.filter.value || null)]).subscribe({
      next: ([categories, result]) => {
        this.allCategories.set(categories);
        this.show(result.data, result.meta.page, result.meta.total);
      },
      error: () => this.state.set('error'),
    });
  }

  protected loadBudgets(page: number): void {
    this.state.set('loading');
    this.budgets.list(page, this.filter.value || null).subscribe({
      next: (result) => {
        // A page emptied by a delete: step back to the last page that has rows.
        if (result.data.length === 0 && result.meta.total > 0 && page > 1) {
          this.loadBudgets(Math.ceil(result.meta.total / BUDGETS_PAGE_SIZE));
          return;
        }
        this.show(result.data, result.meta.page, result.meta.total);
      },
      error: () => this.state.set('error'),
    });
  }

  private show(rows: readonly Budget[], page: number, total: number): void {
    this.rows.set(rows);
    this.page.set(page);
    this.total.set(total);
    this.state.set('ready');
  }

  protected categoryName(categoryId: string): string {
    return this.allCategories().find((c) => c.id === categoryId)?.name ?? 'Categoría eliminada';
  }

  // --- Form --------------------------------------------------------------------

  protected openNew(): void {
    this.openForm(null);
  }

  protected openEdit(budget: Budget): void {
    this.openForm(budget);
  }

  private openForm(budget: Budget | null): void {
    this.notice.set(null);
    this.editing.set(budget);
    this.attempted.set(false);
    this.failure.set(false);
    this.form.reset({
      // An inactive category is not offered: the user has to choose another (BR-05).
      categoryId: budget && this.activeCategories().some((c) => c.id === budget.categoryId) ? budget.categoryId : '',
      amount: budget ? amountText(budget.amount) : '',
      periodStart: budget?.periodStart ?? '',
      periodEnd: budget?.periodEnd ?? '',
    });
    this.serverErrors.set({});
    this.formOpen.set(true);
    afterNextRender(() => this.firstField()?.nativeElement.focus(), { injector: this.injector });
  }

  protected cancelForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
    afterNextRender(() => this.heading()?.nativeElement.focus(), { injector: this.injector });
  }

  protected error(field: Field): string | null {
    const server = this.serverErrors()[field];
    if (server) {
      return server;
    }
    const control = this.form.controls[field];
    if (!(control.dirty || this.attempted())) {
      return null;
    }
    const { categoryId, amount, periodStart, periodEnd } = this.form.getRawValue();
    const wrong: Record<Field, boolean> = {
      categoryId: !categoryId,
      amount: (parseAmount(amount) ?? 0) <= 0,
      periodStart: !periodStart,
      // ISO dates compare as text.
      periodEnd: !periodEnd || (periodStart !== '' && periodEnd <= periodStart),
    };
    return wrong[field] ? MESSAGES[field] : null;
  }

  protected save(): void {
    this.attempted.set(true);
    this.failure.set(false);
    const fields: Field[] = ['categoryId', 'amount', 'periodStart', 'periodEnd'];
    if (fields.some((field) => this.error(field) !== null)) {
      return;
    }

    const value = this.form.getRawValue();
    const budget: NewBudget = { ...value, amount: parseAmount(value.amount)! };
    const editing = this.editing();
    const request = editing ? this.budgets.update(editing.id, changesOf(editing, budget)) : this.budgets.create(budget);

    this.saving.set(true);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.cancelForm();
        this.notice.set('Presupuesto guardado.');
        this.loadBudgets(editing ? this.page() : 1);
      },
      error: (error: ApiError) => {
        this.saving.set(false);
        const messages = fieldMessages(error);
        if (Object.keys(messages).length > 0) {
          this.serverErrors.set(messages);
          // The category went inactive meanwhile: stop offering it.
          if (messages.categoryId) {
            this.categories.listAll().subscribe({ next: (categories) => this.allCategories.set(categories) });
          }
        } else if (error.status === 404) {
          this.cancelForm();
          this.loadBudgets(this.page());
        } else {
          // The form keeps what was typed (budgets.md, States).
          this.failure.set(true);
        }
      },
    });
  }

  // --- Delete ----------------------------------------------------------------

  protected askDelete(budget: Budget): void {
    this.notice.set(null);
    this.opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    this.pending.set({ budget, failed: false });
    afterNextRender(() => this.dialogAction()?.nativeElement.focus(), { injector: this.injector });
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

    this.busy.set(true);
    this.budgets.delete(pending.budget.id).subscribe({
      next: () => this.deleted(),
      error: (error: ApiError) => {
        // Already gone: the list just catches up.
        if (error.status === 404) {
          this.deleted();
        } else {
          this.busy.set(false);
          this.pending.set({ budget: pending.budget, failed: true });
        }
      },
    });
  }

  private deleted(): void {
    this.busy.set(false);
    this.pending.set(null);
    this.opener = null;
    this.notice.set('Presupuesto eliminado.');
    this.loadBudgets(this.page());
    afterNextRender(() => this.heading()?.nativeElement.focus(), { injector: this.injector });
  }
}

/** An amount as the user would type it back: "500000", "85,50". */
function amountText(amount: number): string {
  return Number.isInteger(amount) ? String(amount) : amount.toFixed(2).replace('.', ',');
}

/** Only what differs from the stored budget (BR-06: PATCH changes the fields sent). */
function changesOf(current: Budget, next: NewBudget): BudgetChanges {
  const changes: BudgetChanges = {
    ...(next.categoryId !== current.categoryId ? { categoryId: next.categoryId } : {}),
    ...(next.amount !== current.amount ? { amount: next.amount } : {}),
    ...(next.periodStart !== current.periodStart ? { periodStart: next.periodStart } : {}),
    ...(next.periodEnd !== current.periodEnd ? { periodEnd: next.periodEnd } : {}),
  };
  // Nothing changed: send the amount, so the API still answers with the budget.
  return Object.keys(changes).length > 0 ? changes : { amount: next.amount };
}

/** The fields a 400 or 422 points at, each with the screen's own message. */
function fieldMessages(error: ApiError): Partial<Record<Field, string>> {
  if (error.status !== 400 && error.status !== 422) {
    return {};
  }
  const messages: Partial<Record<Field, string>> = {};
  for (const detail of error.details) {
    if (detail.field in MESSAGES) {
      messages[detail.field as Field] = MESSAGES[detail.field as Field];
    }
  }
  return messages;
}
