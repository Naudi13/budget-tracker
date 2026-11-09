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
import { ActivatedRoute, Router } from '@angular/router';

import { ApiError } from '../../core/api-error';
import { UsersService } from '../../core/users.service';
import { passwordsMatch } from '../form-rules';
import { SignInState } from '../sign-in/sign-in.page';

type Failure = 'link' | 'unavailable' | null;

/** HU-10, 12-ux-ui/screens/activate-account.md: one screen for activating and for resetting. */
@Component({
  selector: 'bt-activate-page',
  imports: [ReactiveFormsModule],
  templateUrl: './activate.page.html',
  styleUrl: '../auth-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivatePage {
  private readonly users = inject(UsersService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  /** After `#` in the link, so it never reaches a server log (ADR-010). */
  private readonly token = new URLSearchParams(this.route.snapshot.fragment ?? '').get('token');

  protected readonly title = this.route.snapshot.data['mode'] === 'reset' ? 'Restablece tu contraseña' : 'Activa tu cuenta';

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
      repeatPassword: [''],
    },
    { validators: passwordsMatch },
  );

  private readonly first = viewChild<ElementRef<HTMLInputElement>>('first');

  protected readonly submitting = signal(false);
  protected readonly attempted = signal(false);
  protected readonly failure = signal<Failure>(this.token ? null : 'link');

  constructor() {
    afterNextRender(() => this.first()?.nativeElement.focus());
  }

  protected showError(field: 'password' | 'repeatPassword'): boolean {
    const control = this.form.controls[field];
    const visited = control.dirty || this.attempted();
    return field === 'repeatPassword' ? visited && this.form.hasError('mismatch') : visited && control.invalid;
  }

  protected passwordMessage(): string {
    return this.form.controls.password.hasError('maxlength') ? 'Usa máximo 128 caracteres.' : 'Usa al menos 8 caracteres.';
  }

  protected submit(): void {
    this.attempted.set(true);
    if (this.form.invalid || !this.token) {
      return;
    }

    this.submitting.set(true);
    this.failure.set(null);
    this.users.confirmLink(this.token, this.form.getRawValue().password).subscribe({
      next: () => {
        const state: SignInState = { passwordSaved: true };
        void this.router.navigate(['/sign-in'], { state });
      },
      error: (error: ApiError) => {
        this.submitting.set(false);
        this.failure.set(error.status === 400 && error.details.some((d) => d.field === 'token') ? 'link' : 'unavailable');
      },
    });
  }
}
