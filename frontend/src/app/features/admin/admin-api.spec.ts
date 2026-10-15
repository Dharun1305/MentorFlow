import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AdminApi, ProfileInput } from './admin-api';
import { errorMessage } from '../../core/api-error';
import { HttpErrorResponse } from '@angular/common/http';

describe('Admin API contracts', () => {
  let api: AdminApi;
  let http: HttpTestingController;
  const fields: ProfileInput = { firstName: 'Test', lastName: 'Mentor', email: 'test@example.test', code: 'MT-002', password: 'PrivatePassword123!', specialization: 'Career guidance' };
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(AdminApi); http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('sends inactive filters and pagination to the server', () => {
    api.profiles('students', { page: 6, isActive: false, search: ' ST-101 ' }).subscribe();
    const request = http.expectOne(req => req.url === '/api/students');
    expect(request.request.params.get('isActive')).toBe('false');
    expect(request.request.params.get('page')).toBe('6');
    expect(request.request.params.get('search')).toBe('ST-101');
    request.flush({ items: [], totalCount: 0, page: 6, pageSize: 20 });
  });
  it('uses the mentor request fields for account creation', () => {
    api.saveProfile('mentors', fields).subscribe();
    const request = http.expectOne('/api/mentors');
    expect(request.request.method).toBe('POST');
    expect(request.request.body.employeeCode).toBe('MT-002');
    expect(request.request.body.password).toBe(fields.password);
    expect(request.request.body.studentCode).toBeUndefined();
    request.flush({});
  });
  it('does not send password or status during profile editing', () => {
    api.saveProfile('students', fields, 3).subscribe();
    const request = http.expectOne('/api/students/3');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body.studentCode).toBe('MT-002');
    expect(request.request.body.password).toBeUndefined();
    expect(request.request.body.isActive).toBeUndefined();
    request.flush({});
  });
  it('changes status without overwriting profile details', () => {
    api.setActive('mentors', 2, false).subscribe();
    const request = http.expectOne('/api/mentors/2/status');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ isActive: false });
    request.flush({});
  });
  it('uses the atomic transfer endpoint instead of ending and creating separately', () => {
    api.reassign(11, 7).subscribe();
    const request = http.expectOne('/api/assignments/11/reassign');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ mentorId: 7 });
    request.flush({});
  });
  it('preserves specific duplicate-account conflict messages', () => {
    const error = new HttpErrorResponse({ status: 409, error: { message: 'Email is already registered.' } });
    expect(errorMessage(error, 'Save failed')).toBe('Email is already registered.');
  });
});
