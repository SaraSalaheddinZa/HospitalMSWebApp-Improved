SET NOCOUNT ON;
IF OBJECT_ID('gh.vwBookableDoctorSlots','V') IS NULL OR OBJECT_ID('gh.DoctorPublicProfiles','U') IS NULL OR OBJECT_ID('gh.SyntheticDemoRuns','U') IS NULL
 THROW 51243,'Medical team or synthetic provenance migration is missing.',1;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1)
 THROW 51100,'Disabled or untrusted foreign keys need review.',1;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1)
 THROW 51101,'Disabled or untrusted check constraints need review.',1;
IF EXISTS(SELECT MedicalRecordNumber FROM dbo.patientTbl GROUP BY MedicalRecordNumber HAVING COUNT(*)>1 OR MedicalRecordNumber IS NULL)
 THROW 51102,'Patient medical record numbers are missing or duplicated.',1;
IF (SELECT COUNT(*) FROM gh.UserAccounts WHERE Email LIKE 'dev.%@guardianhealth.test' AND IsActive=1)<>7
 THROW 51103,'Development accounts are missing or inactive.',1;
IF EXISTS(SELECT 1 FROM gh.MedicationBatches b OUTER APPLY(SELECT SUM(Quantity) AS Balance FROM gh.StockMovements m WHERE m.MedicationBatchId=b.MedicationBatchId)l WHERE b.QuantityOnHand<0 OR b.QuantityOnHand<>ISNULL(l.Balance,0))
 THROW 51104,'Stock quantity does not match the movement ledger.',1;
IF EXISTS(SELECT 1 FROM gh.PrescriptionItems i CROSS APPLY(SELECT SUM(Quantity) AS Dispensed FROM gh.Dispensations d WHERE d.PrescriptionItemId=i.PrescriptionItemId)x WHERE x.Dispensed>i.Quantity)
 THROW 51105,'A prescription has been over-dispensed.',1;
IF EXISTS(SELECT 1 FROM gh.Invoices i OUTER APPLY(SELECT SUM(Amount) AS Paid FROM gh.Payments p WHERE p.InvoiceId=i.InvoiceId)x WHERE i.PaidAmount<>ISNULL(x.Paid,0) OR i.PaidAmount>i.TotalAmount)
 THROW 51106,'Invoice balances do not match payment records.',1;
IF EXISTS(SELECT 1 FROM gh.Admissions a JOIN gh.Beds b ON b.BedId=a.BedId WHERE a.DischargedAt IS NULL AND b.Status<>'Occupied')
 THROW 51107,'An active admission has a non-occupied bed.',1;
IF EXISTS(SELECT BedId FROM gh.Admissions WHERE DischargedAt IS NULL AND BedId IS NOT NULL GROUP BY BedId HAVING COUNT(*)>1)
 THROW 51108,'More than one active admission is assigned to a bed.',1;
IF EXISTS(SELECT 1 FROM gh.Documents d LEFT JOIN gh.DocumentContents c ON c.DocumentId=d.DocumentId WHERE d.IsActive=1 AND c.DocumentId IS NULL)
 THROW 51109,'An active document is missing its stored content.',1;
IF EXISTS(SELECT 1 FROM gh.LabOrders o WHERE Status='Completed' AND EXISTS(SELECT 1 FROM gh.LabOrderItems i WHERE i.LabOrderId=o.LabOrderId AND i.ResultStatus<>'Final'))
 THROW 51110,'A completed laboratory order has unfinished results.',1;
IF EXISTS(SELECT 1 FROM gh.MedicationCatalog WHERE MedicationCode LIKE 'QA-%')
 OR EXISTS(SELECT 1 FROM gh.LabCatalog WHERE TestCode LIKE 'QA-%')
 THROW 51111,'A rolled-back integration fixture was retained.',1;
IF COL_LENGTH('gh.InsuranceClaims','DecisionNote') IS NULL OR COL_LENGTH('gh.Payments','InsuranceClaimId') IS NULL OR OBJECT_ID('gh.EncounterDraftHistory','U') IS NULL
 THROW 51112,'Workflow accountability migration is missing.',1;
IF EXISTS(SELECT 1 FROM gh.Payments p JOIN gh.InsuranceClaims c ON c.InsuranceClaimId=p.InsuranceClaimId WHERE p.InvoiceId<>c.InvoiceId OR p.Amount<>c.ApprovedAmount OR p.Method<>'Insurance' OR c.Status<>'Paid')
 THROW 51113,'A linked insurance settlement disagrees with its claim.',1;
IF EXISTS(SELECT 1 FROM gh.Invoices i CROSS APPLY(SELECT SUM(CASE WHEN c.Status='Approved' THEN c.ApprovedAmount ELSE c.ClaimedAmount END) AS Reserved FROM gh.InsuranceClaims c WHERE c.InvoiceId=i.InvoiceId AND c.Status IN('Submitted','UnderReview','Approved'))r WHERE r.Reserved>i.TotalAmount-i.PaidAmount)
 THROW 51114,'Pending insurance allocation exceeds the remaining invoice balance.',1;
IF EXISTS(SELECT 1 FROM dbo.patientTbl WHERE patientName LIKE 'QA Patient QA-%') OR EXISTS(SELECT 1 FROM gh.Suppliers WHERE SupplierName LIKE 'QA-%') OR EXISTS(SELECT 1 FROM gh.InsuranceProviders WHERE ProviderCode LIKE 'QA-%') OR EXISTS(SELECT 1 FROM gh.Wards WHERE WardName LIKE 'QA-%')
 THROW 51115,'A registration, supplier, provider or admission fixture was retained.',1;
IF EXISTS(SELECT PatientId FROM gh.Admissions WHERE DischargedAt IS NULL GROUP BY PatientId HAVING COUNT(*)>1)
 THROW 51116,'A patient has more than one active admission.',1;
IF EXISTS(SELECT 1 FROM gh.Encounters WHERE Status IN('Signed','Amended') AND (ClosedAt IS NULL OR LEN(LTRIM(RTRIM(ISNULL(Assessment,''))))=0 OR LEN(LTRIM(RTRIM(ISNULL(TreatmentPlan,''))))=0))
 THROW 51117,'A signed consultation is missing required clinical information.',1;
IF OBJECT_ID('gh.vwPatientEvents','V') IS NULL
 THROW 51118,'Patient 360 event migration is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('gh.Encounters') AND name='UX_Encounters_Appointment' AND is_unique=1 AND is_disabled=0)
 THROW 51119,'Appointment consultation uniqueness protection is missing.',1;
IF EXISTS(SELECT AppointmentId FROM gh.Encounters WHERE AppointmentId IS NOT NULL GROUP BY AppointmentId HAVING COUNT(*)>1)
 THROW 51120,'An appointment has duplicate consultations.',1;
IF COL_LENGTH('gh.Admissions','TriageAssessmentId') IS NULL
 THROW 51121,'Emergency admission handoff migration is missing.',1;
IF EXISTS(SELECT 1 FROM gh.Admissions a JOIN gh.TriageAssessments t ON t.TriageAssessmentId=a.TriageAssessmentId WHERE a.PatientId<>t.PatientId)
 THROW 51122,'An admission is linked to another patient emergency assessment.',1;
IF OBJECT_ID('gh.StockReceipts','U') IS NULL
 THROW 51123,'Supplier receipt migration is missing.',1;
IF EXISTS(SELECT 1 FROM gh.StockReceipts r JOIN gh.StockMovements m ON m.StockMovementId=r.StockMovementId WHERE r.MedicationBatchId<>m.MedicationBatchId OR r.Quantity<>m.Quantity OR m.MovementType<>'Received' OR m.RecordedByUserId<>r.ReceivedByUserId OR ISNULL(m.ReferenceId,'')<>r.ReceiptReference OR ISNULL(m.ReferenceType,'')<>'SupplierDelivery')
 THROW 51124,'A supplier receipt disagrees with its stock movement.',1;
IF OBJECT_ID('gh.LabResultAmendments','U') IS NULL OR OBJECT_ID('gh.vwLabResults','V') IS NULL OR COL_LENGTH('gh.LabOrders','ResultVersion') IS NULL
 THROW 51125,'Laboratory result amendment migration is missing.',1;
IF EXISTS(SELECT LabOrderItemId FROM gh.LabResultAmendments GROUP BY LabOrderItemId HAVING MIN(Revision)<>1 OR MAX(Revision)<>COUNT(*))
 THROW 51126,'Laboratory amendment revisions are not contiguous.',1;
IF EXISTS(SELECT 1 FROM gh.LabResultAmendments a JOIN gh.LabOrderItems i ON i.LabOrderItemId=a.LabOrderItemId LEFT JOIN gh.LabResultAmendments p ON p.LabOrderItemId=a.LabOrderItemId AND p.Revision=a.Revision-1
 WHERE i.ResultStatus<>'Final' OR a.PreviousValue<>CASE WHEN a.Revision=1 THEN i.ResultValue ELSE p.ResultValue END
 OR ISNULL(a.PreviousUnit,'')<>ISNULL(CASE WHEN a.Revision=1 THEN i.ResultUnit ELSE p.ResultUnit END,'')
 OR ISNULL(a.PreviousReferenceRange,'')<>ISNULL(CASE WHEN a.Revision=1 THEN i.ReferenceRange ELSE p.ReferenceRange END,'')
 OR a.PreviousFlag<>CASE WHEN a.Revision=1 THEN i.ResultFlag ELSE p.ResultFlag END)
 THROW 51127,'Laboratory correction history disagrees with the preceding result.',1;
IF COL_LENGTH('gh.PasswordResetTokens','IssuedAuthVersion') IS NULL
 THROW 51128,'Version-bound password recovery migration is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('gh.PasswordResetTokens') AND name='IX_PasswordResetTokens_Hash' AND is_disabled=0)
 OR NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('gh.PasswordResetTokens') AND name='CK_PasswordResetTokens_AuthVersion' AND is_disabled=0 AND is_not_trusted=0)
 THROW 51129,'Recovery version constraint or lookup index is missing.',1;
IF EXISTS(SELECT 1 FROM gh.UserAccounts u LEFT JOIN gh.StaffMembers s ON s.StaffMemberId=u.StaffMemberId
 WHERE u.RoleName IN('Nurse','Pharmacist') AND (s.StaffMemberId IS NULL OR s.RoleName<>u.RoleName OR s.IsActive<>u.IsActive OR s.FullName<>u.DisplayName OR s.Email<>u.Email))
 OR EXISTS(SELECT StaffMemberId FROM gh.UserAccounts WHERE StaffMemberId IS NOT NULL GROUP BY StaffMemberId HAVING COUNT(*)>1)
 THROW 51130,'Nurse/pharmacist profile identity or linked account state is inconsistent.',1;
IF EXISTS(SELECT 1 FROM gh.UserAccounts WHERE Email LIKE 'QA-RECOVERY-%@example.test' OR Email LIKE 'QA-PROFILE-%@example.test')
 OR EXISTS(SELECT 1 FROM gh.StaffMembers WHERE StaffNumber LIKE 'QA-RECOVERY-%' OR StaffNumber LIKE 'QA-PROFILE-%')
 THROW 51131,'A staff profile or recovery fixture was retained.',1;
SELECT 'PASS: schema, account, recovery, stock, supplier receipt, payment, admission, document and laboratory integrity' AS Result;
SELECT s.name AS [Schema],t.name AS [Table],SUM(p.rows) AS [Rows]
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN(0,1)
GROUP BY s.name,t.name ORDER BY s.name,t.name;
