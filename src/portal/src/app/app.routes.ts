import { Routes } from '@angular/router';

import { NotFoundPage } from './pages/not-found/not-found.page';
import { StatusPage } from './pages/status/status.page';

export const routes: Routes = [
  { path: '', component: StatusPage, title: 'Budget Tracker' },
  { path: '**', component: NotFoundPage, title: 'Page not found · Budget Tracker' },
];
