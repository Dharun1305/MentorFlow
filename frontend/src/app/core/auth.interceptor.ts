import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { API_URL } from './api.config';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const target = new URL(request.url, window.location.origin);
  const api = new URL(API_URL, window.location.origin);
  const isApi = target.origin === api.origin && (target.pathname === api.pathname || target.pathname.startsWith(`${api.pathname}/`));
  const isLogin = target.pathname === `${api.pathname}/auth/login`;
  const token = isApi && !isLogin ? auth.token() : null;
  const outgoing = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;

  return next(outgoing).pipe(catchError((error: HttpErrorResponse) => {
    if (isApi && !isLogin && error.status === 401) auth.logout(true);
    return throwError(() => error);
  }));
};
