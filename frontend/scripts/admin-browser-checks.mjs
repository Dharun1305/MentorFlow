// Stateful API fixtures test UI behavior; real SQL Server verification is a separate check.
import assert from 'node:assert/strict';
import { mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';
import { startFrontend } from './test-server.mjs';

const { base, close } = await startFrontend();
const output = fileURLToPath(new URL('../test-results/', import.meta.url));
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ headless: true, executablePath: process.env.CHROME_BIN || undefined, args: ['--no-sandbox', '--disable-dev-shm-usage'] });
const errors = [];
let checks = 0;
const pass = label => { checks++; console.log(`PASS ${label}`); };
const state = {
  students: Array.from({ length: 105 }, (_, i) => ({ studentId: i + 1, userId: 9000 + i, firstName: 'Student', lastName: `${i + 1}`, email: `student${i + 1}@example.test`, studentCode: `ST-${String(i + 1).padStart(3, '0')}`, isActive: true })),
  mentors: [{ mentorId: 5, userId: 5005, firstName: 'Jordan', lastName: 'Lee', email: 'jordan@example.test', employeeCode: 'MT-005', specialization: 'Career guidance', isActive: true }, { mentorId: 7, userId: 5007, firstName: 'Mina', lastName: 'Rao', email: 'mina@example.test', employeeCode: 'MT-007', specialization: 'Learning support', isActive: true }],
  assignments: [{ mentorStudentId: 11, mentorId: 5, studentId: 2, assignedAt: '2026-10-01T10:00:00Z', isActive: true }],
  writes: [], studentPages: [], failTransfer: false,
};
function assignmentView(item) {
  const student = state.students.find(p => p.studentId === item.studentId);
  const mentor = state.mentors.find(p => p.mentorId === item.mentorId);
  return { ...item, studentName: `${student.firstName} ${student.lastName}`, mentorName: `${mentor.firstName} ${mentor.lastName}` };
}
function studentView(item) { return { ...item, activeAssignment: state.assignments.filter(a => a.studentId === item.studentId && a.isActive).map(assignmentView)[0] ?? null }; }
function mentorView(item) { return { ...item, assignments: state.assignments.filter(a => a.mentorId === item.mentorId && a.isActive).map(assignmentView) }; }
function paged(items, url) {
  const search = (url.searchParams.get('search') ?? '').toLowerCase();
  if (url.searchParams.has('isActive')) items = items.filter(p => p.isActive === (url.searchParams.get('isActive') === 'true'));
  if (search) items = items.filter(p => `${p.firstName ?? ''} ${p.lastName ?? ''} ${p.email ?? ''} ${p.studentCode ?? ''} ${p.employeeCode ?? ''} ${p.studentName ?? ''} ${p.mentorName ?? ''}`.toLowerCase().includes(search));
  const page = Number(url.searchParams.get('page') ?? 1), pageSize = Number(url.searchParams.get('pageSize') ?? 20);
  return { items: items.slice((page - 1) * pageSize, page * pageSize), totalCount: items.length, page, pageSize };
}
async function setup(page, role = 'Admin') {
  page.on('pageerror', error => errors.push(error.message));
  await page.route('**/api/**', async route => {
    const request = route.request(), url = new URL(request.url()), parts = url.pathname.split('/').filter(Boolean).slice(1);
    const [resource, rawId, action] = parts, id = Number(rawId), method = request.method();
    const reply = (json, status = 200) => route.fulfill({ status, json });
    const user = { userId: 1, firstName: 'Dev', lastName: role, email: 'admin@example.test', role };
    if (resource === 'auth') return reply(parts[1] === 'login' ? { token: 'test-admin', expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(), user } : user);
    assert.match(request.headers().authorization ?? '', /^Bearer /);
    if (method !== 'GET') state.writes.push({ resource, id, action, method, body: request.postDataJSON() });
    if (resource === 'students' || resource === 'mentors') {
      const records = state[resource], key = resource === 'students' ? 'studentId' : 'mentorId', code = resource === 'students' ? 'studentCode' : 'employeeCode';
      const view = resource === 'students' ? studentView : mentorView;
      if (method === 'GET') {
        if (rawId) return records.some(p => p[key] === id) ? reply(view(records.find(p => p[key] === id))) : reply({ message: 'Account not found.' }, 404);
        if (resource === 'students' && url.searchParams.get('pageSize') === '5') state.studentPages.push(Number(url.searchParams.get('page')));
        return reply(paged(records.map(view), url));
      }
      const body = request.postDataJSON();
      if (method === 'POST') {
        if ([...state.students, ...state.mentors].some(p => p.email === body.email)) return reply({ message: 'Email is already registered.' }, 409);
        await new Promise(resolve => setTimeout(resolve, 80));
        const nextId = Math.max(...records.map(p => p[key])) + 1;
        const { password, ...details } = body;
        const record = { ...details, [key]: nextId, userId: nextId + 9000, isActive: true };
        records.push(record); return reply(view(record), 201);
      }
      const record = records.find(p => p[key] === id);
      assert.ok(record);
      if (method === 'PATCH') {
        assert.deepEqual(Object.keys(body), ['isActive']);
        record.isActive = body.isActive;
        if (!body.isActive) state.assignments.filter(a => a[key] === id).forEach(a => a.isActive = false);
      } else {
        assert.equal(body.password, undefined); assert.equal(body.isActive, undefined);
        Object.assign(record, body);
      }
      return reply(view(record));
    }
    if (resource === 'assignments') {
      if (method === 'GET') return reply(paged([...state.assignments].reverse().map(assignmentView), url));
      const body = request.postDataJSON();
      if (method === 'PUT') { const item = state.assignments.find(a => a.mentorStudentId === id); item.isActive = false; return reply(assignmentView(item)); }
      if (action === 'reassign' && state.failTransfer) return reply({ message: 'The mentor is no longer active.' }, 409);
      const previous = action === 'reassign' ? state.assignments.find(a => a.mentorStudentId === id) : null;
      const studentId = previous?.studentId ?? body.studentId;
      assert.ok(state.students.some(p => p.studentId === studentId));
      assert.ok(state.mentors.some(p => p.mentorId === body.mentorId));
      if (previous) previous.isActive = false;
      else assert.ok(!state.assignments.some(a => a.studentId === studentId && a.isActive));
      const next = { mentorStudentId: Math.max(...state.assignments.map(a => a.mentorStudentId)) + 1, studentId, mentorId: body.mentorId, assignedAt: new Date().toISOString(), isActive: true };
      state.assignments.push(next); return reply(assignmentView(next), 201);
    }
    if (resource === 'meetings' || resource === 'goals') return reply({ items: [], totalCount: 0, page: 1, pageSize: 3 });
    throw new Error(`Unexpected request: ${method} ${url.pathname}`);
  });
}
async function login(page) {
  await page.goto(`${base}/login`);
  await page.getByLabel('Email address').fill('admin@example.test');
  await page.getByLabel('Password', { exact: true }).fill('TestPassword123!');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.locator('.metric-card').first().waitFor();
}
const link = (page, name) => page.getByRole('navigation', { name: 'Main navigation' }).getByRole('link', { name, exact: true });
const row = (page, name) => page.getByRole('row').filter({ has: page.getByText(name, { exact: true }) });
async function noOverflow(page) { assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'No horizontal page scroll'); }
let page;
try {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  page = await context.newPage(); await setup(page); await login(page);
  await link(page, 'Students').click();
  await page.getByRole('heading', { name: 'Students.' }).waitFor();
  await page.getByRole('button', { name: 'Add student' }).click();
  let dialog = page.getByRole('dialog', { name: 'Add student' });
  await dialog.getByRole('button', { name: 'Create account' }).click();
  await dialog.locator('#firstName-error').filter({ hasText: 'Enter a first name' }).waitFor();
  pass('Required profile fields are validated');
  await dialog.getByLabel('First name').fill('Alex'); await dialog.getByLabel('Last name').fill('Student');
  await dialog.getByLabel('Email address').fill('student1@example.test'); await dialog.getByLabel('Student code').fill('NEW-106');
  await dialog.getByLabel('Initial password').fill('TestPassword123!');
  await dialog.getByRole('button', { name: 'Create account' }).click();
  await dialog.getByRole('alert').filter({ hasText: 'Email is already registered.' }).waitFor();
  pass('Duplicate email shows the actual backend conflict');
  await dialog.getByLabel('Email address').fill('newstudent@example.test');
  await dialog.getByRole('button', { name: 'Create account' }).dblclick();
  await dialog.waitFor({ state: 'detached' });
  await row(page, 'Alex Student').waitFor();
  assert.equal(state.students.filter(p => p.email === 'newstudent@example.test').length, 1);
  pass('Create student and prevent duplicate submission');
  await row(page, 'Alex Student').getByRole('button', { name: 'Deactivate Alex Student' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Deactivate account' }).click();
  await page.getByRole('dialog').waitFor({ state: 'detached' });
  await row(page, 'Alex Student').getByText('Inactive', { exact: true }).waitFor();
  await row(page, 'Alex Student').getByRole('button', { name: 'Edit Alex Student' }).click();
  dialog = page.getByRole('dialog', { name: 'Edit student' });
  await dialog.getByLabel('First name').fill('Avery');
  await dialog.getByRole('button', { name: 'Save changes' }).click();
  await dialog.waitFor({ state: 'detached' });
  await row(page, 'Avery Student').getByText('Inactive', { exact: true }).waitFor();
  pass('Editing an inactive student preserves activation');
  await row(page, 'Avery Student').getByRole('button', { name: 'Activate Avery Student', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Activate account' }).click();
  await page.getByRole('dialog').waitFor({ state: 'detached' });
  await row(page, 'Avery Student').getByText('Active', { exact: true }).waitFor();
  pass('Activate/deactivate with confirmation');
  await page.getByLabel('Search students').fill('ST-001'); await page.getByLabel('Search students').press('Enter');
  await page.getByText('1 students · Page 1 of 1', { exact: true }).waitFor(); await row(page, 'Student 1').waitFor(); await page.screenshot({ path: `${output}/admin-students-desktop.png`, fullPage: true });
  pass('Profile search applies server filters');
  await link(page, 'Mentors').click(); await page.getByRole('button', { name: 'Add mentor' }).click();
  dialog = page.getByRole('dialog', { name: 'Add mentor' });
  await dialog.getByLabel('First name').fill('New'); await dialog.getByLabel('Last name').fill('Mentor');
  await dialog.getByLabel('Email address').fill('newmentor@example.test'); await dialog.getByLabel('Employee code').fill('MT-NEW');
  await dialog.getByLabel('Specialization').fill('Career guidance'); await dialog.getByLabel('Initial password').fill('TestPassword123!');
  await dialog.getByRole('button', { name: 'Create account' }).click(); await dialog.waitFor({ state: 'detached' });
  await row(page, 'New Mentor').waitFor(); pass('Create mentor with specialization');
  await link(page, 'Assignments').click(); await page.getByRole('button', { name: 'New assignment' }).click();
  dialog = page.getByRole('dialog', { name: 'New assignment' });
  const students = dialog.getByRole('group', { name: 'Choose a student', exact: true });
  await students.getByRole('radio').first().waitFor();
  assert.ok(await students.getByRole('radio').nth(1).isDisabled());
  for (let pageNumber = 2; pageNumber <= 21; pageNumber++) {
    await students.getByRole('button', { name: 'Next page' }).click();
    await students.getByText(`106 active students · Page ${pageNumber} of 22`).waitFor();
  }
  await students.getByRole('radio', { name: /Student 101 / }).check();
  const mentors = dialog.getByRole('group', { name: 'Choose a mentor', exact: true });
  await mentors.getByRole('radio', { name: /New Mentor/ }).check();
  await dialog.getByRole('button', { name: 'Assign mentor' }).click(); await dialog.waitFor({ state: 'detached' });
  await row(page, 'Student 101').waitFor();
  assert.ok(state.studentPages.includes(21));
  assert.deepEqual(state.writes.find(w => w.resource === 'assignments' && !w.action)?.body, { studentId: 101, mentorId: 8 });
  pass('Paginated picker reaches beyond 100 people and uses profile IDs');
  await row(page, 'Student 2').getByRole('button', { name: 'Reassign Student 2' }).click();
  dialog = page.getByRole('dialog', { name: 'Reassign student' });
  assert.ok(await dialog.getByRole('radio', { name: /Jordan Lee/ }).isDisabled());
  await dialog.getByRole('radio', { name: /Mina Rao/ }).check();
  state.failTransfer = true;
  await dialog.getByRole('button', { name: 'Transfer student' }).click();
  await dialog.getByRole('alert').filter({ hasText: 'The mentor is no longer active.' }).waitFor();
  assert.ok(state.assignments.find(a => a.mentorStudentId === 11).isActive);
  assert.equal(state.writes.filter(w => w.resource === 'assignments' && w.method === 'PUT').length, 0);
  pass('Failed transfer keeps original assignment and avoids end/create requests');
  state.failTransfer = false;
  await dialog.getByRole('button', { name: 'Transfer student' }).click(); await dialog.waitFor({ state: 'detached' });
  await row(page, 'Student 2').filter({ hasText: 'Mina Rao' }).waitFor();
  assert.equal(state.assignments.filter(a => a.studentId === 2 && a.isActive).length, 1);
  assert.equal(state.assignments.find(a => a.mentorStudentId === 11).isActive, false);
  pass('Transfer creates one active period and preserves the ended period');
  await row(page, 'Student 101').getByRole('button', { name: 'End assignment for Student 101' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Cancel', exact: true }).click();
  assert.ok(state.assignments.find(a => a.studentId === 101).isActive);
  await row(page, 'Student 101').getByRole('button', { name: 'End assignment for Student 101' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'End assignment', exact: true }).click();
  await page.getByRole('dialog').waitFor({ state: 'detached' });
  await row(page, 'Student 101').getByText('Read-only history').waitFor();
  pass('End assignment confirmation and read-only history');
  await page.getByLabel('Search assignments').fill('Student 101'); await page.getByLabel('Search assignments').press('Enter');
  await page.getByText('1 assignments · Page 1 of 1', { exact: true }).waitFor(); await row(page, 'Student 101').waitFor(); pass('Assignment search');
  await page.getByLabel('Status', { exact: true }).selectOption('active');
  await page.getByRole('heading', { name: 'No assignments found' }).waitFor();
  await page.getByRole('button', { name: 'Clear filters' }).click(); await row(page, 'Student 101').waitFor();
  pass('Status filters and empty results');
  await page.setViewportSize({ width: 375, height: 812 }); await noOverflow(page);
  await page.screenshot({ path: `${output}/admin-assignments-mobile.png`, fullPage: true });
  await page.getByRole('button', { name: 'New assignment' }).click();
  await page.getByRole('dialog', { name: 'New assignment' }).waitFor(); await noOverflow(page);
  await page.screenshot({ path: `${output}/admin-assignment-dialog-mobile.png`, fullPage: true });
  await page.keyboard.press('Escape'); await page.getByRole('dialog').waitFor({ state: 'detached' });
  pass('Mobile lists/dialogs, no horizontal scrolling, and Escape');
  await context.close();
  const mentorContext = await browser.newContext(); page = await mentorContext.newPage(); await setup(page, 'Mentor'); await login(page);
  for (const route of ['students', 'mentors', 'assignments']) { await page.goto(`${base}/admin/${route}`); await page.waitForURL('**/mentor/overview'); }
  pass('Non-Admin cannot enter any management screen');
  await mentorContext.close();
  assert.deepEqual(errors, []);
  console.log(`${checks} Admin browser checks passed.`);
} catch (error) {
  if (page && !page.isClosed()) await page.screenshot({ path: `${output}/admin-failure.png`, fullPage: true });
  throw error;
} finally { await browser.close(); await close(); }
