SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
-- Historical admissions remain unlinked. Do not guess an emergency episode from dates.
IF COL_LENGTH('gh.Admissions','TriageAssessmentId') IS NULL
    ALTER TABLE gh.Admissions ADD TriageAssessmentId bigint NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('gh.TriageAssessments') AND name='UX_Triage_PatientAssessment')
    CREATE UNIQUE INDEX UX_Triage_PatientAssessment ON gh.TriageAssessments(PatientId,TriageAssessmentId);
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Admission_PatientTriage')
    EXEC('ALTER TABLE gh.Admissions WITH CHECK ADD CONSTRAINT FK_Admission_PatientTriage FOREIGN KEY(PatientId,TriageAssessmentId) REFERENCES gh.TriageAssessments(PatientId,TriageAssessmentId)');
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('gh.Admissions') AND name='UX_Admissions_Triage')
    EXEC('CREATE UNIQUE INDEX UX_Admissions_Triage ON gh.Admissions(TriageAssessmentId) WHERE TriageAssessmentId IS NOT NULL');
COMMIT;
