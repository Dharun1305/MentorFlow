/*
    MentorFlow V1 database schema
    Target: Microsoft SQL Server

    Notes:
    - All timestamps are stored in UTC.
    - UpdatedAt has a default for inserts; the API is responsible for updating
      it when an entity changes.
    - PasswordHash stores only a backend-generated password hash. Plain-text
      passwords must never be stored in this database.
*/

USE [master];
GO

IF DB_ID(N'MentorFlowDb') IS NULL
BEGIN
    CREATE DATABASE [MentorFlowDb];
END;
GO

USE [MentorFlowDb];
GO

/* --------------------------------------------------------------------------
   Reference and identity tables
   -------------------------------------------------------------------------- */

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RoleId      int IDENTITY(1,1) NOT NULL,
        Name        nvarchar(50) NOT NULL,
        Description nvarchar(250) NULL,
        CreatedAt   datetime2(0) NOT NULL
            CONSTRAINT DF_Roles_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt   datetime2(0) NOT NULL
            CONSTRAINT DF_Roles_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        IsActive    bit NOT NULL
            CONSTRAINT DF_Roles_IsActive DEFAULT (1),

        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RoleId),
        CONSTRAINT UQ_Roles_Name UNIQUE (Name)
    );
END;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId       int IDENTITY(1,1) NOT NULL,
        RoleId       int NOT NULL,
        FirstName    nvarchar(100) NOT NULL,
        LastName     nvarchar(100) NOT NULL,
        Email        nvarchar(320) NOT NULL,
        PasswordHash nvarchar(500) NOT NULL,
        CreatedAt    datetime2(0) NOT NULL
            CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt    datetime2(0) NOT NULL
            CONSTRAINT DF_Users_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        IsActive     bit NOT NULL
            CONSTRAINT DF_Users_IsActive DEFAULT (1),

        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId)
            REFERENCES dbo.Roles (RoleId)
    );
END;
GO

/* --------------------------------------------------------------------------
   Mentor and student profiles
   -------------------------------------------------------------------------- */

IF OBJECT_ID(N'dbo.Students', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Students
    (
        StudentId  int IDENTITY(1,1) NOT NULL,
        UserId     int NOT NULL,
        StudentCode nvarchar(50) NOT NULL,
        CreatedAt  datetime2(0) NOT NULL
            CONSTRAINT DF_Students_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt  datetime2(0) NOT NULL
            CONSTRAINT DF_Students_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        IsActive   bit NOT NULL
            CONSTRAINT DF_Students_IsActive DEFAULT (1),

        CONSTRAINT PK_Students PRIMARY KEY CLUSTERED (StudentId),
        CONSTRAINT UQ_Students_UserId UNIQUE (UserId),
        CONSTRAINT UQ_Students_StudentCode UNIQUE (StudentCode),
        CONSTRAINT FK_Students_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users (UserId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Mentors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Mentors
    (
        MentorId      int IDENTITY(1,1) NOT NULL,
        UserId        int NOT NULL,
        EmployeeCode  nvarchar(50) NOT NULL,
        Specialization nvarchar(150) NULL,
        CreatedAt     datetime2(0) NOT NULL
            CONSTRAINT DF_Mentors_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt     datetime2(0) NOT NULL
            CONSTRAINT DF_Mentors_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        IsActive      bit NOT NULL
            CONSTRAINT DF_Mentors_IsActive DEFAULT (1),

        CONSTRAINT PK_Mentors PRIMARY KEY CLUSTERED (MentorId),
        CONSTRAINT UQ_Mentors_UserId UNIQUE (UserId),
        CONSTRAINT UQ_Mentors_EmployeeCode UNIQUE (EmployeeCode),
        CONSTRAINT FK_Mentors_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users (UserId)
    );
END;
GO

/* --------------------------------------------------------------------------
   Mentorship workflow tables
   -------------------------------------------------------------------------- */

IF OBJECT_ID(N'dbo.MentorStudents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MentorStudents
    (
        MentorStudentId int IDENTITY(1,1) NOT NULL,
        MentorId        int NOT NULL,
        StudentId       int NOT NULL,
        AssignedAt      datetime2(0) NOT NULL
            CONSTRAINT DF_MentorStudents_AssignedAt DEFAULT (SYSUTCDATETIME()),
        UnassignedAt    datetime2(0) NULL,
        CreatedAt       datetime2(0) NOT NULL
            CONSTRAINT DF_MentorStudents_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       datetime2(0) NOT NULL
            CONSTRAINT DF_MentorStudents_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        IsActive        bit NOT NULL
            CONSTRAINT DF_MentorStudents_IsActive DEFAULT (1),

        CONSTRAINT PK_MentorStudents PRIMARY KEY CLUSTERED (MentorStudentId),
        CONSTRAINT CK_MentorStudents_AssignmentDates
            CHECK (UnassignedAt IS NULL OR UnassignedAt >= AssignedAt),
        CONSTRAINT FK_MentorStudents_Mentors FOREIGN KEY (MentorId)
            REFERENCES dbo.Mentors (MentorId),
        CONSTRAINT FK_MentorStudents_Students FOREIGN KEY (StudentId)
            REFERENCES dbo.Students (StudentId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Meetings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Meetings
    (
        MeetingId       int IDENTITY(1,1) NOT NULL,
        MentorStudentId int NOT NULL,
        MeetingAt       datetime2(0) NOT NULL,
        DurationMinutes smallint NULL,
        Topic           nvarchar(200) NOT NULL,
        Notes           nvarchar(max) NULL,
        Feedback        nvarchar(max) NULL,
        CreatedAt       datetime2(0) NOT NULL
            CONSTRAINT DF_Meetings_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       datetime2(0) NOT NULL
            CONSTRAINT DF_Meetings_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        RowVersion      rowversion NOT NULL,
        IsActive        bit NOT NULL
            CONSTRAINT DF_Meetings_IsActive DEFAULT (1),

        CONSTRAINT PK_Meetings PRIMARY KEY CLUSTERED (MeetingId),
        CONSTRAINT CK_Meetings_DurationMinutes
            CHECK (DurationMinutes IS NULL OR DurationMinutes BETWEEN 1 AND 1440),
        CONSTRAINT FK_Meetings_MentorStudents FOREIGN KEY (MentorStudentId)
            REFERENCES dbo.MentorStudents (MentorStudentId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Goals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Goals
    (
        GoalId          int IDENTITY(1,1) NOT NULL,
        MentorStudentId int NOT NULL,
        Title           nvarchar(200) NOT NULL,
        Description     nvarchar(max) NULL,
        TargetDate      date NULL,
        Status          nvarchar(20) NOT NULL
            CONSTRAINT DF_Goals_Status DEFAULT (N'NotStarted'),
        Priority        nvarchar(20) NOT NULL
            CONSTRAINT DF_Goals_Priority DEFAULT (N'Medium'),
        CompletedAt     datetime2(0) NULL,
        CreatedAt       datetime2(0) NOT NULL
            CONSTRAINT DF_Goals_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       datetime2(0) NOT NULL
            CONSTRAINT DF_Goals_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        RowVersion      rowversion NOT NULL,
        IsActive        bit NOT NULL
            CONSTRAINT DF_Goals_IsActive DEFAULT (1),

        CONSTRAINT PK_Goals PRIMARY KEY CLUSTERED (GoalId),
        CONSTRAINT CK_Goals_Status
            CHECK (Status IN (N'NotStarted', N'InProgress', N'Completed')),
        CONSTRAINT CK_Goals_Priority
            CHECK (Priority IN (N'Low', N'Medium', N'High')),
        CONSTRAINT FK_Goals_MentorStudents FOREIGN KEY (MentorStudentId)
            REFERENCES dbo.MentorStudents (MentorStudentId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_MentorStudents_ActiveStudent' AND object_id = OBJECT_ID(N'dbo.MentorStudents'))
    CREATE UNIQUE INDEX UX_MentorStudents_ActiveStudent ON dbo.MentorStudents (StudentId) WHERE IsActive = 1;
GO

/* --------------------------------------------------------------------------
   Supporting indexes
   -------------------------------------------------------------------------- */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Users_RoleId_IsActive'
      AND object_id = OBJECT_ID(N'dbo.Users')
)
BEGIN
    CREATE INDEX IX_Users_RoleId_IsActive
        ON dbo.Users (RoleId, IsActive);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_MentorStudents_StudentId_IsActive'
      AND object_id = OBJECT_ID(N'dbo.MentorStudents')
)
BEGIN
    CREATE INDEX IX_MentorStudents_StudentId_IsActive
        ON dbo.MentorStudents (StudentId, IsActive);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_MentorStudents_MentorId_IsActive'
      AND object_id = OBJECT_ID(N'dbo.MentorStudents')
)
BEGIN
    CREATE INDEX IX_MentorStudents_MentorId_IsActive
        ON dbo.MentorStudents (MentorId, IsActive);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Meetings_MentorStudentId_MeetingAt'
      AND object_id = OBJECT_ID(N'dbo.Meetings')
)
BEGIN
    CREATE INDEX IX_Meetings_MentorStudentId_MeetingAt
        ON dbo.Meetings (MentorStudentId, MeetingAt DESC);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Goals_MentorStudentId_Status'
      AND object_id = OBJECT_ID(N'dbo.Goals')
)
BEGIN
    CREATE INDEX IX_Goals_MentorStudentId_Status
        ON dbo.Goals (MentorStudentId, Status);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Goals_MentorStudentId_TargetDate'
      AND object_id = OBJECT_ID(N'dbo.Goals')
)
BEGIN
    CREATE INDEX IX_Goals_MentorStudentId_TargetDate
        ON dbo.Goals (MentorStudentId, TargetDate);
END;
GO

/* --------------------------------------------------------------------------
   Minimal development seed data
   -------------------------------------------------------------------------- */

DECLARE @AdminRoleId int;
DECLARE @MentorRoleId int;
DECLARE @StudentRoleId int;

SELECT @AdminRoleId = RoleId FROM dbo.Roles WHERE Name = N'Admin';
IF @AdminRoleId IS NULL
BEGIN
    INSERT INTO dbo.Roles (Name, Description)
    VALUES (N'Admin', N'Development administrator role');
    SET @AdminRoleId = CONVERT(int, SCOPE_IDENTITY());
END;

SELECT @MentorRoleId = RoleId FROM dbo.Roles WHERE Name = N'Mentor';
IF @MentorRoleId IS NULL
BEGIN
    INSERT INTO dbo.Roles (Name, Description)
    VALUES (N'Mentor', N'Development mentor role');
    SET @MentorRoleId = CONVERT(int, SCOPE_IDENTITY());
END;

SELECT @StudentRoleId = RoleId FROM dbo.Roles WHERE Name = N'Student';
IF @StudentRoleId IS NULL
BEGIN
    INSERT INTO dbo.Roles (Name, Description)
    VALUES (N'Student', N'Development student role');
    SET @StudentRoleId = CONVERT(int, SCOPE_IDENTITY());
END;

DECLARE @AdminUserId int;
DECLARE @MentorUserId int;
DECLARE @StudentUserId int;

SELECT @AdminUserId = UserId
FROM dbo.Users
WHERE Email = N'admin.dev@mentorflow.local';
IF @AdminUserId IS NULL
BEGIN
    INSERT INTO dbo.Users
    (
        RoleId, FirstName, LastName, Email, PasswordHash
    )
    VALUES
    (
        @AdminRoleId,
        N'Dev',
        N'Admin',
        N'admin.dev@mentorflow.local',
        N'DEV_ONLY_REPLACE_WITH_A_REAL_PASSWORD_HASH'
    );
    SET @AdminUserId = CONVERT(int, SCOPE_IDENTITY());
END;

SELECT @MentorUserId = UserId
FROM dbo.Users
WHERE Email = N'mentor.dev@mentorflow.local';
IF @MentorUserId IS NULL
BEGIN
    INSERT INTO dbo.Users
    (
        RoleId, FirstName, LastName, Email, PasswordHash
    )
    VALUES
    (
        @MentorRoleId,
        N'Dev',
        N'Mentor',
        N'mentor.dev@mentorflow.local',
        N'DEV_ONLY_REPLACE_WITH_A_REAL_PASSWORD_HASH'
    );
    SET @MentorUserId = CONVERT(int, SCOPE_IDENTITY());
END;

SELECT @StudentUserId = UserId
FROM dbo.Users
WHERE Email = N'student.dev@mentorflow.local';
IF @StudentUserId IS NULL
BEGIN
    INSERT INTO dbo.Users
    (
        RoleId, FirstName, LastName, Email, PasswordHash
    )
    VALUES
    (
        @StudentRoleId,
        N'Dev',
        N'Student',
        N'student.dev@mentorflow.local',
        N'DEV_ONLY_REPLACE_WITH_A_REAL_PASSWORD_HASH'
    );
    SET @StudentUserId = CONVERT(int, SCOPE_IDENTITY());
END;

DECLARE @MentorId int;
DECLARE @StudentId int;
DECLARE @MentorStudentId int;

SELECT @MentorId = MentorId
FROM dbo.Mentors
WHERE EmployeeCode = N'DEV-MENTOR-001';
IF @MentorId IS NULL
BEGIN
    INSERT INTO dbo.Mentors (UserId, EmployeeCode, Specialization)
    VALUES (@MentorUserId, N'DEV-MENTOR-001', N'General Student Development');
    SET @MentorId = CONVERT(int, SCOPE_IDENTITY());
END;

SELECT @StudentId = StudentId
FROM dbo.Students
WHERE StudentCode = N'DEV-STUDENT-001';
IF @StudentId IS NULL
BEGIN
    INSERT INTO dbo.Students (UserId, StudentCode)
    VALUES (@StudentUserId, N'DEV-STUDENT-001');
    SET @StudentId = CONVERT(int, SCOPE_IDENTITY());
END;

SELECT @MentorStudentId = MentorStudentId
FROM dbo.MentorStudents
WHERE MentorId = @MentorId
  AND StudentId = @StudentId;
IF @MentorStudentId IS NULL
BEGIN
    INSERT INTO dbo.MentorStudents (MentorId, StudentId)
    VALUES (@MentorId, @StudentId);
    SET @MentorStudentId = CONVERT(int, SCOPE_IDENTITY());
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Meetings
    WHERE MentorStudentId = @MentorStudentId
      AND Topic = N'DEV - Initial mentorship meeting'
)
BEGIN
    INSERT INTO dbo.Meetings
    (
        MentorStudentId, MeetingAt, DurationMinutes, Topic, Notes, Feedback
    )
    VALUES
    (
        @MentorStudentId,
        '2026-01-15T10:00:00',
        60,
        N'DEV - Initial mentorship meeting',
        N'Development seed meeting for local testing.',
        N'DEV - Student is engaged and ready to define initial goals.'
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Goals
    WHERE MentorStudentId = @MentorStudentId
      AND Title = N'DEV - Complete onboarding plan'
)
BEGIN
    INSERT INTO dbo.Goals
    (
        MentorStudentId, Title, Description, TargetDate, Status, Priority
    )
    VALUES
    (
        @MentorStudentId,
        N'DEV - Complete onboarding plan',
        N'Finish the initial MentorFlow student onboarding checklist.',
        '2026-02-15',
        N'InProgress',
        N'High'
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Goals
    WHERE MentorStudentId = @MentorStudentId
      AND Title = N'DEV - Prepare monthly progress reflection'
)
BEGIN
    INSERT INTO dbo.Goals
    (
        MentorStudentId, Title, Description, TargetDate, Status, Priority
    )
    VALUES
    (
        @MentorStudentId,
        N'DEV - Prepare monthly progress reflection',
        N'Write a short reflection before the next mentoring meeting.',
        '2026-02-28',
        N'NotStarted',
        N'Medium'
    );
END;
GO
