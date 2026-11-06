import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { ApiError } from '../../core/api-error';
import { IdentityService } from '../../core/identity.service';
import { emailShape, notBlank, passwordsMatch } from '../form-rules';
import { SignInState } from '../sign-in/sign-in.page';

type Field = 'name' | 'email' | 'password' | 'repeatPassword';

/** HU-01, 12-ux-ui/screens/sign-up.md. */
@Component({
  selector: 'bt-sign-up-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './sign-up.page.html',
  styleUrl: '../auth-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignUpPage {
  private readonly identity = inject(IdentityService);
  private readonly router = inject(Router);

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      name: ['', [notBlank, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.pattern(emailShape), Validators.maxLength(254)]],
      password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
      repeatPassword: [''],
    },
    { validators: passwordsMatch },
  );

  private readonly first = viewChild.required<ElementRef<HTMLInputElement>>('first');

  protected readonly submitting = signal(false);
  /**
   * Errors show once a field is typed in or the form is submitted, not on
   * blur: a message appearing on blur pushes the button down under the
   * pointer, and the click that caused the blur misses it.
   */
  protected readonly attempted = signal(false);
  /** 409: shown inline under Email (sign-up.md, Error state). */
  protected readonly emailInUse = signal(false);
  protected readonly unavailable = signal(false);
  /**
   * Fields the API refused with a 400 although the browser accepted them. The
   * screen shows its own Spanish message for the field, never the API's text.
   */
  protected readonly serverErrors = signal<ReadonlySet<Field>>(new Set());

  constructor() {
    // Empty state: the cursor starts in Name (sign-up.md).
    afterNextRender(() => this.first().nativeElement.focus());
    this.form.controls.email.valueChanges.subscribe(() => this.emailInUse.set(false));
    this.form.valueChanges.subscribe(() => this.serverErrors.set(new Set()));
  }

  protected showError(field: Field): boolean {
    const control = this.form.controls[field];
    const visited = control.dirty || this.attempted();
    if (field === 'repeatPassword') {
      return visited && this.form.hasError('mismatch');
    }
    return (visited && control.invalid) || this.serverErrors().has(field);
  }

  /** What to fix, in the user's words (Spanish), for a field with an error. */
  protected message(field: 'name' | 'email' | 'password'): string {
    const errors = this.form.controls[field].errors;
    switch (field) {
      case 'name':
        return errors?.['maxlength'] ? 'Usa máximo 100 caracteres.' : 'Escribe tu nombre o el de tu negocio.';
      case 'email':
        return 'Escribe un correo válido, por ejemplo ana@example.com.';
      case 'password':
        if (errors?.['maxlength']) {
          return 'Usa máximo 128 caracteres.';
        }
        return errors ? 'Usa al menos 8 caracteres.' : 'Usa entre 8 y 128 caracteres.';
    }
  }

  protected submit(): void {
    this.attempted.set(true);
    if (this.form.invalid) {
      return;
    }

    const { name, email, password } = this.form.getRawValue();
    this.submitting.set(true);
    this.unavailable.set(false);
    this.emailInUse.set(false);
    this.serverErrors.set(new Set());
    this.identity.signUp({ name, email, password }).subscribe({
      next: (user) => {
        const state: SignInState = { email: user.email, created: true };
        void this.router.navigate(['/sign-in'], { state });
      },
      error: (error: ApiError) => {
        // The form is never cleared on an error (sign-up.md).
        this.submitting.set(false);
        if (error.status === 409) {
          this.emailInUse.set(true);
        } else if (error.status === 400 && error.details.length > 0) {
          this.serverErrors.set(new Set(error.details.map((detail) => detail.field as Field)));
        } else {
          this.unavailable.set(true);
        }
      },
    });
  }
}
