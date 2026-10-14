# MentorFlow frontend

Angular 20 frontend for the standalone MentorFlow mentoring platform. Uses standalone components, Reactive Forms, HttpClient, signals, and plain HTML/SCSS. No GoodBooks components or dependencies.

## Implemented in this milestone

- Responsive blue-and-white login page and application shell.
- Login, logout, session restoration through `/api/auth/me`, and automatic expiry.
- API-only Bearer token interceptor and server rejection handling.
- Admin/Mentor/Student route guards and role-specific overview screens.
- Read-only counts and record previews from the existing paginated APIs.
- Loading, empty, error and retry states; keyboard-accessible mobile navigation.

Student/mentor forms, assignment management, and meeting/goal editing are upcoming features, visibly marked as such. This milestone does not include those forms or new analytics endpoints.

## Start locally

Requires Node `^20.19.0`, `^22.12.0`, or `^24.0.0`. The existing Node 22.19 installation is supported.

First update and start the backend as described in `../docs/backend-setup.md`. The database upgrade must be applied to an existing MentorFlow database.

```sh
cd frontend
npm ci
npm start
```

Open `http://localhost:4200`. Angular proxies `/api` to `http://localhost:5180` using `proxy.conf.json`. Sign in with a backend account; there are no hardcoded frontend accounts or demo data.

The role comes from the API, not from a role selector. Tokens and expiry are held in sessionStorage for the current tab; passwords and user profiles are not stored. Reload validates the saved token through `/auth/me`. Temporary session-check failures show a retry action and do not grant access.

## API contract

Typed API models are in `src/app/core/models.ts`. Profile IDs and account IDs are distinct. Overview counts use `totalCount`, never the number of rows on the current page. Meeting times display in the browser's local timezone.

The upcoming edit forms must retain `rowVersion` and send it on PUT. A 409 response must prompt reload/review rather than silently overwrite another edit. The API remains responsible for authorization and ownership checks.

## Verification

```sh
npm run build
npm test -- --watch=false
npx playwright install chromium
npm run test:e2e
```

`npm run test:ci` uses the explicit ChromeHeadlessNoSandbox launcher for CI containers. Set `CHROME_BIN` if Chrome is not in its usual location. Use the normal ChromeHeadless launcher on development machines.

Browser checks serve the production build locally and intercept API calls with controlled fixtures. They cover all three roles, restoration, logout, cross-role routing, validation, API errors, mobile overflow/navigation, and rejected sessions. Screenshots are written to ignored `test-results/`. These tests do not verify real SQL Server connectivity or persistence.

## Deployment

`npm run build` creates `dist/mentorflow/browser`. Configure the web host to serve `index.html` for Angular routes and reverse-proxy `/api` to the backend. The development proxy does not run in a production build.

If deploying the API on a different origin, change `src/app/core/api.config.ts` to that exact API base URL and configure backend CORS for the frontend origin. The interceptor attaches tokens only to the configured API origin/path. Use HTTPS for deployment.
