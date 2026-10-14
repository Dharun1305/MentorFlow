import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/auth.guards';

export const routes: Routes = [
  { path: 'login', title: 'Sign in · MentorFlow', canActivate: [guestGuard], loadComponent: () => import('./features/auth/login').then(m => m.Login) },
  {
    path: '', canActivate: [authGuard], loadComponent: () => import('./layout/shell').then(m => m.Shell),
    children: [
      ...(['Admin', 'Mentor', 'Student'] as const).map(role => ({
        path: `${role.toLowerCase()}/overview`, title: `${role} workspace · MentorFlow`,
        data: { role }, canActivate: [roleGuard], loadComponent: () => import('./features/home/home').then(m => m.Home),
      })),
      { path: '', pathMatch: 'full', redirectTo: 'login' },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
