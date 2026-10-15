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
      ...(['students', 'mentors'] as const).map(profileType => ({
        path: `admin/${profileType}`, title: `${profileType === 'students' ? 'Students' : 'Mentors'} · MentorFlow`,
        data: { role: 'Admin', profileType }, canActivate: [roleGuard],
        loadComponent: () => import('./features/admin/profiles').then(m => m.Profiles),
      })),
      { path: 'admin/assignments', title: 'Assignments · MentorFlow', data: { role: 'Admin' }, canActivate: [roleGuard], loadComponent: () => import('./features/admin/assignments').then(m => m.Assignments) },
      { path: '', pathMatch: 'full', redirectTo: 'login' },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
