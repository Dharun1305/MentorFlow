import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';
import { authGuard, guestGuard, roleGuard } from './auth.guards';

describe('Workspace route protection', () => {
  const auth = { signedIn: () => true, token: () => 'token', user: () => ({ role: 'Mentor' }), home: () => '/mentor/overview' };
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: AuthService, useValue: auth }] }));

  it('redirects a mentor away from admin routes', () => {
    const route = { data: { role: 'Admin' } } as unknown as ActivatedRouteSnapshot;
    const result = TestBed.runInInjectionContext(() => roleGuard(route, {} as RouterStateSnapshot)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/mentor/overview');
  });

  it('allows the matching role', () => {
    const route = { data: { role: 'Mentor' } } as unknown as ActivatedRouteSnapshot;
    expect(TestBed.runInInjectionContext(() => roleGuard(route, {} as RouterStateSnapshot))).toBeTrue();
  });

  it('rejects an expired token even when user information remains in memory', () => {
    spyOn(auth, 'token').and.returnValue(null as unknown as string);
    const result = TestBed.runInInjectionContext(() => authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/login');
  });

  it('redirects authenticated users from login to their own workspace', () => {
    const result = TestBed.runInInjectionContext(() => guestGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/mentor/overview');
  });
});
