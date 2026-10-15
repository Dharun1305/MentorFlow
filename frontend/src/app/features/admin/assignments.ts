import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Subscription } from 'rxjs';
import { Assignment, PagedResponse } from '../../core/models';
import { errorMessage } from '../../core/api-error';
import { Pager } from '../../shared/pager';
import { Modal } from '../../shared/modal';
import { AdminApi } from './admin-api';
import { AssignmentEditor } from './assignment-editor';

@Component({ selector: 'app-assignments', imports: [DatePipe, ReactiveFormsModule, RouterLink, Pager, Modal, AssignmentEditor], templateUrl: './assignments.html' })
export class Assignments {
  private readonly api = inject(AdminApi);
  private readonly destroy = inject(DestroyRef);
  readonly search = new FormControl('', { nonNullable: true });
  readonly status = new FormControl(inject(ActivatedRoute).snapshot.queryParamMap.get('status') === 'active' ? 'active' : 'all', { nonNullable: true });
  readonly page = signal(1);
  readonly data = signal<PagedResponse<Assignment> | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  readonly editor = signal<{ assignment: Assignment | null } | null>(null);
  readonly ending = signal<Assignment | null>(null);
  readonly endBusy = signal(false);
  readonly endError = signal('');
  private request?: Subscription;
  constructor() { this.load(); }
  load(): void {
    this.request?.unsubscribe();
    this.loading.set(true); this.error.set('');
    this.request = this.api.assignments({ page: this.page(), search: this.search.value, isActive: this.status.value === 'all' ? undefined : this.status.value === 'active' })
      .pipe(takeUntilDestroyed(this.destroy), finalize(() => this.loading.set(false)))
      .subscribe({ next: data => {
        const lastPage = Math.max(1, Math.ceil(data.totalCount / 20));
        if (this.page() > lastPage) { this.page.set(lastPage); this.load(); return; }
        this.data.set(data);
      }, error: error => this.error.set(errorMessage(error, 'Assignments could not be loaded. Please try again.')) });
  }
  filter(): void { this.page.set(1); this.load(); }
  changePage(page: number): void { this.page.set(page); this.load(); }
  clearFilters(): void { this.search.setValue(''); this.status.setValue('all'); this.filter(); }
  saved(assignment: Assignment): void {
    const transferred = !!this.editor()?.assignment;
    this.editor.set(null);
    this.message.set(`${assignment.studentName} ${transferred ? 'transferred' : 'assigned'} to ${assignment.mentorName}.`);
    this.clearFilters();
  }
  confirmEnd(assignment: Assignment): void { this.endError.set(''); this.ending.set(assignment); }
  end(): void {
    const assignment = this.ending();
    if (!assignment || this.endBusy()) return;
    this.endBusy.set(true); this.endError.set('');
    this.api.endAssignment(assignment.mentorStudentId).pipe(takeUntilDestroyed(this.destroy), finalize(() => this.endBusy.set(false)))
      .subscribe({ next: () => {
        this.ending.set(null);
        this.message.set(`The assignment for ${assignment.studentName} has ended. Its history is preserved.`);
        this.load();
      }, error: error => this.endError.set(errorMessage(error, 'The assignment could not be ended.')) });
  }
}
