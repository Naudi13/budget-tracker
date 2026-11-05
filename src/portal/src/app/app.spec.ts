import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { App } from './app';
import { routes } from './app.routes';
import { AuthTokenStore } from './core/auth-token.store';

describe('App', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  async function render(url: string): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl(url);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('sends a visitor without a token from the root to sign-in', async () => {
    const page = await render('/');

    expect(TestBed.inject(Router).url).toBe('/sign-in');
    expect(page.querySelector('main h1')?.textContent).toBe('Budget Tracker');
    const logo = page.querySelector('header a img') as HTMLImageElement;
    expect(logo.getAttribute('src')).toBe('brand/logo-reverse.png');
    expect(logo.alt).toBe('BudgetTrack');
    expect(page.querySelector('header button')).toBeNull();
  });

  it('sends a signed-in user from the root to the dashboard, with a sign-out button', async () => {
    TestBed.inject(AuthTokenStore).set('a.b.c');

    const page = await render('/');

    expect(TestBed.inject(Router).url).toBe('/dashboard');
    expect(page.querySelector('main h1')?.textContent).toBe('Dashboard');
    expect(page.querySelector('header button')?.textContent?.trim()).toBe('Sign out');
  });

  it('signs out by discarding the token and going to sign-in', async () => {
    TestBed.inject(AuthTokenStore).set('a.b.c');
    const page = await render('/dashboard');

    (page.querySelector('header button') as HTMLButtonElement).click();
    await TestBed.inject(Router).navigated;
    await new Promise((resolve) => setTimeout(resolve));

    expect(TestBed.inject(AuthTokenStore).token()).toBeNull();
    expect(TestBed.inject(Router).url).toBe('/sign-in');
  });

  it('keeps the status page open without a token', async () => {
    const page = await render('/status');

    expect(page.querySelector('main h1')?.textContent).toBe('System status');
    TestBed.inject(HttpTestingController).expectOne('/health');
  });

  it('shows the not-found page for an unknown path', async () => {
    const page = await render('/no/such/page');

    expect(page.querySelector('main h1')?.textContent).toBe('Page not found');
  });
});
