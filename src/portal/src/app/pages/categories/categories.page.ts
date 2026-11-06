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
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin, map, of, switchMap } from 'rxjs';

import { ApiError } from '../../core/api-error';
import { CategoriesService, Category, CategoryType, Subcategory } from '../../core/categories.service';
import { notBlank } from '../form-rules';

/** A category with its subcategories, as the screen lists them. */
interface Group {
  readonly category: Category;
  readonly subcategories: readonly Subcategory[];
}

/** What a delete is about to remove. */
type Target =
  | { readonly kind: 'category'; readonly category: Category }
  | { readonly kind: 'subcategory'; readonly category: Category; readonly subcategory: Subcategory };

/** The delete flow of categories-configuration.md: confirm, then blocked (BR-05) or failed. */
interface Pending {
  readonly target: Target;
  readonly step: 'confirm' | 'blocked' | 'failed';
}

const TYPE_WORD: Record<CategoryType, string> = { income: 'ingreso', expense: 'gasto' };

/** HU-02 and HU-03, 12-ux-ui/screens/categories-configuration.md. */
@Component({
  selector: 'bt-categories-page',
  imports: [ReactiveFormsModule],
  templateUrl: './categories.page.html',
  styleUrl: './categories.page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(keydown.escape)': 'closeDialog()' },
})
export class CategoriesPage {
  private readonly categories = inject(CategoriesService);
  private readonly forms = inject(NonNullableFormBuilder);
  private readonly injector = inject(Injector);
  private readonly dialogAction = viewChild<ElementRef<HTMLButtonElement>>('dialogAction');
  private readonly newCategoryName = viewChild<ElementRef<HTMLInputElement>>('newCategoryName');
  private readonly heading = viewChild<ElementRef<HTMLHeadingElement>>('heading');

  protected readonly state = signal<'loading' | 'error' | 'ready'>('loading');
  protected readonly groups = signal<readonly Group[]>([]);
  protected readonly incomes = computed(() => this.groups().filter((g) => g.category.type === 'income'));
  protected readonly expenses = computed(() => this.groups().filter((g) => g.category.type === 'expense'));

  /** The success banner, and the error banner for a change that did not save. */
  protected readonly notice = signal<string | null>(null);
  protected readonly failure = signal<string | null>(null);

  // New category form.
  protected readonly creating = signal(false);
  protected readonly savingCategory = signal(false);
  protected readonly categoryAttempted = signal(false);
  protected readonly categoryConflict = signal<string | null>(null);
  protected readonly categoryForm = this.forms.group({
    name: ['', [notBlank, Validators.maxLength(100)]],
    type: this.forms.control<CategoryType | ''>('', Validators.required),
  });

  // One "new subcategory" field per category.
  private readonly subcategoryFields = new Map<string, FormControl<string>>();
  protected readonly subcategoryErrors = signal<Readonly<Record<string, string>>>({});
  protected readonly savingSubcategoryOf = signal<string | null>(null);

  protected readonly pending = signal<Pending | null>(null);
  protected readonly busy = signal(false);
  /** The button that opened the dialog: focus goes back to it when the dialog closes. */
  private opener: HTMLElement | null = null;

  constructor() {
    this.load();
    this.categoryForm.valueChanges.subscribe(() => this.categoryConflict.set(null));
  }

  protected load(): void {
    this.state.set('loading');
    this.categories
      .listAll()
      .pipe(
        switchMap((categories) =>
          categories.length === 0
            ? of([])
            : forkJoin(
                categories.map((category) =>
                  this.categories
                    .listAllSubcategories(category.id)
                    .pipe(map((subcategories): Group => ({ category, subcategories }))),
                ),
              ),
        ),
      )
      .subscribe({
        next: (groups) => {
          this.groups.set(groups);
          this.state.set('ready');
        },
        error: () => this.state.set('error'),
      });
  }

  // --- New category ----------------------------------------------------------

  protected openNewCategory(): void {
    this.creating.set(true);
    this.notice.set(null);
    afterNextRender(() => this.newCategoryName()?.nativeElement.focus(), { injector: this.injector });
  }

  protected cancelNewCategory(): void {
    this.creating.set(false);
    this.categoryAttempted.set(false);
    this.categoryForm.reset();
  }

  protected categoryError(field: 'name' | 'type'): string | null {
    const control = this.categoryForm.controls[field];
    if (!(control.dirty || this.categoryAttempted()) || control.valid) {
      return null;
    }
    if (field === 'type') {
      return 'Elige ingreso o gasto.';
    }
    return control.hasError('maxlength') ? 'Usa máximo 100 caracteres.' : 'Escribe un nombre.';
  }

  protected createCategory(): void {
    this.categoryAttempted.set(true);
    if (this.categoryForm.invalid) {
      return;
    }

    const { name, type } = this.categoryForm.getRawValue();
    this.savingCategory.set(true);
    this.categories.create(name.trim(), type as CategoryType).subscribe({
      next: (category) => {
        this.savingCategory.set(false);
        this.groups.update((groups) => sortGroups([...groups, { category, subcategories: [] }]));
        this.notice.set(`${category.name} creada.`);
        this.cancelNewCategory();
      },
      error: (error: ApiError) => {
        this.savingCategory.set(false);
        if (error.status === 409) {
          this.categoryConflict.set(`Ya existe una categoría de ${TYPE_WORD[type as CategoryType]} llamada ${name.trim()}.`);
        } else {
          this.failure.set('No pudimos crear la categoría. Inténtalo de nuevo.');
        }
      },
    });
  }

  // --- Subcategories ---------------------------------------------------------

  protected subcategoryField(categoryId: string): FormControl<string> {
    let field = this.subcategoryFields.get(categoryId);
    if (!field) {
      field = this.forms.control('', [notBlank, Validators.maxLength(100)]);
      field.valueChanges.subscribe(() => this.setSubcategoryError(categoryId, null));
      this.subcategoryFields.set(categoryId, field);
    }
    return field;
  }

  protected createSubcategory(category: Category): void {
    this.notice.set(null);
    const field = this.subcategoryField(category.id);
    if (field.invalid) {
      this.setSubcategoryError(
        category.id,
        field.hasError('maxlength') ? 'Usa máximo 100 caracteres.' : 'Escribe un nombre.',
      );
      return;
    }

    const name = field.value.trim();
    this.savingSubcategoryOf.set(category.id);
    this.categories.createSubcategory(category.id, name).subscribe({
      next: (subcategory) => {
        this.savingSubcategoryOf.set(null);
        this.updateGroup(category.id, (group) => ({
          ...group,
          subcategories: [...group.subcategories, subcategory].sort(byName),
        }));
        field.reset();
      },
      error: (error: ApiError) => {
        this.savingSubcategoryOf.set(null);
        if (error.status === 409) {
          this.setSubcategoryError(category.id, `Esta categoría ya tiene una subcategoría llamada ${name}.`);
        } else if (error.status === 404) {
          this.load();
        } else {
          this.failure.set('No pudimos crear la subcategoría. Inténtalo de nuevo.');
        }
      },
    });
  }

  // --- Activate or deactivate ------------------------------------------------

  protected toggle(target: Target): void {
    const isActive = !(target.kind === 'category' ? target.category.isActive : target.subcategory.isActive);
    this.setActive(target, isActive);
  }

  private setActive(target: Target, isActive: boolean, after?: () => void): void {
    this.notice.set(null);
    this.failure.set(null);
    const request =
      target.kind === 'category'
        ? this.categories.setActive(target.category.id, isActive).pipe(
            map((category) => (group: Group): Group => ({ ...group, category })),
          )
        : this.categories.setSubcategoryActive(target.category.id, target.subcategory.id, isActive).pipe(
            map((subcategory) => (group: Group): Group => ({
              ...group,
              subcategories: group.subcategories.map((s) => (s.id === subcategory.id ? subcategory : s)),
            })),
          );

    request.subscribe({
      next: (change) => {
        this.updateGroup(target.category.id, change);
        after?.();
      },
      error: (error: ApiError) => {
        after?.();
        if (error.status === 404) {
          this.load();
        } else {
          this.failure.set('No pudimos guardar el cambio. Inténtalo de nuevo.');
        }
      },
    });
  }

  // --- Delete ----------------------------------------------------------------

  protected askDelete(target: Target): void {
    this.notice.set(null);
    this.failure.set(null);
    this.opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    this.pending.set({ target, step: 'confirm' });
    this.focusDialog();
  }

  protected closeDialog(): void {
    if (!this.busy() && this.pending()) {
      this.finishDialog();
    }
  }

  /** Closes the dialog and puts focus back where it was, or on the heading if that row is gone. */
  private finishDialog(): void {
    this.pending.set(null);
    const opener = this.opener;
    this.opener = null;
    afterNextRender(
      () => (opener?.isConnected ? opener : this.heading()?.nativeElement)?.focus(),
      { injector: this.injector },
    );
  }

  protected confirmDelete(): void {
    const pending = this.pending();
    if (!pending) {
      return;
    }

    const { target } = pending;
    const request =
      target.kind === 'category'
        ? this.categories.delete(target.category.id)
        : this.categories.deleteSubcategory(target.category.id, target.subcategory.id);

    this.busy.set(true);
    request.subscribe({
      next: () => {
        this.busy.set(false);
        this.finishDialog();
        if (target.kind === 'category') {
          this.groups.update((groups) => groups.filter((g) => g.category.id !== target.category.id));
        } else {
          this.updateGroup(target.category.id, (group) => ({
            ...group,
            subcategories: group.subcategories.filter((s) => s.id !== target.subcategory.id),
          }));
        }
        this.notice.set(`${nameOf(target)} eliminada.`);
      },
      error: (error: ApiError) => {
        this.busy.set(false);
        if (error.status === 409) {
          this.pending.set({ target, step: 'blocked' });
          this.focusDialog();
        } else if (error.status === 404) {
          this.finishDialog();
          this.load();
        } else {
          this.pending.set({ target, step: 'failed' });
          this.focusDialog();
        }
      },
    });
  }

  /** BR-05: what is in use is deactivated instead of deleted. */
  protected deactivateInstead(): void {
    const pending = this.pending();
    if (!pending) {
      return;
    }

    this.busy.set(true);
    this.setActive(pending.target, false, () => {
      this.busy.set(false);
      this.finishDialog();
    });
  }

  protected nameOf(target: Target): string {
    return nameOf(target);
  }

  // --- Helpers ---------------------------------------------------------------

  private focusDialog(): void {
    afterNextRender(() => this.dialogAction()?.nativeElement.focus(), { injector: this.injector });
  }

  private updateGroup(categoryId: string, change: (group: Group) => Group): void {
    this.groups.update((groups) => groups.map((g) => (g.category.id === categoryId ? change(g) : g)));
  }

  private setSubcategoryError(categoryId: string, message: string | null): void {
    this.subcategoryErrors.update((errors) => {
      const next = { ...errors };
      if (message === null) {
        delete next[categoryId];
      } else {
        next[categoryId] = message;
      }
      return next;
    });
  }
}

function nameOf(target: Target): string {
  return target.kind === 'category' ? target.category.name : target.subcategory.name;
}

function byName(a: { name: string }, b: { name: string }): number {
  return a.name.localeCompare(b.name, 'es', { sensitivity: 'base' });
}

function sortGroups(groups: Group[]): Group[] {
  return groups.sort((a, b) => byName(a.category, b.category));
}
