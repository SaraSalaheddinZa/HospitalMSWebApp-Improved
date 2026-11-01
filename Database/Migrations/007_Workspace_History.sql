SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('gh.WorkflowHistory','U') IS NULL
 CREATE TABLE gh.WorkflowHistory(HistoryId bigint IDENTITY PRIMARY KEY, EntityType nvarchar(40) NOT NULL, EntityId bigint NOT NULL, ActorUserId int NOT NULL REFERENCES gh.UserAccounts(UserId), PreviousStatus nvarchar(40), NewStatus nvarchar(40), Detail nvarchar(1000), OccurredAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
IF COL_LENGTH('gh.VitalSigns','PatientId') IS NULL
 ALTER TABLE gh.VitalSigns ADD PatientId int NULL REFERENCES dbo.patientTbl(patientId);
ALTER TABLE gh.VitalSigns ALTER COLUMN EncounterId bigint NULL;
COMMIT;
