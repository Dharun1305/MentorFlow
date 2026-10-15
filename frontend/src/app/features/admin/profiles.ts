import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Subscription } from 'rxjs';
import { PagedResponse } from '../../core/models';
import { errorMessage } from '../../core/api-error';
import { Modal } from '../../shared/modal';
import { Pager } from '../../shared/pager';
import { AdminApi, Profile, profileCode, profileId, ProfileKind } from './admin-api';
import { ProfileEditor } from './profile-editor';

@Component({ selector: 'app-profiles', imports: [ReactiveFormsModule, RouterLink, ProfileEditor, Modal, Pager], templateUrl: './profiles.html' })
export class Profiles {
  private readonly api = inject(AdminApi);
  private readonly destroy = inject(DestroyRef);
  readonly kind = inject(ActivatedRoute).snapshot.data['profileType'] as ProfileKind;
  readonly singular = this.kind === 'students' ? 'student' : 'mentor';
  readonly heading = this.kind === 'students' ? 'Students' : 'Mentors';
  readonly search = new FormControl('', { nonNullable: true });
  readonly status = new FormControl('all', { nonNullable: true });
  readonly page = signal(1);
  readonly data = signal<PagedResponse<Profile> | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly actionError = signal('');
  readonly message = signal('');
  readonly opening = signal(false);
  readonly editor = signal<{ profile: Profile | null } | null>(null);
  readonly pendingStatus = signal<Profile | null>(null);
  readonly statusBusy = signal(false);
  readonly statusError = signal('');
  readonly id = profileId;
  readonly code = profileCode;
  private request?: Subscription;
  constructor() { this.load(); }

  load(): void {
    this.request?.unsubscribe();
    this.loading.set(true); this.error.set('');
    this.request = this.api.profiles(this.kind, { page: this.page(), search: this.search.value, isActive: this.status.value === 'all' ? undefined : this.status.value === 'active' })
      .pipe(takeUntilDestroyed(this.destroy), finalize(() => this.loading.set(false)))
      .subscribe({ next: data => {
        const lastPage = Math.max(1, Math.ceil(data.totalCount / 20));
        if (this.page() > lastPage) { this.page.set(lastPage); this.load(); return; }
        this.data.set(data);
      }, error: error => this.error.set(errorMessage(error, 'Accounts could not be loaded. Please try again.')) });
  }
  filter(): void { this.page.set(1); this.load(); }
  changePage(page: number): void { this.page.set(page); this.load(); }
  clearFilters(): void { this.search.setValue(''); this.status.setValue('all'); this.filter(); }
  add(): void { this.actionError.set(''); this.editor.set({ profile: null }); }
  edit(profile: Profile): void {
    if (this.opening()) return;
    this.opening.set(true); this.actionError.set('');
    this.api.profile(this.kind, profileId(profile)).pipe(takeUntilDestroyed(this.destroy), finalize(() => this.opening.set(false)))
      .subscribe({ next: profile => this.editor.set({ profile }), error: error => this.actionError.set(errorMessage(error, 'Account details could not be loaded.')) });
  }
  saved(profile: Profile): void {
    const created = !this.editor()?.profile;
    this.editor.set(null);
    this.message.set(`${profile.firstName} ${profile.lastName} ${created ? 'created' : 'updated'} successfully.`);
    if (created) { this.search.setValue(profileCode(profile)); this.status.setValue('all'); this.page.set(1); }
    this.load();
  }
  mentorship(profile: Profile): string {
    if ('studentCode' in profile) return profile.activeAssignment?.mentorName ?? 'Not assigned';
    return `${profile.assignments.length} active ${profile.assignments.length === 1 ? 'student' : 'students'}`;
  }
  confirmStatus(profile: Profile): void { this.statusError.set(''); this.pendingStatus.set(profile); }
  changeStatus(): void {
    const profile = this.pendingStatus();
    if (!profile || this.statusBusy()) return;
    this.statusBusy.set(true); this.statusError.set('');
    this.api.setActive(this.kind, profileId(profile), !profile.isActive)
      .pipe(takeUntilDestroyed(this.destroy), finalize(() => this.statusBusy.set(false)))
      .subscribe({ next: () => {
        this.pendingStatus.set(null);
        this.message.set(profile.isActive ? 'Account deactivated. Its active assignments have ended.' : 'Account activated. Create a new assignment when needed.');
        this.load();
      }, error: error => this.statusError.set(errorMessage(error, 'The account status could not be changed.')) });
  }
}
