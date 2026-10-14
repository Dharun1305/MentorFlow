import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { errorMessage } from '../../core/api-error';
import { HomeData, HomeService } from './home.service';

@Component({ selector: 'app-home', imports: [DatePipe], templateUrl: './home.html', styleUrl: './home.scss' })
export class Home {
  readonly auth = inject(AuthService);
  private readonly service = inject(HomeService);
  private readonly destroyRef = inject(DestroyRef);
  readonly role = this.auth.user()!.role;
  readonly today = new Date();
  readonly data = signal<HomeData | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly introductions = {
    Admin: 'A clear view of the people and connections in your mentoring community.',
    Mentor: 'Stay connected with your students and the steps they are taking.',
    Student: 'Your conversations, your goals, and the guidance to keep moving forward.',
  };

  constructor() { this.reload(); }

  reload(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.service.load(this.role).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.loading.set(false))).subscribe({
      next: data => this.data.set(data),
      error: error => this.error.set(errorMessage(error, 'We could not load your workspace. Please try again.')),
    });
  }
}
