SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('gh.TriageAssessments','Status') IS NULL
 ALTER TABLE gh.TriageAssessments ADD Status nvarchar(20) NOT NULL CONSTRAINT DF_Triage_Status DEFAULT 'Waiting' WITH VALUES;
COMMIT;
GO
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_Triage_Status')
 ALTER TABLE gh.TriageAssessments ADD CONSTRAINT CK_Triage_Status CHECK(Status IN('Waiting','UnderCare','Resolved'));
