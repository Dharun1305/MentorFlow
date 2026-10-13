/* Run once against an existing MentorFlowDb before deploying this API.
   Preflight failures leave the schema unchanged. No mentorship records are deleted. */
USE [MentorFlowDb];
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS (SELECT StudentId FROM dbo.MentorStudents WHERE IsActive = 1 GROUP BY StudentId HAVING COUNT(*) > 1)
BEGIN
    ROLLBACK;
    THROW 50001, 'Resolve duplicate active mentor assignments before running this upgrade.', 1;
END;
IF EXISTS (SELECT 1 FROM dbo.Goals WHERE Status NOT IN (N'NotStarted', N'InProgress', N'Completed'))
BEGIN
    ROLLBACK;
    THROW 50002, 'Review unsupported goal statuses before running this upgrade.', 1;
END;
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_MentorStudents_Mentor_Student' AND parent_object_id = OBJECT_ID(N'dbo.MentorStudents'))
    ALTER TABLE dbo.MentorStudents DROP CONSTRAINT UQ_MentorStudents_Mentor_Student;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_MentorStudents_ActiveStudent' AND object_id = OBJECT_ID(N'dbo.MentorStudents'))
    CREATE UNIQUE INDEX UX_MentorStudents_ActiveStudent ON dbo.MentorStudents (StudentId) WHERE IsActive = 1;
IF COL_LENGTH(N'dbo.Meetings', N'RowVersion') IS NULL
    ALTER TABLE dbo.Meetings ADD RowVersion rowversion NOT NULL;
IF COL_LENGTH(N'dbo.Goals', N'RowVersion') IS NULL
    ALTER TABLE dbo.Goals ADD RowVersion rowversion NOT NULL;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Goals_Status' AND parent_object_id = OBJECT_ID(N'dbo.Goals'))
    ALTER TABLE dbo.Goals DROP CONSTRAINT CK_Goals_Status;
ALTER TABLE dbo.Goals WITH CHECK ADD CONSTRAINT CK_Goals_Status CHECK (Status IN (N'NotStarted', N'InProgress', N'Completed'));
COMMIT;
GO
