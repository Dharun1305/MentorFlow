# MentorFlow

Implementation status: this repository currently contains the backend and SQL schema. The Angular screens below describe the planned frontend.

See [backend setup and API contract](docs/backend-setup.md) before running the API or connecting Angular.

### Student Mentorship & Progress Platform

MentorFlow is a full-stack web application for managing the student mentoring lifecycle between **Admin, Mentor, and Student**.

The goal is to replace scattered/manual mentoring records with one simple system for managing mentor assignment, meetings, feedback, goals, and student progress.

---

## Core Flow

```text
Admin
  ↓
Create Students & Mentors
  ↓
Assign Mentor to Student
  ↓
Mentor
  ↓
View Assigned Students
  ↓
Conduct Mentoring Meeting
  ↓
Add Feedback
  ↓
Create Student Goal
  ↓
Track Progress
  ↓
Student
  ↓
View Mentor, Meetings, Feedback & Goals
```

---

## User Roles

### Admin

```text
Dashboard
Students
Mentors
Mentor Assignment
```

### Mentor

```text
Dashboard
My Students
Student Profile
Meetings
Feedback
Goals
```

### Student

```text
Dashboard
My Mentor
Meetings
Feedback
Goals
Progress
```

---

## Main Features

- Authentication & role-based access
- Student management
- Mentor management
- Mentor-student assignment
- Mentoring meetings
- Feedback
- Student goals
- Goal progress tracking
- Student progress timeline
- Role-based dashboards

---

## Technology

```text
Frontend  → Angular + TypeScript + SCSS
Backend   → ASP.NET Core Web API
ORM       → Entity Framework Core
Database  → SQL Server
API Docs  → Swagger
Version   → Git + GitHub
```

---

## Application Architecture

```text
Angular
   ↓
Services
   ↓
REST API
   ↓
ASP.NET Core
   ↓
EF Core
   ↓
SQL Server
```

Frontend structure:

```text
core/
shared/
layout/
features/
 ├── auth/
 ├── admin/
 ├── mentor/
 └── student/
```

Backend structure:

```text
Controllers/
Services/
DTOs/
Entities/
Data/
Repositories/
```

---

## Core Entities

```text
User
Role
Student
Mentor
MentorStudent
Meeting
Goal
```

Relationships:

```text
User
 ├── Student
 └── Mentor

Mentor
 └── MentorStudent
       └── Student

Mentor ── Meeting ── Student

Mentor ── Goal ──── Student
```

---

## V1 Scope

### Build First

```text
1. Project setup
2. Database
3. Authentication
4. Admin
5. Mentor assignment
6. Mentor workflow
7. Student workflow
8. Progress timeline
9. Dashboard
10. Responsive UI
```

### Do Not Build Yet

```text
AI
Advanced analytics
Notifications
Parent portal
Payments
Multi-tenancy
Mobile app
ERP integrations
```

These can be added after V1 is stable.

---

## Development Strategy

Each feature must complete the full flow:

```text
UI
 ↓
Angular Component
 ↓
Service
 ↓
API
 ↓
Backend Service
 ↓
Database
 ↓
Response
 ↓
UI Update
```

Build one working module at a time.

```text
Foundation
   ↓
Authentication
   ↓
Admin
   ↓
Mentor
   ↓
Student
   ↓
Progress
   ↓
Polish
```

---

## V1 Success Criteria

MentorFlow V1 is complete when:

```text
Admin
 → creates student
 → creates mentor
 → assigns mentor

Mentor
 → views student
 → records meeting
 → adds feedback
 → creates goal

Student
 → views mentor
 → views meeting
 → views feedback
 → tracks goal
```

The complete workflow must work through **Angular → API → SQL Server → Angular**.

---

## Project Direction

```text
V1
Mentorship Management
       ↓
V2
Student Progress
       ↓
V3
Analytics & Interventions
       ↓
V4
Multi-College Platform
       ↓
V5
AI-powered Student Success
```