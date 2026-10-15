import { Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { Assignment, Mentor, Student } from '../../core/models';
import { errorMessage } from '../../core/api-error';
import { Modal } from '../../shared/modal';
import { AdminApi, Profile } from './admin-api';
import { PersonPicker } from './person-picker';

@Component({ selector: 'app-assignment-editor', imports: [Modal, PersonPicker], templateUrl: './assignment-editor.html' })
export class AssignmentEditor {
  readonly assignment = input<Assignment | null>(null);
  readonly saved = output<Assignment>();
  readonly closed = output<void>();
  private readonly api = inject(AdminApi);
  private readonly destroy = inject(DestroyRef);
  readonly student = signal<Student | null>(null);
  readonly mentor = signal<Mentor | null>(null);
  readonly busy = signal(false);
  readonly error = signal('');
  selectStudent(profile: Profile): void { if ('studentId' in profile) { this.student.set(profile); this.error.set(''); } }
  selectMentor(profile: Profile): void { if ('mentorId' in profile) { this.mentor.set(profile); this.error.set(''); } }
  submit(): void {
    const mentor = this.mentor(), student = this.student(), assignment = this.assignment();
    if (this.busy() || !mentor || (!assignment && !student)) return;
    this.busy.set(true); this.error.set('');
    const request = assignment ? this.api.reassign(assignment.mentorStudentId, mentor.mentorId) : this.api.assign(student!.studentId, mentor.mentorId);
    request.pipe(takeUntilDestroyed(this.destroy), finalize(() => this.busy.set(false)))
      .subscribe({ next: result => this.saved.emit(result), error: error => this.error.set(errorMessage(error, 'The assignment could not be saved. Please try again.')) });
  }
}
