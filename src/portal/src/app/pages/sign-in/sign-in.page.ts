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
import { Router } from '@angular/router';

import { ApiError } from '../../core/api-error';
import { IdentityService } from '../../core/identity.service';
import { emailShape } from '../form-rules';

type Failure = 'credentials' | 'attempts' | 'unavailable' | null;

/** What setting a password hands over when it sends the user here (activate-account.md). */
export interface SignInState {
  readonly passwordSaved?: boolean;
}

/** HU-01, 12-ux-ui/screens/sign-in.md. */
@Component({
  selector: 'bt-sign-in-page',
  imports: [ReactiveFormsModule],
  templateUrl: './sign-in.page.html',
  styleUrl: '../auth-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignInPage {
  private readonly identity = inject(IdentityService);
  private readonly router = inject(Router);

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.pattern(emailShape)]],
    password: ['', Validators.required],
  });

  private readonly first = viewChild.required<ElementRef<HTMLInputElement>>('first');

  protected readonly submitting = signal(false);
  /**
   * Errors show once a field is typed in or the form is submitted, not on
   * blur: a message appearing on blur pushes the button down under the
   * pointer, and the click that caused the blur misses it.
   */
  protected readonly attempted = signal(false);
  protected readonly failure = signal<Failure>(null);
  protected readonly passwordSaved: boolean;

  constructor() {
    // Empty state: the cursor starts in Email (sign-in.md).
    afterNextRender(() => this.first().nativeElement.focus());
    const state = (this.router.currentNavigation()?.extras.state ?? history.state ?? {}) as SignInState;
    this.passwordSaved = state.passwordSaved === true;
  }

  protected showError(field: 'email' | 'password'): boolean {
    const control = this.form.controls[field];
    return control.invalid && (control.dirty || this.attempted());
  }

  protected submit(): void {
    this.attempted.set(true);
    if (this.form.invalid) {
      return;
    }

    this.submitting.set(true);
    this.failure.set(null);
    this.identity.signIn(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigate(['/dashboard']),
      error: (error: ApiError) => {
        this.submitting.set(false);
        if (error.status === 401) {
          // Keep the email, clear the password (sign-in.md, Error state). The
          // banner already says what happened: no "enter your password" under it.
          this.form.controls.password.reset();
          this.attempted.set(false);
          this.failure.set('credentials');
        } else if (error.status === 429) {
          // Too many failures with this email (RNF-02): the wait is in minutes.
          this.form.controls.password.reset();
          this.attempted.set(false);
          this.failure.set('attempts');
        } else {
          this.failure.set('unavailable');
        }
      },
    });
  }
}
