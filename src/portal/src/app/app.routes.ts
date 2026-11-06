import { Routes } from '@angular/router';

import { signedInGuard, signedOutGuard } from './core/auth.guards';
import { DashboardPage } from './pages/dashboard/dashboard.page';
import { NotFoundPage } from './pages/not-found/not-found.page';
import { SignInPage } from './pages/sign-in/sign-in.page';
import { SignUpPage } from './pages/sign-up/sign-up.page';
import { StatusPage } from './pages/status/status.page';

export const routes: Routes = [
  // The dashboard is the entry point; without a token its guard sends you to sign-in.
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'sign-in', component: SignInPage, canActivate: [signedOutGuard], title: 'Iniciar sesión · Budget Tracker' },
  { path: 'sign-up', component: SignUpPage, canActivate: [signedOutGuard], title: 'Crear cuenta · Budget Tracker' },
  { path: 'dashboard', component: DashboardPage, canActivate: [signedInGuard], title: 'Panel · Budget Tracker' },
  // Whether the API and its database are up; open, for local setup (local-setup.md).
  { path: 'status', component: StatusPage, title: 'Estado del sistema · Budget Tracker' },
  { path: '**', component: NotFoundPage, title: 'Página no encontrada · Budget Tracker' },
];
