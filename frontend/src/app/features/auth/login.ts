import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/auth.service';
import { errorMessage } from '../../core/api-error';

@Component({ selector: 'app-login', imports: [ReactiveFormsModule], templateUrl: './login.html', styleUrl: './login.scss' })
export class Login {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  readonly busy = signal(false);
  readonly showPassword = signal(false);
  readonly error = signal('');
  readonly expired = this.route.snapshot.queryParamMap.get('reason') === 'expired';
  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(200)]],
  });

  submit(): void {
    if (this.busy()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    this.error.set('');
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false))).subscribe({
      next: () => { void this.router.navigateByUrl(this.auth.home()); },
      error: error => this.error.set(errorMessage(error, 'Sign-in failed. Please check your details and try again.')),
    });
  }

  async retrySession(): Promise<void> {
    if (this.busy()) return;
    this.busy.set(true);
    try {
      await this.auth.restoreSession();
      if (this.auth.signedIn()) await this.router.navigateByUrl(this.auth.home());
    } finally { this.busy.set(false); }
  }
}
