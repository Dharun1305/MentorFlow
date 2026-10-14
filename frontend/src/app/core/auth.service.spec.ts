import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';
import { LoginResponse } from './models';

const user = { userId: 42, firstName: 'Test', lastName: 'Mentor', email: 'mentor@example.test', role: 'Mentor' as const };

function response(): LoginResponse {
  return { token: 'test-token', expiresAtUtc: new Date(Date.now() + 60_000).toISOString(), user };
}

describe('Authentication and API boundaries', () => {
  let auth: AuthService;
  let http: HttpTestingController;
  let client: HttpClient;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    sessionStorage.clear();
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    router.navigate.and.returnValue(Promise.resolve(true));
    TestBed.configureTestingModule({ providers: [
      provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting(),
      { provide: Router, useValue: router },
    ] });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
  });

  afterEach(() => { http.verify(); auth.logout(); sessionStorage.clear(); });

  function signIn(): void {
    auth.login(' mentor@example.test ', 'test-password').subscribe();
    const request = http.expectOne('/api/auth/login');
    expect(request.request.body.email).toBe('mentor@example.test');
    expect(request.request.headers.has('Authorization')).toBeFalse();
    request.flush(response());
  }

  it('stores the token and expiry, routes by API role, and does not store the password', () => {
    signIn();
    expect(auth.home()).toBe('/mentor/overview');
    expect(auth.user()?.userId).toBe(42);
    const stored = JSON.parse(sessionStorage.getItem('mentorflow.session')!);
    expect(stored.token).toBe('test-token');
    expect(stored.password).toBeUndefined();
    expect(stored.user).toBeUndefined();
  });

  it('sends the token only to the configured API', () => {
    signIn();
    client.get('/api/students').subscribe();
    expect(http.expectOne('/api/students').request.headers.get('Authorization')).toBe('Bearer test-token');
    client.get('https://third-party.example/api/students').subscribe();
    expect(http.expectOne('https://third-party.example/api/students').request.headers.has('Authorization')).toBeFalse();
    client.get('/api-other/students').subscribe();
    expect(http.expectOne('/api-other/students').request.headers.has('Authorization')).toBeFalse();
  });

  it('clears the session and returns to login when an authenticated API request returns 401', () => {
    signIn();
    client.get('/api/students').subscribe({ error: () => undefined });
    http.expectOne('/api/students').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(auth.user()).toBeNull();
    expect(auth.token()).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { reason: 'expired' } });
  });

  it('does not treat invalid login credentials as an expired session', () => {
    auth.login('mentor@example.test', 'wrong-password').subscribe({ error: () => undefined });
    http.expectOne('/api/auth/login').flush({ message: 'Invalid email or password.' }, { status: 401, statusText: 'Unauthorized' });
    expect(router.navigate).not.toHaveBeenCalled();
    expect(auth.signedIn()).toBeFalse();
  });

  it('ends the session at the API expiry time', fakeAsync(() => {
    signIn();
    tick(60_001);
    expect(auth.token()).toBeNull();
    expect(auth.signedIn()).toBeFalse();
    expect(router.navigate).toHaveBeenCalled();
  }));
});

describe('Session restoration', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.setItem('mentorflow.session', JSON.stringify({ token: 'saved-token', expiresAtUtc: new Date(Date.now() + 60_000).toISOString() }));
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Router, useValue: { navigate: () => Promise.resolve(true) } }] });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { http.verify(); auth.logout(); sessionStorage.clear(); });

  it('restores the user from /auth/me rather than trusting a stored role', async () => {
    const pending = auth.restoreSession();
    expect(auth.user()).toBeNull();
    http.expectOne('/api/auth/me').flush(user);
    await pending;
    expect(auth.home()).toBe('/mentor/overview');
  });

  it('preserves a valid saved token on a temporary backend failure but does not unlock routes', async () => {
    const pending = auth.restoreSession();
    http.expectOne('/api/auth/me').flush({}, { status: 503, statusText: 'Unavailable' });
    await pending;
    expect(auth.token()).toBe('saved-token');
    expect(auth.signedIn()).toBeFalse();
    expect(auth.sessionError()).toBeTruthy();
  });

  it('discards a session rejected by the backend', async () => {
    const pending = auth.restoreSession();
    http.expectOne('/api/auth/me').flush({}, { status: 401, statusText: 'Unauthorized' });
    await pending;
    expect(auth.token()).toBeNull();
    expect(auth.user()).toBeNull();
  });
});
