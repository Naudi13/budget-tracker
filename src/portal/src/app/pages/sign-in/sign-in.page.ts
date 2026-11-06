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
import { emailShape } from '../form-rules';

type Failure = 'credentials' | 'unavailable' | null;

/** What sign-up hands over when it sends the user here (sign-up.md). */
export interface SignInState {
  readonly email?: string;
  readonly created?: boolean;
}

/** HU-01, 12-ux-ui/screens/sign-in.md. */
@Component({
  selector: 'bt-sign-in-page',
  imports: [ReactiveFormsModule, RouterLink],
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
  /** Errors show once a field is typed in or the form is submitted, not on blur (see SignUpPage). */
  protected readonly attempted = signal(false);
  protected readonly failure = signal<Failure>(null);
  protected readonly created: boolean;

  constructor() {
    // Empty state: the cursor starts in Email (sign-in.md).
    afterNextRender(() => this.first().nativeElement.focus());
    const state = (this.router.currentNavigation()?.extras.state ?? history.state ?? {}) as SignInState;
    this.created = state.created === true;
    if (state.email) {
      this.form.controls.email.setValue(state.email);
    }
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
        } else {
          this.failure.set('unavailable');
        }
      },
    });
  }
}
