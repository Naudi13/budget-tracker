import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(() => {
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
    return fixture.nativeElement as HTMLElement;
  }

  it('shows the header and the status page at the root', async () => {
    const page = await render('/');

    expect(page.querySelector('header')?.textContent).toContain('Budget Tracker');
    expect(page.querySelector('main h1')?.textContent).toBe('System status');
    TestBed.inject(HttpTestingController).expectOne('/health');
  });

  it('shows the not-found page for an unknown path', async () => {
    const page = await render('/no/such/page');

    expect(page.querySelector('main h1')?.textContent).toBe('Page not found');
  });
});
