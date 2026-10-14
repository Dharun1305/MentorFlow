import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.signedIn() && auth.token()) return true;
  return inject(Router).createUrlTree(['/login']);
};

export const roleGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  return auth.user()?.role === route.data['role'] ? true : inject(Router).parseUrl(auth.home());
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.signedIn() && auth.token() ? inject(Router).parseUrl(auth.home()) : true;
};
