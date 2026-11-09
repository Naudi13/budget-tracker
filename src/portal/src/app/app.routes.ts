import { Routes } from '@angular/router';

import { adminGuard, signedInGuard, signedOutGuard } from './core/auth.guards';
import { ActivatePage } from './pages/activate/activate.page';
import { BudgetsPage } from './pages/budgets/budgets.page';
import { CategoriesPage } from './pages/categories/categories.page';
import { DashboardPage } from './pages/dashboard/dashboard.page';
import { HistoryPage } from './pages/history/history.page';
import { NewRecordPage } from './pages/new-record/new-record.page';
import { NotFoundPage } from './pages/not-found/not-found.page';
import { SignInPage } from './pages/sign-in/sign-in.page';
import { StatusPage } from './pages/status/status.page';
import { UsersPage } from './pages/users/users.page';

export const routes: Routes = [
  // The dashboard is the entry point; without a token its guard sends you to sign-in.
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'sign-in', component: SignInPage, canActivate: [signedOutGuard], title: 'Iniciar sesión · Budget Tracker' },
  // The link is the credential: these two need no session (HU-10).
  { path: 'activate', component: ActivatePage, data: { mode: 'activate' }, title: 'Activa tu cuenta · Budget Tracker' },
  { path: 'reset-password', component: ActivatePage, data: { mode: 'reset' }, title: 'Restablece tu contraseña · Budget Tracker' },
  { path: 'dashboard', component: DashboardPage, canActivate: [signedInGuard], title: 'Panel · Budget Tracker' },
  {
    path: 'transactions',
    component: HistoryPage,
    canActivate: [signedInGuard],
    title: 'Movimientos · Budget Tracker',
  },
  {
    path: 'transactions/new',
    component: NewRecordPage,
    canActivate: [signedInGuard],
    title: 'Nuevo registro · Budget Tracker',
  },
  {
    path: 'settings/categories',
    component: CategoriesPage,
    canActivate: [signedInGuard, adminGuard],
    title: 'Categorías · Budget Tracker',
  },
  {
    path: 'settings/budgets',
    component: BudgetsPage,
    canActivate: [signedInGuard, adminGuard],
    title: 'Presupuestos · Budget Tracker',
  },
  {
    path: 'settings/users',
    component: UsersPage,
    canActivate: [signedInGuard, adminGuard],
    title: 'Usuarios · Budget Tracker',
  },
  // Whether the API and its database are up; open, for local setup (local-setup.md).
  { path: 'status', component: StatusPage, title: 'Estado del sistema · Budget Tracker' },
  { path: '**', component: NotFoundPage, title: 'Página no encontrada · Budget Tracker' },
];
