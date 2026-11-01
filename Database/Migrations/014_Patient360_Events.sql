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
 UNION ALL SELECT i.PatientId,h.OccurredAt,'Insurance update',CAST(h.NewStatus+COALESCE(': '+h.Detail,'') AS nvarchar(1000)),h.ActorUserId FROM gh.WorkflowHistory h JOIN gh.InsuranceClaims c ON c.InsuranceClaimId=h.EntityId JOIN gh.Invoices i ON i.InvoiceId=c.InvoiceId WHERE h.EntityType='Claim' AND h.NewStatus<>'Submitted';
GO
CREATE OR ALTER VIEW gh.vwPatientTimeline AS
 SELECT PatientId,EventAt,EventType,Detail FROM gh.vwPatientEvents;
GO
