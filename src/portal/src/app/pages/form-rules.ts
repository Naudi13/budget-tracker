import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * The same email shape the API accepts (BudTack.Identity EmailAddress): one
 * `@`, something on both sides, a dot in the domain, no spaces. The API stays
 * the authority; this only spares a round trip.
 */
export const emailShape = /^\s*[^@\s]+@[^@\s]+\.[^@\s]+\s*$/;

/** Not only spaces (the API trims the name). */
export const notBlank: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && control.value.trim().length > 0 ? null : { blank: true };

/**
 * On the form group: `repeatPassword` equals `password`. Checked in the browser
 * only (sign-up.md); re-runs whenever either field changes.
 */
export const passwordsMatch: ValidatorFn = (group: AbstractControl): ValidationErrors | null =>
  group.get('password')?.value === group.get('repeatPassword')?.value ? null : { mismatch: true };
