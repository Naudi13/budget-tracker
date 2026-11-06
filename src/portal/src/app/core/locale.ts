import { registerLocaleData } from '@angular/common';
import localeEsCo from '@angular/common/locales/es-CO';
import { LOCALE_ID, Provider } from '@angular/core';

/** The user sees Spanish (Colombia): dates, numbers and money in es-CO (design-system.md, Language). */
export function provideSpanishLocale(): Provider[] {
  registerLocaleData(localeEsCo);
  return [{ provide: LOCALE_ID, useValue: 'es-CO' }];
}
