SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF COL_LENGTH('gh.InsuranceClaims','DecisionNote') IS NULL
 ALTER TABLE gh.InsuranceClaims ADD DecisionNote nvarchar(700) NULL, SettlementReference nvarchar(80) NULL, DecidedAt datetime2 NULL, SettledAt datetime2 NULL;
IF COL_LENGTH('gh.Payments','InsuranceClaimId') IS NULL
 ALTER TABLE gh.Payments ADD InsuranceClaimId bigint NULL REFERENCES gh.InsuranceClaims(InsuranceClaimId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('gh.Payments') AND name='UX_Payments_Claim')
 EXEC('CREATE UNIQUE INDEX UX_Payments_Claim ON gh.Payments(InsuranceClaimId) WHERE InsuranceClaimId IS NOT NULL');
IF COL_LENGTH('gh.Encounters','NoteRevision') IS NULL
 ALTER TABLE gh.Encounters ADD NoteRevision int NOT NULL CONSTRAINT DF_Encounters_NoteRevision DEFAULT 0;
IF OBJECT_ID('gh.EncounterDraftHistory','U') IS NULL
 CREATE TABLE gh.EncounterDraftHistory(
  DraftHistoryId bigint IDENTITY PRIMARY KEY,
  EncounterId bigint NOT NULL REFERENCES gh.Encounters(EncounterId),
  Revision int NOT NULL,
  ChiefComplaint nvarchar(max) NULL, Assessment nvarchar(max) NULL, TreatmentPlan nvarchar(max) NULL,
  FollowUpDate date NULL, ChangedByUserId int NOT NULL REFERENCES gh.UserAccounts(UserId),
  ChangedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT UQ_EncounterDraftRevision UNIQUE(EncounterId,Revision));
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('gh.WorkflowHistory') AND name='IX_WorkflowHistory_Entity')
 CREATE INDEX IX_WorkflowHistory_Entity ON gh.WorkflowHistory(EntityType,EntityId,OccurredAt) INCLUDE(ActorUserId,NewStatus);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('gh.InsuranceClaims') AND name='IX_Claims_InvoiceStatus')
 CREATE INDEX IX_Claims_InvoiceStatus ON gh.InsuranceClaims(InvoiceId,Status) INCLUDE(ClaimedAmount,ApprovedAmount);
COMMIT;
