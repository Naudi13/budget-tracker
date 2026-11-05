import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'bt-not-found-page',
  imports: [RouterLink],
  template: `
    <h1>Page not found</h1>
    <p>The page you asked for does not exist.</p>
    <a routerLink="/">Go to the start page</a>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundPage {}
