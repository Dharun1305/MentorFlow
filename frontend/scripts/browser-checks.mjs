// Browser checks use controlled API fixtures. They do not replace a real SQL Server integration test.
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile, stat, mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import { chromium } from 'playwright';

const root = path.resolve(fileURLToPath(new URL('../dist/mentorflow/browser/', import.meta.url)));
const output = fileURLToPath(new URL('../test-results/', import.meta.url));
const mime = { '.js': 'text/javascript', '.css': 'text/css', '.svg': 'image/svg+xml', '.html': 'text/html', '.ico': 'image/x-icon' };
const server = createServer(async (request, response) => {
  try {
    let file = path.resolve(root, `.${new URL(request.url, 'http://localhost').pathname}`);
    if (!file.startsWith(root + path.sep)) file = path.join(root, 'index.html');
    try { if (!(await stat(file)).isFile()) file = path.join(root, 'index.html'); }
    catch { file = path.join(root, 'index.html'); }
    response.setHeader('Content-Type', mime[path.extname(file)] ?? 'application/octet-stream');
    response.end(await readFile(file));
  } catch { response.writeHead(500).end(); }
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
const base = `http://127.0.0.1:${server.address().port}`;
const browser = await chromium.launch({ headless: true, executablePath: process.env.CHROME_BIN || undefined, args: ['--no-sandbox', '--disable-dev-shm-usage'] });
const errors = [];
let checks = 0;
const pass = label => { checks++; console.log(`PASS ${label}`); };

function pageData(items, totalCount = items.length) { return { items, totalCount, page: 1, pageSize: 5 }; }
async function fixture(page, role, options = {}) {
  const user = { userId: 700, firstName: 'Alex', lastName: 'Taylor', email: 'alex@example.test', role };
  const assignment = { mentorStudentId: 11, mentorId: 2, studentId: 3, mentorName: 'Jordan Lee', studentName: 'Sam Patel', assignedAt: '2026-10-01T08:00:00Z', isActive: true };
  const student = { studentId: 3, userId: 901, studentCode: 'ST-003', firstName: 'Sam', lastName: 'Patel', email: 'sam@example.test', isActive: true, activeAssignment: assignment };
  const mentor = { mentorId: 2, userId: 902, employeeCode: 'MT-002', firstName: 'Jordan', lastName: 'Lee', specialization: 'Student development', email: 'jordan@example.test', isActive: true, assignments: [assignment] };
  const meeting = { meetingId: 1, mentorStudentId: 11, mentorId: 2, studentId: 3, mentorName: 'Jordan Lee', studentName: 'Sam Patel', meetingAt: '2026-10-14T10:00:00Z', durationMinutes: 30, topic: 'Career goals and next steps', notes: null, feedback: null, isActive: true, rowVersion: 'AAAAAAAAAAE=' };
  const goal = { goalId: 1, mentorStudentId: 11, mentorId: 2, studentId: 3, mentorName: 'Jordan Lee', studentName: 'Sam Patel', title: 'Complete the first learning milestone', description: null, targetDate: '2026-11-01T00:00:00', status: 'In Progress', priority: 'Medium', completedAt: null, isActive: true, rowVersion: 'AAAAAAAAAAE=' };
  page.on('pageerror', error => errors.push(error.message));
  await page.route('**/api/**', async route => {
    const request = route.request();
    const url = new URL(request.url());
    const endpoint = url.pathname;
    if (endpoint === '/api/auth/login') {
      if (request.postDataJSON().password !== 'TestPass123!') return route.fulfill({ status: 401, json: { message: 'Invalid email or password.' } });
      return route.fulfill({ json: { token: `test-${role}`, expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(), user } });
    }
    if (!request.headers()['authorization']) return route.fulfill({ status: 401, json: {} });
    if (endpoint === '/api/auth/me') return route.fulfill({ status: options.rejectSession ? 401 : 200, json: user });
    if (options.unavailable) return route.fulfill({ status: 503, json: { message: 'Workspace temporarily unavailable.' } });
    const records = {
      '/api/students': pageData([student], role === 'Admin' ? 51 : 7),
      '/api/mentors': pageData([mentor], role === 'Admin' ? 8 : 1),
      '/api/assignments': pageData([assignment], 42),
      '/api/meetings': pageData([meeting], 12),
      '/api/goals': pageData([goal], 24),
    };
    const result = records[endpoint];
    if (!result) throw new Error(`Unexpected API call: ${endpoint}`);
    return route.fulfill({ json: options.empty ? pageData([]) : result });
  });
  return user;
}
async function login(page) {
  await page.goto(`${base}/login`);
  await page.getByLabel('Email address').fill('alex@example.test');
  await page.getByLabel('Password', { exact: true }).fill('TestPass123!');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
}
async function noOverflow(page) {
  const fits = await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth);
  assert.equal(fits, true, 'Page must not scroll horizontally');
}
try {
  await mkdir(output, { recursive: true });
  for (const role of ['Admin', 'Mentor', 'Student']) {
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const page = await context.newPage();
    await fixture(page, role);
    await login(page);
    await page.waitForURL(`**/${role.toLowerCase()}/overview`);
    await page.getByRole('heading', { name: 'Hello, Alex.' }).waitFor();
    await page.locator('.metric-card').first().waitFor();
    assert.equal(await page.locator('.metric-card').count(), 3);
    await noOverflow(page);
    await page.screenshot({ path: path.join(output, `${role.toLowerCase()}-desktop.png`), fullPage: true });
    pass(`${role} login and real API totals contract`);
    await page.reload();
    await page.getByRole('heading', { name: 'Hello, Alex.' }).waitFor();
    pass(`${role} session restoration`);
    if (role === 'Mentor') {
      await page.goto(`${base}/admin/overview`);
      await page.waitForURL('**/mentor/overview');
      pass('Mentor cannot enter an Admin route');
      await page.setViewportSize({ width: 375, height: 812 });
      await noOverflow(page);
      await page.getByRole('button', { name: 'Open navigation' }).click();
      await page.locator('.sidebar.open').waitFor();
      await page.waitForFunction(() => document.activeElement?.closest('.sidebar') !== null);
      await page.keyboard.press('Escape');
      await page.waitForFunction(() => !document.querySelector('.sidebar')?.classList.contains('open'));
      await page.screenshot({ path: path.join(output, 'mentor-mobile.png'), fullPage: true });
      pass('Mobile navigation, Escape, and single-column layout');
    }
    if (role === 'Mentor') await page.getByRole('button', { name: 'Open navigation' }).click();
    await page.getByRole('button', { name: 'Sign out', exact: true }).click();
    await page.waitForURL('**/login');
    assert.equal(await page.evaluate(() => sessionStorage.getItem('mentorflow.session')), null);
    pass(`${role} logout clears the session`);
    await context.close();
  }
  const context = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const page = await context.newPage();
  await fixture(page, 'Mentor');
  await page.goto(`${base}/mentor/overview`);
  await page.waitForURL('**/login');
  pass('Unauthenticated deep link returns to login');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  assert.equal(await page.locator('.field-error').filter({ hasText: 'valid email' }).count(), 1);
  pass('Login field validation');
  await page.getByLabel('Email address').fill('alex@example.test');
  await page.getByLabel('Password', { exact: true }).fill('WrongPass123!');
  await page.getByRole('button', { name: 'Show password' }).click();
  assert.equal(await page.getByLabel('Password', { exact: true }).getAttribute('type'), 'text');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.getByRole('alert').filter({ hasText: 'Invalid email or password.' }).waitFor();
  assert.match(page.url(), /\/login$/);
  await noOverflow(page);
  await page.screenshot({ path: path.join(output, 'login-mobile.png'), fullPage: true });
  pass('Invalid credentials and password visibility');
  await context.close();
  for (const state of ['empty', 'unavailable', 'rejectSession']) {
    const context = await browser.newContext();
    const page = await context.newPage();
    const user = await fixture(page, 'Student', { [state]: true });
    if (state === 'rejectSession') {
      await page.addInitScript(() => sessionStorage.setItem('mentorflow.session', JSON.stringify({ token: 'rejected', expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString() })));
      await page.goto(`${base}/student/overview`);
      await page.waitForURL('**/login**');
      assert.equal(await page.evaluate(() => sessionStorage.getItem('mentorflow.session')), null);
    } else {
      await login(page);
      if (state === 'empty') await page.getByRole('heading', { name: 'A connection to look forward to' }).waitFor();
      else await page.getByRole('heading', { name: 'Your workspace couldn’t load' }).waitFor();
    }
    pass(`${state} state`);
    await context.close();
  }
  assert.deepEqual(errors, [], 'No uncaught browser errors');
  console.log(`${checks} browser checks passed. Screenshots: test-results/`);
} finally {
  await browser.close();
  await new Promise(resolve => server.close(resolve));
}
