import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { App } from './app';
import { AuthTokenStore } from './core/auth-token.store';

describe('the navigation, by role', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ imports: [App], providers: [provideRouter([]), provideHttpClient()] });
  });

  function links(): (string | undefined)[] {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('nav a')).map((a) => a.textContent?.trim());
  }

  it('offers an administrator every screen', () => {
    TestBed.inject(AuthTokenStore).set('t.o.k', 'ADMIN');

    expect(links()).toEqual(['Panel', 'Movimientos', 'Categorías', 'Presupuestos', 'Usuarios']);
  });

  it('offers a user only the dashboard and their movements', () => {
    TestBed.inject(AuthTokenStore).set('t.o.k', 'USER');

    expect(links()).toEqual(['Panel', 'Movimientos']);
  });
});
