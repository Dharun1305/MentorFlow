import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, firstValueFrom, map, of, tap } from 'rxjs';
import { API_URL } from './api.config';
import { errorMessage } from './api-error';
import { homeUrl, isRole, LoginResponse, User } from './models';

interface Session { token: string; expiresAtUtc: string; }
const SESSION_KEY = 'mentorflow.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly currentUser = signal<User | null>(null);
  private session: Session | null = this.readSession();
  private expiryTimer?: ReturnType<typeof setTimeout>;

  readonly user = this.currentUser.asReadonly();
  readonly signedIn = computed(() => this.currentUser() !== null);
  readonly sessionError = signal('');

  token(): string | null {
    if (!this.session || Date.parse(this.session.expiresAtUtc) <= Date.now()) return null;
    return this.session.token;
  }

  async restoreSession(): Promise<void> {
    this.sessionError.set('');
    if (!this.token()) { this.clearSession(); return; }
    await firstValueFrom(this.http.get<User>(`${API_URL}/auth/me`).pipe(
      tap(user => {
        if (!isRole(user.role)) throw new Error('Unsupported account role');
        this.currentUser.set(user);
        this.scheduleExpiry();
      }),
      map(() => undefined),
      catchError(error => {
        this.currentUser.set(null);
        if (error.status === 401 || !this.token()) this.clearSession();
        else this.sessionError.set(errorMessage(error, 'We could not verify your session. Please try again.'));
        return of(undefined);
      }),
    ));
  }

  login(email: string, password: string) {
    return this.http.post<LoginResponse>(`${API_URL}/auth/login`, { email: email.trim(), password }).pipe(
      tap(response => {
        if (!isRole(response.user.role) || !response.token ||
            !Number.isFinite(Date.parse(response.expiresAtUtc)) || Date.parse(response.expiresAtUtc) <= Date.now()) {
          throw new Error('The API returned an invalid session');
        }
        this.session = { token: response.token, expiresAtUtc: response.expiresAtUtc };
        try { sessionStorage.setItem(SESSION_KEY, JSON.stringify(this.session)); } catch { /* Memory-only session. */ }
        this.currentUser.set(response.user);
        this.sessionError.set('');
        this.scheduleExpiry();
      }),
    );
  }

  logout(expired = false): void {
    this.clearSession();
    void this.router.navigate(['/login'], { queryParams: expired ? { reason: 'expired' } : {} });
  }

  home(): string { return this.user() ? homeUrl(this.user()!.role) : '/login'; }

  private clearSession(): void {
    if (this.expiryTimer) clearTimeout(this.expiryTimer);
    this.expiryTimer = undefined;
    this.session = null;
    this.currentUser.set(null);
    this.sessionError.set('');
    try { sessionStorage.removeItem(SESSION_KEY); } catch { /* Storage may be unavailable. */ }
  }

  private scheduleExpiry(): void {
    if (this.expiryTimer) clearTimeout(this.expiryTimer);
    const remaining = Date.parse(this.session!.expiresAtUtc) - Date.now();
    this.expiryTimer = setTimeout(() => this.logout(true), Math.min(Math.max(remaining, 0), 2_147_483_647));
  }

  private readSession(): Session | null {
    try {
      const stored: unknown = JSON.parse(sessionStorage.getItem(SESSION_KEY) ?? 'null');
      if (!stored || typeof stored !== 'object') return null;
      const session = stored as Partial<Session>;
      return typeof session.token === 'string' && session.token.length > 0 &&
        typeof session.expiresAtUtc === 'string' && Number.isFinite(Date.parse(session.expiresAtUtc))
        ? { token: session.token, expiresAtUtc: session.expiresAtUtc } : null;
    } catch { return null; }
  }
}
