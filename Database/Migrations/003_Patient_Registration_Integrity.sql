/*
  Guardian Health migration 003
  The legacy patientTbl.addBy column references receptionistTbl and therefore cannot
  accurately store a modern UserAccounts identifier. Keep the historical link when
  registration is performed by a receptionist, but permit NULL for administrator or
  automated registrations; the actor is retained in gh.AuditEvents instead.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.patientTbl', N'U') IS NULL
    THROW 51000, 'dbo.patientTbl is required before applying migration 003.', 1;

IF COL_LENGTH(N'dbo.patientTbl', N'addBy') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.patientTbl') AND name=N'addBy' AND is_nullable=0)
BEGIN
    ALTER TABLE dbo.patientTbl ALTER COLUMN addBy int NULL;
END;
GO
