SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('gh.EncounterAddenda','U') IS NULL
 CREATE TABLE gh.EncounterAddenda(AddendumId bigint IDENTITY PRIMARY KEY,EncounterId bigint NOT NULL REFERENCES gh.Encounters(EncounterId),AuthorUserId int NOT NULL REFERENCES gh.UserAccounts(UserId),Note nvarchar(4000) NOT NULL,RecordedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
COMMIT;
GO
CREATE OR ALTER VIEW gh.vwPatientTimeline
AS
 SELECT patientId AS PatientId,registeredAt AS EventAt,CAST('Registration' AS nvarchar(40)) AS EventType,CAST(patientName AS nvarchar(1000)) AS Detail FROM dbo.patientTbl
 UNION ALL SELECT PatientId,StartedAt,'Encounter',CAST(ISNULL(ChiefComplaint,'Clinical encounter') AS nvarchar(1000)) FROM gh.Encounters
 UNION ALL SELECT PatientId,ScheduledStart,'Appointment',CAST(ISNULL(Reason,Status) AS nvarchar(1000)) FROM gh.Appointments
 UNION ALL SELECT PatientId,OrderedAt,'Laboratory order',Status FROM gh.LabOrders
 UNION ALL SELECT PatientId,PrescribedAt,'Prescription',Status FROM gh.PrescriptionOrders
 UNION ALL SELECT PatientId,AdmittedAt,'Admission',AdmissionReason FROM gh.Admissions
 UNION ALL SELECT PatientId,DischargedAt,'Discharge',CAST(DischargeSummary AS nvarchar(1000)) FROM gh.Admissions WHERE DischargedAt IS NOT NULL
 UNION ALL SELECT PatientId,AssessedAt,'Triage',ChiefComplaint FROM gh.TriageAssessments
 UNION ALL SELECT PatientId,RecordedAt,'Nursing assessment',CareStatus FROM gh.NursingNotes
 UNION ALL SELECT e.PatientId,a.RecordedAt,'Clinical addendum',CAST(a.Note AS nvarchar(1000)) FROM gh.EncounterAddenda a JOIN gh.Encounters e ON e.EncounterId=a.EncounterId;
