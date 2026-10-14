import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { forkJoin, map, Observable } from 'rxjs';
import { API_URL } from '../../core/api.config';
import { Assignment, Goal, Meeting, Mentor, PagedResponse, Role, Student } from '../../core/models';

export interface Metric { label: string; count: number; detail: string; }
export interface HomeData {
  metrics: Metric[];
  assignments: Assignment[];
  students: Student[];
  mentor: Mentor | null;
  meetings: Meeting[];
  goals: Goal[];
}

@Injectable({ providedIn: 'root' })
export class HomeService {
  private readonly http = inject(HttpClient);

  load(role: Role): Observable<HomeData> {
    if (role === 'Admin') return this.loadAdmin();
    if (role === 'Mentor') return this.loadMentor();
    return this.loadStudent();
  }

  private loadAdmin(): Observable<HomeData> {
    return forkJoin({
      students: this.http.get<PagedResponse<Student>>(`${API_URL}/students`, { params: { isActive: true, pageSize: 1 } }),
      mentors: this.http.get<PagedResponse<Mentor>>(`${API_URL}/mentors`, { params: { isActive: true, pageSize: 1 } }),
      assignments: this.http.get<PagedResponse<Assignment>>(`${API_URL}/assignments`, { params: { isActive: true, pageSize: 5 } }),
    }).pipe(map(({ students, mentors, assignments }) => ({
      metrics: [
        { label: 'Active students', count: students.totalCount, detail: 'Ready for guidance' },
        { label: 'Active mentors', count: mentors.totalCount, detail: 'Supporting student growth' },
        { label: 'Active assignments', count: assignments.totalCount, detail: 'Mentoring connections' },
      ],
      assignments: assignments.items, students: [], mentor: null, meetings: [], goals: [],
    })));
  }

  private loadMentor(): Observable<HomeData> {
    return forkJoin({
      students: this.http.get<PagedResponse<Student>>(`${API_URL}/students`, { params: { isActive: true, pageSize: 5 } }),
      meetings: this.http.get<PagedResponse<Meeting>>(`${API_URL}/meetings`, { params: { pageSize: 3 } }),
      goals: this.http.get<PagedResponse<Goal>>(`${API_URL}/goals`, { params: { pageSize: 4 } }),
    }).pipe(map(({ students, meetings, goals }) => ({
      metrics: [
        { label: 'Assigned students', count: students.totalCount, detail: 'Your current mentoring group' },
        { label: 'Recorded meetings', count: meetings.totalCount, detail: 'Across your mentoring periods' },
        { label: 'Student goals', count: goals.totalCount, detail: 'Across your mentoring periods' },
      ],
      assignments: [], students: students.items, mentor: null, meetings: meetings.items, goals: goals.items,
    })));
  }

  private loadStudent(): Observable<HomeData> {
    return forkJoin({
      mentors: this.http.get<PagedResponse<Mentor>>(`${API_URL}/mentors`, { params: { isActive: true, pageSize: 1 } }),
      meetings: this.http.get<PagedResponse<Meeting>>(`${API_URL}/meetings`, { params: { pageSize: 3 } }),
      goals: this.http.get<PagedResponse<Goal>>(`${API_URL}/goals`, { params: { pageSize: 4 } }),
    }).pipe(map(({ mentors, meetings, goals }) => ({
      metrics: [
        { label: 'Your mentor', count: mentors.totalCount, detail: 'Your current mentoring connection' },
        { label: 'Recorded meetings', count: meetings.totalCount, detail: 'Conversations along your journey' },
        { label: 'Your goals', count: goals.totalCount, detail: 'Across your mentoring periods' },
      ],
      assignments: [], students: [], mentor: mentors.items[0] ?? null, meetings: meetings.items, goals: goals.items,
    })));
  }
}
