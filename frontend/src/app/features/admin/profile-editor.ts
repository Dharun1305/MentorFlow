import { Component, computed, DestroyRef, inject, input, OnInit, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { Modal } from '../../shared/modal';
import { errorMessage } from '../../core/api-error';
import { AdminApi, Profile, profileCode, profileId, ProfileKind } from './admin-api';
import { notBlank, validationErrors } from './form-utils';

@Component({ selector: 'app-profile-editor', imports: [ReactiveFormsModule, Modal], templateUrl: './profile-editor.html' })
export class ProfileEditor implements OnInit {
  readonly kind = input.required<ProfileKind>();
  readonly profile = input<Profile | null>(null);
  readonly saved = output<Profile>();
  readonly closed = output<void>();
  private readonly api = inject(AdminApi);
  private readonly destroy = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  readonly singular = computed(() => this.kind() === 'students' ? 'student' : 'mentor');
  readonly title = computed(() => `${this.profile() ? 'Edit' : 'Add'} ${this.singular()}`);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly errors = signal<string[]>([]);
  readonly showPassword = signal(false);
  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, notBlank, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, notBlank, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
    code: ['', [Validators.required, notBlank, Validators.maxLength(50)]],
    specialization: ['', Validators.maxLength(150)],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(200)]],
  });
  ngOnInit(): void {
    const profile = this.profile();
    if (profile) {
      this.form.patchValue({ firstName: profile.firstName, lastName: profile.lastName, email: profile.email, code: profileCode(profile), specialization: 'specialization' in profile ? profile.specialization : '' });
      this.form.controls.password.disable();
    }
  }
  invalid(field: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[field];
    return control.touched && control.invalid;
  }
  submit(): void {
    if (this.busy()) return;
    const fields = this.form.getRawValue();
    this.form.patchValue({ firstName: fields.firstName.trim(), lastName: fields.lastName.trim(), email: fields.email.trim(), code: fields.code.trim(), specialization: fields.specialization.trim() });
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true); this.error.set(''); this.errors.set([]);
    const existing = this.profile();
    this.api.saveProfile(this.kind(), this.form.getRawValue(), existing ? profileId(existing) : undefined)
      .pipe(takeUntilDestroyed(this.destroy), finalize(() => this.busy.set(false)))
      .subscribe({ next: profile => this.saved.emit(profile), error: error => {
        this.error.set(errorMessage(error, 'The account could not be saved. Please try again.'));
        this.errors.set(validationErrors(error));
      } });
  }
}
