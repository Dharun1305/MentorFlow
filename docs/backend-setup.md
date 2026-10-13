# Backend setup and frontend API contract

The backend is a standalone ASP.NET Core / EF Core / SQL Server application. It has no GoodBooks dependencies.

## Database

For a new database, execute `database/MentorFlowDb.sql` in SQL Server.
For an existing database, execute `database/upgrades/001_backend_integrity.sql` before running this version of the API.
The upgrade adds meeting/goal row versions, enforces one active mentorship per student, and permits separate periods for repeated mentor/student pairs. It stops without changing the schema if duplicate active assignments or unsupported goal statuses need review. Re-running it is supported. Original assignment history that was overwritten by older code cannot be reconstructed.

The schema remains managed by versioned SQL scripts; the API does not automatically migrate it.

## Configuration and startup

Development (`ASPNETCORE_ENVIRONMENT=Development`) uses the local signing key and demo seed settings in `appsettings.Development.json`. Demo seeding requires an existing database. Swagger is available only in Development.

For deployed environments, supply:

- `ConnectionStrings__DefaultConnection`: SQL Server connection string.
- `Jwt__Key`: a private random signing secret of at least 32 UTF-8 bytes. Do not reuse the development key.
- `Cors__AllowedOrigins__0`: the exact frontend origin, such as `https://mentorflow.example.com`. Additional origins use indexes 1, 2, etc.

Demo seeding is disabled outside Development even if `Seed:Enabled` is true. Disabling seeding does not remove existing demo users; deactivate them when preparing an existing database for deployment.

```sh
dotnet run --project backend/MentorFlow.API --launch-profile MentorFlow.API
dotnet test backend/MentorFlow.Tests/MentorFlow.Tests.csproj
```

## List responses

All five list endpoints (`students`, `mentors`, `assignments`, `meetings`, `goals`) return:

```json
{ "items": [], "totalCount": 0, "page": 1, "pageSize": 20 }
```

Use `page` (minimum 1) and `pageSize` (1–100). List ordering is stable. Filters never expand the caller's role-based access.

- Students/mentors: `search` matches name, email or profile code; `isActive` filters profile activation.
- Assignments: `studentId`, `mentorId`, `isActive`.
- Meetings: `studentId`, `mentorId`, `from`, `to` (inclusive timestamps, with an offset or `Z`).
- Goals: `studentId`, `mentorId`, `status` (`NotStarted`, `InProgress`, `Completed`; displayed status names also accepted).

## Dates and concurrent edits

Send `meetingAt` as an ISO timestamp with an explicit offset, for example `2026-10-13T18:00:00+05:30`, or `2026-10-13T12:30:00Z`. The API stores and returns meeting times in UTC. Goal target dates are calendar dates, not timestamps.

Meeting and goal responses contain a base64 `rowVersion`. Return that exact value on PUT. A missing/invalid version returns 400; a stale version returns 409. Reload the record and let the user review changes rather than silently retrying with a new version.

## Assignment and access rules

Each student has at most one active assignment. End a period with `PUT /api/assignments/{id}` and `{ "isActive": false }`; start another with POST. An ended period cannot be reactivated. Reassigning the same mentor creates a new row and preserves the original dates and records.

Both profile update with `isActive: false` and DELETE deactivate the account and end active assignments. Reactivating a profile does not restore its earlier assignments.

Admins, the original mentor, and the student can read historical meeting/goal records. Writing requires an active assignment and active accounts; ended periods are read-only, including for admins. A newly assigned mentor does not automatically gain access to another mentor's historical records. Student-facing mentor responses contain only the requesting student's assignment.

JWTs are checked against current user, role, and profile activation on every authenticated request. Deactivated accounts and changed roles invalidate existing tokens for subsequent requests.

## Errors

Validation and service errors use camelCase `message` and `errors`. Database conflicts and stale edits return 409. Unexpected database failures return 500 and are logged. Authentication/authorization failures use 401/403.

## Remaining feature work

Dashboard aggregates, goal type (short/long term), feedback revision history, and a progress timeline are separate features. They are not introduced by this backend integrity change.

Regression tests use EF InMemory for service behavior and inspect the SQL Server model. A real SQL Server smoke test is still required to verify the upgrade script, database-enforced uniqueness under simultaneous writes, and rowversion behavior.
