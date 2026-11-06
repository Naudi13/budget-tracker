import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CurrencyPipe } from '@angular/common';
import { FormArray, FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { startWith } from 'rxjs';

import { ApiError } from '../../core/api-error';
import { CategoryType } from '../../core/categories.service';
import { SubcategoryOption, TransactionsService } from '../../core/transactions.service';
import { notBlank } from '../form-rules';
import { parseAmount, todayIso } from '../money';

type Line = FormGroup<{ subcategoryId: FormControl<string>; amount: FormControl<string> }>;

/** What the dashboard shows on return (dashboard.md, new-record.md). */
export interface DashboardState {
  readonly notice?: string;
}

const TYPE_WORD: Record<CategoryType, string> = { income: 'ingreso', expense: 'gasto' };

/** HU-04, 12-ux-ui/screens/new-record.md. */
@Component({
  selector: 'bt-new-record-page',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe],
  templateUrl: './new-record.page.html',
  styleUrl: './new-record.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewRecordPage {
  private readonly transactions = inject(TransactionsService);
  private readonly router = inject(Router);
  private readonly forms = inject(NonNullableFormBuilder);

  protected readonly optionsState = signal<'loading' | 'error' | 'ready'>('loading');
  /** Once the form has been shown it stays, whatever a later reload of the options finds. */
  protected readonly formShown = signal(false);
  protected readonly options = signal<readonly SubcategoryOption[]>([]);
  /** Option groups for the subcategory select: one per category. */
  protected readonly groups = computed(() => {
    const groups = new Map<string, SubcategoryOption[]>();
    for (const option of this.options()) {
      const label = `${option.categoryName} (${TYPE_WORD[option.type]})`;
      groups.set(label, [...(groups.get(label) ?? []), option]);
    }
    return [...groups.entries()].map(([label, options]) => ({ label, options }));
  });

  protected readonly form = this.forms.group({
    transactionDate: [todayIso(), Validators.required],
    description: ['', [notBlank, Validators.maxLength(200)]],
    lines: this.forms.array<Line>([this.newLine()]),
  });

  private readonly value = toSignal(this.form.valueChanges.pipe(startWith(this.form.getRawValue())), {
    requireSync: true,
  });

  /** The sum of the amounts typed so far, ignoring what is not yet a number. */
  protected readonly total = computed(() =>
    (this.value().lines ?? []).reduce((sum, line) => sum + (parseAmount(line.amount ?? '') ?? 0), 0),
  );

  protected readonly attempted = signal(false);
  protected readonly saving = signal(false);
  protected readonly failure = signal(false);
  /** Line messages the API sent back (400 or 422), when the browser had accepted the form. */
  protected readonly serverLineErrors = signal<Readonly<Record<number, string>>>({});

  constructor() {
    this.loadOptions();
    this.form.valueChanges.subscribe(() => this.serverLineErrors.set({}));
  }

  protected get lines(): FormArray<Line> {
    return this.form.controls.lines;
  }

  /**
   * Loads the subcategories to offer. A quiet reload (after a 422, when one of
   * them went inactive) keeps the form and what was typed on screen.
   */
  protected loadOptions(quiet = false): void {
    if (!quiet) {
      this.optionsState.set('loading');
    }
    this.transactions.subcategoryOptions().subscribe({
      next: (options) => {
        this.options.set(options);
        this.optionsState.set('ready');
        if (options.length > 0) {
          this.formShown.set(true);
        }
      },
      error: () => {
        if (!quiet) {
          this.optionsState.set('error');
        }
      },
    });
  }

  protected addLine(): void {
    this.lines.push(this.newLine());
  }

  protected removeLine(index: number): void {
    this.lines.removeAt(index);
  }

  // --- Messages (new-record.md, Validation rules) ----------------------------

  protected dateError(): string | null {
    const control = this.form.controls.transactionDate;
    return this.visible(control) && control.invalid ? 'Elige la fecha en que se movió el dinero.' : null;
  }

  protected descriptionError(): string | null {
    const control = this.form.controls.description;
    if (!this.visible(control) || control.valid) {
      return null;
    }
    return control.hasError('maxlength')
      ? 'Usa máximo 200 caracteres.'
      : "Describe el movimiento, por ejemplo 'Útiles de oficina'.";
  }

  protected subcategoryError(index: number): string | null {
    const server = this.serverLineErrors()[index];
    if (server) {
      return server;
    }
    const control = this.lines.at(index).controls.subcategoryId;
    if (this.visible(control) && !control.value) {
      return 'Elige una subcategoría. Las inactivas no aparecen.';
    }
    return this.mixedTypeError(index);
  }

  protected amountError(index: number): string | null {
    const control = this.lines.at(index).controls.amount;
    if (!this.visible(control)) {
      return null;
    }
    const amount = parseAmount(control.value);
    if (amount === null && /[.,]\d{3,}\s*$/.test(control.value)) {
      return 'Usa máximo dos decimales.';
    }
    return amount === null || amount <= 0 ? 'Escribe un monto mayor que cero.' : null;
  }

  /** BR-02: a line whose type differs from the first typed line's. */
  private mixedTypeError(index: number): string | null {
    const types = this.lines.controls.map((line) => this.typeOf(line.controls.subcategoryId.value));
    const first = types.find((type) => type !== null);
    const mine = types[index];
    if (!first || !mine || mine === first) {
      return null;
    }
    const other: CategoryType = first === 'income' ? 'expense' : 'income';
    return `Este movimiento ya tiene líneas de ${TYPE_WORD[first]}. Quítalas para agregar un ${TYPE_WORD[other]}, o guárdalas como otro movimiento.`;
  }

  private typeOf(subcategoryId: string): CategoryType | null {
    return this.options().find((option) => option.id === subcategoryId)?.type ?? null;
  }

  // --- Save --------------------------------------------------------------------

  protected save(): void {
    this.attempted.set(true);
    this.failure.set(false);
    const lineErrors = this.lines.controls.some(
      (_, i) => this.subcategoryError(i) !== null || this.amountError(i) !== null,
    );
    if (this.form.invalid || lineErrors) {
      return;
    }

    const { transactionDate, description, lines } = this.form.getRawValue();
    this.saving.set(true);
    this.transactions
      .record({
        transactionDate,
        description: description.trim(),
        details: lines.map((line) => ({ subcategoryId: line.subcategoryId, amount: parseAmount(line.amount)! })),
      })
      .subscribe({
        next: () => {
          const state: DashboardState = { notice: 'Movimiento guardado.' };
          void this.router.navigate(['/dashboard'], { state });
        },
        error: (error: ApiError) => {
          this.saving.set(false);
          const lineMessages = this.lineMessages(error);
          if (Object.keys(lineMessages).length > 0) {
            this.serverLineErrors.set(lineMessages);
            if (error.status === 422) {
              this.loadOptions(true);
            }
          } else {
            // The form is never cleared on an error (new-record.md).
            this.failure.set(true);
          }
        },
      });
  }

  /** Turns `details[i].subcategoryId` from a 400 or 422 into the line's own message. */
  private lineMessages(error: ApiError): Record<number, string> {
    if (error.status !== 400 && error.status !== 422) {
      return {};
    }
    const messages: Record<number, string> = {};
    for (const detail of error.details) {
      const match = /^details\[(\d+)\]\.subcategoryId$/.exec(detail.field);
      if (match) {
        messages[Number(match[1])] =
          error.status === 422 && detail.message.startsWith('is an')
            ? (this.mixedTypeError(Number(match[1])) ?? 'Todas las líneas deben ser de ingreso o todas de gasto.')
            : 'Elige una subcategoría. Las inactivas no aparecen.';
      }
    }
    return messages;
  }

  private visible(control: FormControl<string>): boolean {
    return control.dirty || this.attempted();
  }

  private newLine(): Line {
    return this.forms.group({
      subcategoryId: this.forms.control(''),
      amount: this.forms.control(''),
    });
  }
}
