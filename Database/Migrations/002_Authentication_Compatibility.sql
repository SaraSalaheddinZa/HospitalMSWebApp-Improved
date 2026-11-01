/*
   Guardian Health authentication compatibility migration.
   Run after 001_GuardianHealth_Core.sql. It is idempotent and preserves every
   legacy record. Legacy passwords are never copied into the new account store;
   the first successful legacy sign-in upgrades that one credential to PBKDF2.
*/
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF OBJECT_ID(N'gh.UserAccounts', N'U') IS NULL
    THROW 51010, 'Apply 001_GuardianHealth_Core.sql before this migration.', 1;
BEGIN TRANSACTION;

IF COL_LENGTH(N'gh.UserAccounts', N'LegacyUserId') IS NULL
    ALTER TABLE gh.UserAccounts ADD LegacyUserId int NULL;
IF COL_LENGTH(N'gh.UserAccounts', N'AuthVersion') IS NULL
    ALTER TABLE gh.UserAccounts ADD AuthVersion int NOT NULL CONSTRAINT DF_UserAccounts_AuthVersion DEFAULT 1;

GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'gh.UserAccounts') AND name=N'UX_UserAccounts_LegacyRole')
    CREATE UNIQUE INDEX UX_UserAccounts_LegacyRole ON gh.UserAccounts(RoleName, LegacyUserId) WHERE LegacyUserId IS NOT NULL;

/* The legacy laboratorian table had no status column and a 40-character password
   field, which cannot hold a PBKDF2 representation. Extend it without changing
   users or passwords, so a successful legacy sign-in can be migrated safely. */
IF COL_LENGTH(N'dbo.laboratorianTbl', N'labStatus') IS NULL
    ALTER TABLE dbo.laboratorianTbl ADD labStatus varchar(50) NULL;

GO

UPDATE dbo.laboratorianTbl SET labStatus='Active' WHERE labStatus IS NULL OR LTRIM(RTRIM(labStatus))='';
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.laboratorianTbl') AND name=N'labStatus' AND is_nullable=1)
    ALTER TABLE dbo.laboratorianTbl ALTER COLUMN labStatus varchar(50) NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.laboratorianTbl') AND parent_column_id=COLUMNPROPERTY(OBJECT_ID(N'dbo.laboratorianTbl'),N'labStatus','ColumnId'))
    ALTER TABLE dbo.laboratorianTbl ADD CONSTRAINT DF_laboratorianTbl_labStatus DEFAULT 'Active' FOR labStatus;
IF COL_LENGTH(N'dbo.laboratorianTbl', N'labPassword') IS NOT NULL
    ALTER TABLE dbo.laboratorianTbl ALTER COLUMN labPassword nvarchar(256) NOT NULL;

/* Every existing patient receives an extensible profile. */
INSERT INTO gh.PatientProfiles(PatientId)
SELECT p.patientId FROM dbo.patientTbl p
WHERE NOT EXISTS (SELECT 1 FROM gh.PatientProfiles pp WHERE pp.PatientId=p.patientId);

/* Ensure baseline departments are available to staff, scheduling and reporting. */
MERGE gh.Departments AS target
USING (VALUES
    (N'GEN',N'General Medicine',N'Ground floor',N''),
    (N'LAB',N'Laboratory',N'Diagnostics wing',N''),
    (N'PHARM',N'Pharmacy',N'Main lobby',N''),
    (N'ER',N'Emergency Department',N'Emergency entrance',N'')) AS source(DepartmentCode,DepartmentName,Location,Phone)
ON target.DepartmentCode=source.DepartmentCode
WHEN NOT MATCHED THEN INSERT(DepartmentCode,DepartmentName,Location,Phone) VALUES(source.DepartmentCode,source.DepartmentName,source.Location,source.Phone);

MERGE gh.SystemSettings AS target
USING (VALUES (N'AppointmentDefaultMinutes',N'20'),(N'HospitalName',N'Guardian Health')) AS source(SettingKey,SettingValue)
ON target.SettingKey=source.SettingKey
WHEN NOT MATCHED THEN INSERT(SettingKey,SettingValue) VALUES(source.SettingKey,source.SettingValue);

COMMIT TRANSACTION;
GO
