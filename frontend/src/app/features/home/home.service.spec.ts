import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HomeService } from './home.service';

describe('Workspace API summaries', () => {
  let http: HttpTestingController;
  let service: HomeService;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(HomeService);
  });
  afterEach(() => http.verify());

  it('uses server totals rather than the length of the current page', () => {
    let counts: number[] = [];
    service.load('Admin').subscribe(data => counts = data.metrics.map(metric => metric.count));
    const students = http.expectOne(request => request.url.endsWith('/students'));
    expect(students.request.params.get('isActive')).toBe('true');
    students.flush({ items: [], totalCount: 51, page: 1, pageSize: 1 });
    http.expectOne(request => request.url.endsWith('/mentors')).flush({ items: [], totalCount: 8, page: 1, pageSize: 1 });
    http.expectOne(request => request.url.endsWith('/assignments')).flush({ items: [], totalCount: 42, page: 1, pageSize: 5 });
    expect(counts).toEqual([51, 8, 42]);
  });

  it('reports request failures instead of presenting fabricated zero totals', () => {
    let failed = false;
    service.load('Mentor').subscribe({ next: () => fail('Failed data must not be presented as successful'), error: () => failed = true });
    const students = http.expectOne(request => request.url.endsWith('/students'));
    const meetings = http.expectOne(request => request.url.endsWith('/meetings'));
    const goals = http.expectOne(request => request.url.endsWith('/goals'));
    students.flush({}, { status: 500, statusText: 'Server error' });
    expect(failed).toBeTrue();
    expect(meetings.cancelled).toBeTrue();
    expect(goals.cancelled).toBeTrue();
  });
});
