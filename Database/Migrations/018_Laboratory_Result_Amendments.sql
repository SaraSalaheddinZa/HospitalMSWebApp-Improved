SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('gh.LabOrders','ResultVersion') IS NULL
    ALTER TABLE gh.LabOrders ADD ResultVersion int NOT NULL CONSTRAINT DF_LabOrders_ResultVersion DEFAULT 0 CONSTRAINT CK_LabOrders_ResultVersion CHECK(ResultVersion>=0);
IF OBJECT_ID('gh.LabResultAmendments','U') IS NULL
BEGIN
    CREATE TABLE gh.LabResultAmendments(
        LabResultAmendmentId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_LabResultAmendments PRIMARY KEY,
        LabOrderItemId bigint NOT NULL,
        Revision int NOT NULL,
        PreviousValue nvarchar(250) NOT NULL,
        PreviousUnit nvarchar(40) NULL,
        PreviousReferenceRange nvarchar(250) NULL,
        PreviousFlag nvarchar(15) NOT NULL,
        ResultValue nvarchar(250) NOT NULL,
        ResultUnit nvarchar(40) NULL,
        ReferenceRange nvarchar(250) NULL,
        ResultFlag nvarchar(15) NOT NULL,
        Reason nvarchar(700) NOT NULL,
        AmendedByUserId int NOT NULL,
        AmendedAt datetime2(0) NOT NULL CONSTRAINT DF_LabResultAmendments_At DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_LabResultAmendments_Revision UNIQUE(LabOrderItemId,Revision),
        CONSTRAINT FK_LabResultAmendments_Item FOREIGN KEY(LabOrderItemId) REFERENCES gh.LabOrderItems(LabOrderItemId),
        CONSTRAINT FK_LabResultAmendments_User FOREIGN KEY(AmendedByUserId) REFERENCES gh.UserAccounts(UserId),
        CONSTRAINT CK_LabResultAmendments_Values CHECK(Revision>0 AND LEN(LTRIM(RTRIM(ResultValue)))>0 AND LEN(LTRIM(RTRIM(Reason)))>0),
        CONSTRAINT CK_LabResultAmendments_Flags CHECK(ResultFlag IN('Normal','Low','High','Critical','Abnormal') AND PreviousFlag IN('Normal','Low','High','Critical','Abnormal'))
    );
END;
COMMIT;
GO
-- Original finalization is immutable. Current clinical readers explicitly use this projection.
CREATE OR ALTER VIEW gh.vwLabResults AS
 SELECT i.LabOrderItemId,i.LabOrderId,i.LabTestId,i.ResultStatus,i.ResultedAt,i.ResultedByUserId,
 CASE WHEN a.LabResultAmendmentId IS NULL THEN i.ResultValue ELSE a.ResultValue END AS ResultValue,
 CASE WHEN a.LabResultAmendmentId IS NULL THEN i.ResultUnit ELSE a.ResultUnit END AS ResultUnit,
 CASE WHEN a.LabResultAmendmentId IS NULL THEN i.ReferenceRange ELSE a.ReferenceRange END AS ReferenceRange,
 CASE WHEN a.LabResultAmendmentId IS NULL THEN i.ResultFlag ELSE a.ResultFlag END AS ResultFlag,
 ISNULL(a.Revision,0) AS Revision,a.AmendedAt,a.AmendedByUserId
 FROM gh.LabOrderItems i OUTER APPLY(SELECT TOP 1 * FROM gh.LabResultAmendments x WHERE x.LabOrderItemId=i.LabOrderItemId ORDER BY x.Revision DESC)a;
GO
-- Read models only. Never backfill invented actors, statuses, or completion timestamps.
CREATE OR ALTER VIEW gh.vwPatientEvents AS
 SELECT p.patientId AS PatientId,CAST(p.registeredAt AS datetime2) AS EventAt,CAST('Registration' AS nvarchar(40)) AS EventType,CAST('Patient registered' AS nvarchar(1000)) AS Detail,CAST(NULL AS int) AS ActorUserId FROM dbo.patientTbl p
 UNION ALL SELECT PatientId,CreatedAt,'Appointment created','Appointment record created',BookedByUserId FROM gh.Appointments
 UNION ALL SELECT PatientId,StartedAt,'Consultation','Consultation record created',NULL FROM gh.Encounters
 UNION ALL SELECT e.PatientId,d.DiagnosedAt,'Diagnosis',CAST(d.DiagnosisName AS nvarchar(1000)),NULL FROM gh.Diagnoses d JOIN gh.Encounters e ON e.EncounterId=d.EncounterId
 UNION ALL SELECT COALESCE(v.PatientId,e.PatientId),v.RecordedAt,'Observations','Vital signs recorded',v.RecordedByUserId FROM gh.VitalSigns v LEFT JOIN gh.Encounters e ON e.EncounterId=v.EncounterId
 UNION ALL SELECT PatientId,OrderedAt,'Laboratory order','Laboratory order placed',NULL FROM gh.LabOrders
 UNION ALL SELECT o.PatientId,i.ResultedAt,'Laboratory result',CAST(c.TestName+': '+i.ResultFlag AS nvarchar(1000)),i.ResultedByUserId FROM gh.LabOrderItems i JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId WHERE i.ResultStatus='Final'
 UNION ALL SELECT PatientId,PrescribedAt,'Prescription','Prescription placed',NULL FROM gh.PrescriptionOrders
 UNION ALL SELECT o.PatientId,d.DispensedAt,'Dispensing',CAST(m.MedicationName+' dispensed: '+CONVERT(nvarchar(30),d.Quantity) AS nvarchar(1000)),d.DispensedByUserId FROM gh.Dispensations d JOIN gh.PrescriptionItems i ON i.PrescriptionItemId=d.PrescriptionItemId JOIN gh.PrescriptionOrders o ON o.PrescriptionOrderId=i.PrescriptionOrderId JOIN gh.MedicationCatalog m ON m.MedicationId=i.MedicationId
 UNION ALL SELECT PatientId,AssessedAt,'Triage',CAST('Assessment recorded; acuity '+CONVERT(nvarchar(4),AcuityLevel) AS nvarchar(1000)),AssessedByUserId FROM gh.TriageAssessments
 UNION ALL SELECT PatientId,RecordedAt,'Nursing assessment',CAST('Care status recorded: '+CareStatus AS nvarchar(1000)),RecordedByUserId FROM gh.NursingNotes
 UNION ALL SELECT e.PatientId,a.RecordedAt,'Clinical addendum',CAST(a.Note AS nvarchar(1000)),a.AuthorUserId FROM gh.EncounterAddenda a JOIN gh.Encounters e ON e.EncounterId=a.EncounterId
 UNION ALL SELECT PatientId,AdmittedAt,'Admission',CAST(AdmissionReason AS nvarchar(1000)),NULL FROM gh.Admissions
 UNION ALL SELECT PatientId,DischargedAt,'Discharge',CAST(DischargeSummary AS nvarchar(1000)),NULL FROM gh.Admissions WHERE DischargedAt IS NOT NULL
 UNION ALL SELECT PatientId,CreatedAt,'Invoice',CAST(InvoiceNumber+' created' AS nvarchar(1000)),NULL FROM gh.Invoices
 UNION ALL SELECT i.PatientId,p.ReceivedAt,'Payment',CAST(p.Method+' payment '+CONVERT(nvarchar(30),p.Amount)+' recorded against '+i.InvoiceNumber AS nvarchar(1000)),p.ReceivedByUserId FROM gh.Payments p JOIN gh.Invoices i ON i.InvoiceId=p.InvoiceId
 UNION ALL SELECT i.PatientId,c.SubmittedAt,'Insurance claim',CAST(c.ClaimNumber+' submitted' AS nvarchar(1000)),NULL FROM gh.InsuranceClaims c JOIN gh.Invoices i ON i.InvoiceId=c.InvoiceId
 UNION ALL SELECT PatientId,UploadedAt,'Document',CAST(FileName AS nvarchar(1000)),UploadedByUserId FROM gh.Documents
 UNION ALL SELECT n.PatientId,n.CreatedAt,'Patient notification',CAST(n.Subject AS nvarchar(1000)),NULL FROM gh.Notifications n JOIN gh.UserAccounts u ON u.UserId=n.UserId WHERE u.RoleName='Patient' AND n.PatientId IS NOT NULL
 UNION ALL SELECT a.PatientId,h.OccurredAt,'Appointment update',CAST(h.NewStatus+COALESCE(': '+h.Detail,'') AS nvarchar(1000)),h.ActorUserId FROM gh.WorkflowHistory h JOIN gh.Appointments a ON a.AppointmentId=h.EntityId WHERE h.EntityType='Appointment' AND (h.NewStatus<>'Booked' OR h.PreviousStatus IS NOT NULL)
 UNION ALL SELECT e.PatientId,h.OccurredAt,'Clinical note',CAST(h.NewStatus+COALESCE(': '+h.Detail,'') AS nvarchar(1000)),h.ActorUserId FROM gh.WorkflowHistory h JOIN gh.Encounters e ON e.EncounterId=h.EntityId WHERE h.EntityType='Encounter' AND h.NewStatus IN('Signed','DraftUpdated')
 UNION ALL SELECT o.PatientId,h.OccurredAt,'Laboratory update',CAST(h.NewStatus+COALESCE(': '+h.Detail,'') AS nvarchar(1000)),h.ActorUserId FROM gh.WorkflowHistory h JOIN gh.LabOrders o ON o.LabOrderId=h.EntityId WHERE h.EntityType='LabOrder' AND h.NewStatus<>'Ordered'
 UNION ALL SELECT o.PatientId,h.OccurredAt,'Prescription update',CAST(h.NewStatus+COALESCE(': '+h.Detail,'') AS nvarchar(1000)),h.ActorUserId FROM gh.WorkflowHistory h JOIN gh.PrescriptionOrders o ON o.PrescriptionOrderId=h.EntityId WHERE h.EntityType='PrescriptionOrder' AND h.NewStatus<>'Ordered'
 UNION ALL SELECT a.PatientId,h.OccurredAt,'Bed transfer',CAST(h.Detail AS nvarchar(1000)),h.ActorUserId FROM gh.WorkflowHistory h JOIN gh.Admissions a ON a.AdmissionId=h.EntityId WHERE h.EntityType='Admission' AND h.NewStatus='Transferred'
 UNION ALL SELECT t.PatientId,h.OccurredAt,'Triage update',CAST(h.NewStatus AS nvarchar(1000)),h.ActorUserId FROM gh.WorkflowHistory h JOIN gh.TriageAssessments t ON t.TriageAssessmentId=h.EntityId WHERE h.EntityType='Triage' AND h.NewStatus<>'Waiting'
 UNION ALL SELECT i.PatientId,h.OccurredAt,'Insurance update',CAST(h.NewStatus+COALESCE(': '+h.Detail,'') AS nvarchar(1000)),h.ActorUserId FROM gh.WorkflowHistory h JOIN gh.InsuranceClaims c ON c.InsuranceClaimId=h.EntityId JOIN gh.Invoices i ON i.InvoiceId=c.InvoiceId WHERE h.EntityType='Claim' AND h.NewStatus<>'Submitted'
 UNION ALL SELECT o.PatientId,a.AmendedAt,'Laboratory amendment',CAST(c.TestName+': '+a.Reason AS nvarchar(1000)),a.AmendedByUserId FROM gh.LabResultAmendments a JOIN gh.LabOrderItems i ON i.LabOrderItemId=a.LabOrderItemId JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId;
GO
CREATE OR ALTER VIEW gh.vwPatientTimeline AS
 SELECT PatientId,EventAt,EventType,Detail FROM gh.vwPatientEvents;
GO

