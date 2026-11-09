import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'bt-not-found-page',
  imports: [RouterLink],
  template: `
    <h1>Página no encontrada</h1>
    <p>La página que buscas no existe.</p>
    <a routerLink="/">Ir al inicio</a>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundPage {}
