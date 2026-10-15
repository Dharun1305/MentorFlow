import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/api.config';
import { Assignment, Mentor, PagedResponse, Student } from '../../core/models';

export type ProfileKind = 'students' | 'mentors';
export type Profile = Student | Mentor;
export interface ListQuery { page: number; pageSize?: number; search?: string; isActive?: boolean; }
export interface ProfileInput { firstName: string; lastName: string; email: string; code: string; password: string; specialization: string; }

@Injectable({ providedIn: 'root' })
export class AdminApi {
  private readonly http = inject(HttpClient);
  profiles(kind: ProfileKind, query: ListQuery): Observable<PagedResponse<Profile>> {
    return this.http.get<PagedResponse<Profile>>(`${API_URL}/${kind}`, { params: this.params(query) });
  }
  profile(kind: ProfileKind, id: number): Observable<Profile> { return this.http.get<Profile>(`${API_URL}/${kind}/${id}`); }
  saveProfile(kind: ProfileKind, fields: ProfileInput, id?: number): Observable<Profile> {
    const { firstName, lastName, email } = fields;
    const details = kind === 'students' ? { studentCode: fields.code } : { employeeCode: fields.code, specialization: fields.specialization };
    const body = { firstName, lastName, email, ...details };
    // Profile edits deliberately omit activation and password; status has its own endpoint.
    return id === undefined
      ? this.http.post<Profile>(`${API_URL}/${kind}`, { ...body, password: fields.password })
      : this.http.put<Profile>(`${API_URL}/${kind}/${id}`, body);
  }
  setActive(kind: ProfileKind, id: number, isActive: boolean): Observable<Profile> {
    return this.http.patch<Profile>(`${API_URL}/${kind}/${id}/status`, { isActive });
  }
  assignments(query: ListQuery): Observable<PagedResponse<Assignment>> {
    return this.http.get<PagedResponse<Assignment>>(`${API_URL}/assignments`, { params: this.params(query) });
  }
  assign(studentId: number, mentorId: number): Observable<Assignment> {
    return this.http.post<Assignment>(`${API_URL}/assignments`, { studentId, mentorId });
  }
  reassign(id: number, mentorId: number): Observable<Assignment> {
    return this.http.post<Assignment>(`${API_URL}/assignments/${id}/reassign`, { mentorId });
  }
  endAssignment(id: number): Observable<Assignment> {
    return this.http.put<Assignment>(`${API_URL}/assignments/${id}`, { isActive: false });
  }
  private params(query: ListQuery): HttpParams {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize ?? 20);
    if (query.search?.trim()) params = params.set('search', query.search.trim());
    if (query.isActive !== undefined) params = params.set('isActive', query.isActive);
    return params;
  }
}

export function profileId(profile: Profile): number { return 'studentId' in profile ? profile.studentId : profile.mentorId; }
export function profileCode(profile: Profile): string { return 'studentCode' in profile ? profile.studentCode : profile.employeeCode; }
