-- Additive view expansion only. No historical rows are changed.
CREATE OR ALTER VIEW gh.vwPatientTimeline AS
SELECT p.patientId AS PatientId,p.registeredAt AS EventAt,CAST('Registration' AS nvarchar(40)) AS EventType,CAST(p.patientName AS nvarchar(1000)) AS Detail FROM dbo.patientTbl p
UNION ALL SELECT PatientId,StartedAt,'Consultation',CAST('Consultation: '+Status AS nvarchar(1000)) FROM gh.Encounters
UNION ALL SELECT PatientId,ScheduledStart,'Appointment',CAST('Appointment: '+Status AS nvarchar(1000)) FROM gh.Appointments
UNION ALL SELECT PatientId,OrderedAt,'Laboratory order',CAST('Laboratory order: '+Status AS nvarchar(1000)) FROM gh.LabOrders
UNION ALL SELECT o.PatientId,i.ResultedAt,'Laboratory result',CAST(c.TestName+': '+i.ResultFlag AS nvarchar(1000)) FROM gh.LabOrderItems i JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId WHERE i.ResultStatus='Final'
UNION ALL SELECT PatientId,ReviewedAt,'Laboratory review','Completed laboratory order reviewed by clinician' FROM gh.LabOrders WHERE ReviewedAt IS NOT NULL
UNION ALL SELECT PatientId,PrescribedAt,'Prescription',CAST('Prescription: '+Status AS nvarchar(1000)) FROM gh.PrescriptionOrders
UNION ALL SELECT o.PatientId,d.DispensedAt,'Dispensing',CAST(m.MedicationName+' dispensed' AS nvarchar(1000)) FROM gh.Dispensations d JOIN gh.PrescriptionItems i ON i.PrescriptionItemId=d.PrescriptionItemId JOIN gh.PrescriptionOrders o ON o.PrescriptionOrderId=i.PrescriptionOrderId JOIN gh.MedicationCatalog m ON m.MedicationId=i.MedicationId
UNION ALL SELECT PatientId,AdmittedAt,'Admission',CAST(AdmissionReason AS nvarchar(1000)) FROM gh.Admissions
UNION ALL SELECT PatientId,DischargedAt,'Discharge',CAST(DischargeSummary AS nvarchar(1000)) FROM gh.Admissions WHERE DischargedAt IS NOT NULL
UNION ALL SELECT a.PatientId,h.OccurredAt,'Bed transfer','Bed assignment updated' FROM gh.WorkflowHistory h JOIN gh.Admissions a ON a.AdmissionId=h.EntityId WHERE h.EntityType='Admission' AND h.NewStatus='Transferred'
UNION ALL SELECT PatientId,CreatedAt,'Invoice',CAST(InvoiceNumber+': '+Status AS nvarchar(1000)) FROM gh.Invoices WHERE Status NOT IN('Draft','Voided')
UNION ALL SELECT i.PatientId,p.ReceivedAt,'Payment',CAST(p.Method+' payment recorded against '+i.InvoiceNumber AS nvarchar(1000)) FROM gh.Payments p JOIN gh.Invoices i ON i.InvoiceId=p.InvoiceId
UNION ALL SELECT i.PatientId,c.SubmittedAt,'Insurance claim',CAST('Claim recorded: '+c.Status AS nvarchar(1000)) FROM gh.InsuranceClaims c JOIN gh.Invoices i ON i.InvoiceId=c.InvoiceId
UNION ALL SELECT PatientId,UploadedAt,'Document',CAST(FileName AS nvarchar(1000)) FROM gh.Documents WHERE IsActive=1;
GO
