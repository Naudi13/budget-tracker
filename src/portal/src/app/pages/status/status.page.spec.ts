import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { errorInterceptor } from '../../core/error.interceptor';
import { StatusPage } from './status.page';

describe('StatusPage', () => {
  let fixture: ComponentFixture<StatusPage>;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [StatusPage],
      providers: [provideHttpClient(withInterceptors([errorInterceptor])), provideHttpClientTesting()],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(StatusPage);
  });

  afterEach(() => backend.verify());

  function text(): string {
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  function button(): HTMLButtonElement {
    return (fixture.nativeElement as HTMLElement).querySelector('button')!;
  }

  it('says it is checking while /health is in flight', () => {
    expect(text()).toContain('Checking the API');
    expect(button().disabled).toBe(true);
    backend.expectOne('/health').flush({ status: 'Healthy', checks: { database: 'Healthy' } });
  });

  it('reports a healthy API', () => {
    backend.expectOne('/health').flush({ status: 'Healthy', checks: { database: 'Healthy' } });

    expect(text()).toContain('The API and its database are up.');
    expect(button().disabled).toBe(false);
  });

  it('reports an API that cannot reach its database (503)', () => {
    backend
      .expectOne('/health')
      .flush({ status: 'Unhealthy', checks: { database: 'Unhealthy' } }, { status: 503, statusText: 'Unavailable' });

    expect(text()).toContain('cannot reach its database');
  });

  it('reports a degraded answer as a database problem', () => {
    backend.expectOne('/health').flush({ status: 'Degraded', checks: { database: 'Degraded' } });

    expect(text()).toContain('cannot reach its database');
  });

  it('reports an API that does not answer', () => {
    backend.expectOne('/health').error(new ProgressEvent('error'));

    expect(text()).toContain('The API did not answer');
  });

  it('checks again on demand', () => {
    backend.expectOne('/health').error(new ProgressEvent('error'));
    text();

    button().click();
    backend.expectOne('/health').flush({ status: 'Healthy', checks: { database: 'Healthy' } });

    expect(text()).toContain('The API and its database are up.');
  });
});
