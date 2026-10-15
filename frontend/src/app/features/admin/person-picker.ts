import { Component, DestroyRef, inject, input, OnInit, output, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Subscription } from 'rxjs';
import { PagedResponse } from '../../core/models';
import { errorMessage } from '../../core/api-error';
import { Pager } from '../../shared/pager';
import { AdminApi, Profile, profileCode, profileId, ProfileKind } from './admin-api';

@Component({ selector: 'app-person-picker', imports: [ReactiveFormsModule, Pager], templateUrl: './person-picker.html' })
export class PersonPicker implements OnInit {
  readonly kind = input.required<ProfileKind>();
  readonly selectedId = input<number | null>(null);
  readonly disabled = input(false);
  readonly unassignedOnly = input(false);
  readonly excludeId = input<number | null>(null);
  readonly selected = output<Profile>();
  private readonly api = inject(AdminApi);
  private readonly destroy = inject(DestroyRef);
  readonly search = new FormControl('', { nonNullable: true });
  readonly page = signal(1);
  readonly data = signal<PagedResponse<Profile> | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly id = profileId;
  readonly code = profileCode;
  private request?: Subscription;
  ngOnInit(): void { this.load(); }
  load(): void {
    this.request?.unsubscribe();
    this.loading.set(true); this.error.set('');
    this.request = this.api.profiles(this.kind(), { page: this.page(), pageSize: 5, search: this.search.value, isActive: true })
      .pipe(takeUntilDestroyed(this.destroy), finalize(() => this.loading.set(false)))
      .subscribe({ next: data => this.data.set(data), error: error => this.error.set(errorMessage(error, 'Choices could not be loaded. Please retry.')) });
  }
  filter(): void { this.page.set(1); this.load(); }
  changePage(page: number): void { this.page.set(page); this.load(); }
  unavailable(profile: Profile): boolean {
    return profileId(profile) === this.excludeId() || (this.unassignedOnly() && 'activeAssignment' in profile && profile.activeAssignment !== null);
  }
}
