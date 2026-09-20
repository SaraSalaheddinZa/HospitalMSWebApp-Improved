/* Guardian Health Hospital Information System - core migration
   Apply to the existing HospitalMS database once, using an account permitted to create tables.
   The script extends legacy patient/doctor tables instead of dropping or renaming them. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'gh') EXEC(N'CREATE SCHEMA gh');

IF OBJECT_ID(N'dbo.patientTbl', N'U') IS NULL
    THROW 51000, 'The legacy patientTbl table is required before applying this migration.', 1;
IF OBJECT_ID(N'dbo.doctorTbl', N'U') IS NULL
    THROW 51000, 'The legacy doctorTbl table is required before applying this migration.', 1;

IF COL_LENGTH('dbo.patientTbl', 'MedicalRecordNumber') IS NULL
    ALTER TABLE dbo.patientTbl ADD MedicalRecordNumber nvarchar(30) NULL;
IF COL_LENGTH('dbo.patientTbl', 'IsActive') IS NULL
    ALTER TABLE dbo.patientTbl ADD IsActive bit NOT NULL CONSTRAINT DF_patientTbl_IsActive DEFAULT 1;
IF COL_LENGTH('dbo.patientTbl', 'registeredAt') IS NULL
    ALTER TABLE dbo.patientTbl ADD registeredAt datetime2(0) NOT NULL CONSTRAINT DF_patientTbl_registeredAt DEFAULT SYSUTCDATETIME();

;WITH sourceRows AS
(
    SELECT patientId, ROW_NUMBER() OVER (PARTITION BY MedicalRecordNumber ORDER BY patientId) AS duplicateNumber
    FROM dbo.patientTbl WHERE MedicalRecordNumber IS NOT NULL
)
UPDATE sourceRows SET MedicalRecordNumber = NULL WHERE duplicateNumber > 1;
UPDATE dbo.patientTbl SET MedicalRecordNumber = CONCAT('GH-', RIGHT(REPLICATE('0', 8) + CAST(patientId AS varchar(8)), 8))
WHERE MedicalRecordNumber IS NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_patientTbl_MRN' AND object_id = OBJECT_ID(N'dbo.patientTbl'))
    CREATE UNIQUE INDEX UX_patientTbl_MRN ON dbo.patientTbl(MedicalRecordNumber);

CREATE TABLE gh.Departments
(
    DepartmentId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Departments PRIMARY KEY,
    DepartmentCode nvarchar(20) NOT NULL,
    DepartmentName nvarchar(120) NOT NULL,
    Phone nvarchar(30) NULL,
    Location nvarchar(160) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT 1,
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Departments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Departments_Code UNIQUE (DepartmentCode),
    CONSTRAINT UQ_Departments_Name UNIQUE (DepartmentName)
);

CREATE TABLE gh.StaffMembers
(
    StaffMemberId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StaffMembers PRIMARY KEY,
    StaffNumber nvarchar(30) NOT NULL,
    DepartmentId int NULL,
    RoleName nvarchar(30) NOT NULL,
    FullName nvarchar(160) NOT NULL,
    Email nvarchar(254) NOT NULL,
    Phone nvarchar(30) NULL,
    LicenseNumber nvarchar(80) NULL,
    HireDate date NULL,
    IsActive bit NOT NULL CONSTRAINT DF_StaffMembers_IsActive DEFAULT 1,
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_StaffMembers_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2(0) NULL,
    CONSTRAINT UQ_StaffMembers_Number UNIQUE (StaffNumber),
    CONSTRAINT UQ_StaffMembers_Email UNIQUE (Email),
    CONSTRAINT CK_StaffMembers_Role CHECK (RoleName IN ('Nurse','Pharmacist','Admin','Doctor','Receptionist','Laboratorian')),
    CONSTRAINT FK_StaffMembers_Department FOREIGN KEY (DepartmentId) REFERENCES gh.Departments(DepartmentId)
);

CREATE TABLE gh.PatientProfiles
(
    PatientId int NOT NULL CONSTRAINT PK_PatientProfiles PRIMARY KEY,
    NationalId nvarchar(50) NULL,
    Email nvarchar(254) NULL,
    EmergencyContactName nvarchar(160) NULL,
    EmergencyContactPhone nvarchar(30) NULL,
    EmergencyContactRelationship nvarchar(60) NULL,
    BloodType nvarchar(3) NULL,
    ChronicConditions nvarchar(max) NULL,
    InsuranceMemberNumber nvarchar(80) NULL,
    InsuranceProviderId int NULL,
    PatientStatus nvarchar(20) NOT NULL CONSTRAINT DF_PatientProfiles_Status DEFAULT 'Active',
    PreferredLanguage nvarchar(50) NULL,
    ConsentToContact bit NOT NULL CONSTRAINT DF_PatientProfiles_Consent DEFAULT 1,
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_PatientProfiles_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2(0) NULL,
    CONSTRAINT CK_PatientProfiles_BloodType CHECK (BloodType IS NULL OR BloodType IN ('A+','A-','B+','B-','AB+','AB-','O+','O-')),
    CONSTRAINT CK_PatientProfiles_Status CHECK (PatientStatus IN ('Active','Inactive','Deceased','Archived')),
    CONSTRAINT FK_PatientProfiles_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId)
);

CREATE TABLE gh.UserAccounts
(
    UserId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserAccounts PRIMARY KEY,
    StaffMemberId int NULL,
    PatientId int NULL,
    RoleName nvarchar(30) NOT NULL,
    DisplayName nvarchar(160) NOT NULL,
    Email nvarchar(254) NOT NULL,
    NormalizedEmail nvarchar(254) NOT NULL,
    PasswordHash nvarchar(256) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_UserAccounts_IsActive DEFAULT 1,
    FailedLoginCount smallint NOT NULL CONSTRAINT DF_UserAccounts_FailedLogins DEFAULT 0,
    LockedUntil datetime2(0) NULL,
    LastLoginAt datetime2(0) NULL,
    PasswordChangedAt datetime2(0) NOT NULL CONSTRAINT DF_UserAccounts_PasswordChanged DEFAULT SYSUTCDATETIME(),
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_UserAccounts_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_UserAccounts_NormalizedEmail UNIQUE (NormalizedEmail),
    CONSTRAINT CK_UserAccounts_Role CHECK (RoleName IN ('Admin','Doctor','Receptionist','Laboratorian','Nurse','Pharmacist','Patient')),
    CONSTRAINT CK_UserAccounts_Owner CHECK ((PatientId IS NOT NULL AND StaffMemberId IS NULL) OR (PatientId IS NULL)),
    CONSTRAINT FK_UserAccounts_Staff FOREIGN KEY (StaffMemberId) REFERENCES gh.StaffMembers(StaffMemberId),
    CONSTRAINT FK_UserAccounts_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId)
);
CREATE INDEX IX_UserAccounts_Login ON gh.UserAccounts(NormalizedEmail, RoleName, IsActive) INCLUDE (PasswordHash, LockedUntil);

CREATE TABLE gh.RolePermissions
(
    RoleName nvarchar(30) NOT NULL,
    PermissionName nvarchar(80) NOT NULL,
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleName, PermissionName),
    CONSTRAINT CK_RolePermissions_Role CHECK (RoleName IN ('Admin','Doctor','Receptionist','Laboratorian','Nurse','Pharmacist','Patient'))
);
INSERT INTO gh.RolePermissions(RoleName, PermissionName)
SELECT v.RoleName, v.PermissionName FROM (VALUES
    ('Admin','System.Manage'),('Admin','Audit.View'),('Doctor','Encounter.Write'),('Doctor','Lab.Review'),
    ('Receptionist','Patient.Register'),('Receptionist','Appointment.Manage'),('Laboratorian','Lab.Result.Write'),
    ('Nurse','Triage.Write'),('Pharmacist','Pharmacy.Manage'),('Patient','Portal.ViewOwn')) v(RoleName, PermissionName)
WHERE NOT EXISTS (SELECT 1 FROM gh.RolePermissions p WHERE p.RoleName=v.RoleName AND p.PermissionName=v.PermissionName);

CREATE TABLE gh.SystemSettings
(
    SettingKey nvarchar(80) NOT NULL CONSTRAINT PK_SystemSettings PRIMARY KEY,
    SettingValue nvarchar(max) NULL,
    UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_SystemSettings_Updated DEFAULT SYSUTCDATETIME(),
    UpdatedByUserId int NULL CONSTRAINT FK_SystemSettings_User FOREIGN KEY REFERENCES gh.UserAccounts(UserId)
);

CREATE TABLE gh.AuditEvents
(
    AuditEventId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditEvents PRIMARY KEY,
    ActorUserId int NULL,
    ActorRole nvarchar(30) NULL,
    ActionName nvarchar(80) NOT NULL,
    EntityType nvarchar(80) NOT NULL,
    EntityId nvarchar(80) NULL,
    Detail nvarchar(1000) NULL,
    IpAddress nvarchar(64) NULL,
    OccurredAt datetime2(0) NOT NULL CONSTRAINT DF_AuditEvents_Occurred DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuditEvents_User FOREIGN KEY (ActorUserId) REFERENCES gh.UserAccounts(UserId)
);
CREATE INDEX IX_AuditEvents_Entity ON gh.AuditEvents(EntityType, EntityId, OccurredAt DESC);
CREATE INDEX IX_AuditEvents_Actor ON gh.AuditEvents(ActorUserId, OccurredAt DESC);

CREATE TABLE gh.PasswordResetTokens
(
    PasswordResetTokenId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PasswordResetTokens PRIMARY KEY,
    UserId int NOT NULL,
    TokenHash varbinary(32) NOT NULL,
    ExpiresAt datetime2(0) NOT NULL,
    UsedAt datetime2(0) NULL,
    RequestedIp nvarchar(64) NULL,
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_PasswordResetTokens_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PasswordResetTokens_User FOREIGN KEY (UserId) REFERENCES gh.UserAccounts(UserId)
);
CREATE INDEX IX_PasswordResetTokens_Active ON gh.PasswordResetTokens(UserId, ExpiresAt) WHERE UsedAt IS NULL;

CREATE TABLE gh.DoctorSchedules
(
    DoctorScheduleId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DoctorSchedules PRIMARY KEY,
    DoctorId int NOT NULL,
    DepartmentId int NULL,
    ScheduleDate date NOT NULL,
    StartTime time(0) NOT NULL,
    EndTime time(0) NOT NULL,
    SlotMinutes smallint NOT NULL CONSTRAINT DF_DoctorSchedules_Slot DEFAULT 20,
    IsAvailable bit NOT NULL CONSTRAINT DF_DoctorSchedules_Available DEFAULT 1,
    CONSTRAINT CK_DoctorSchedules_Time CHECK (EndTime > StartTime AND SlotMinutes BETWEEN 5 AND 120),
    CONSTRAINT FK_DoctorSchedules_Doctor FOREIGN KEY (DoctorId) REFERENCES dbo.doctorTbl(doctorID),
    CONSTRAINT FK_DoctorSchedules_Department FOREIGN KEY (DepartmentId) REFERENCES gh.Departments(DepartmentId)
);
CREATE UNIQUE INDEX UX_DoctorSchedules_Time ON gh.DoctorSchedules(DoctorId, ScheduleDate, StartTime);

CREATE TABLE gh.Appointments
(
    AppointmentId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Appointments PRIMARY KEY,
    PatientId int NOT NULL,
    DoctorId int NOT NULL,
    DepartmentId int NULL,
    ScheduledStart datetime2(0) NOT NULL,
    ScheduledEnd datetime2(0) NOT NULL,
    Reason nvarchar(500) NULL,
    Status nvarchar(20) NOT NULL CONSTRAINT DF_Appointments_Status DEFAULT 'Booked',
    BookedByUserId int NULL,
    CheckInAt datetime2(0) NULL,
    CancellationReason nvarchar(500) NULL,
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Appointments_Created DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2(0) NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT CK_Appointments_Time CHECK (ScheduledEnd > ScheduledStart),
    CONSTRAINT CK_Appointments_Status CHECK (Status IN ('Requested','Booked','Confirmed','CheckedIn','InProgress','Completed','Cancelled','NoShow','RescheduleRequested')),
    CONSTRAINT FK_Appointments_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId),
    CONSTRAINT FK_Appointments_Doctor FOREIGN KEY (DoctorId) REFERENCES dbo.doctorTbl(doctorID),
    CONSTRAINT FK_Appointments_Department FOREIGN KEY (DepartmentId) REFERENCES gh.Departments(DepartmentId),
    CONSTRAINT FK_Appointments_BookedBy FOREIGN KEY (BookedByUserId) REFERENCES gh.UserAccounts(UserId)
);
CREATE UNIQUE INDEX UX_Appointments_DoctorSlot ON gh.Appointments(DoctorId, ScheduledStart) WHERE Status NOT IN ('Cancelled','NoShow');
CREATE INDEX IX_Appointments_Patient ON gh.Appointments(PatientId, ScheduledStart DESC);
CREATE INDEX IX_Appointments_Day ON gh.Appointments(DoctorId, ScheduledStart, Status);

CREATE TABLE gh.Encounters
(
    EncounterId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Encounters PRIMARY KEY,
    AppointmentId bigint NULL,
    PatientId int NOT NULL,
    DoctorId int NOT NULL,
    EncounterType nvarchar(30) NOT NULL CONSTRAINT DF_Encounters_Type DEFAULT 'Outpatient',
    Status nvarchar(20) NOT NULL CONSTRAINT DF_Encounters_Status DEFAULT 'Open',
    StartedAt datetime2(0) NOT NULL CONSTRAINT DF_Encounters_Started DEFAULT SYSUTCDATETIME(),
    ClosedAt datetime2(0) NULL,
    ChiefComplaint nvarchar(1000) NULL,
    Assessment nvarchar(max) NULL,
    TreatmentPlan nvarchar(max) NULL,
    FollowUpDate date NULL,
    CONSTRAINT CK_Encounters_Status CHECK (Status IN ('Open','Signed','Amended')),
    CONSTRAINT FK_Encounters_Appointment FOREIGN KEY (AppointmentId) REFERENCES gh.Appointments(AppointmentId),
    CONSTRAINT FK_Encounters_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId),
    CONSTRAINT FK_Encounters_Doctor FOREIGN KEY (DoctorId) REFERENCES dbo.doctorTbl(doctorID)
);
CREATE INDEX IX_Encounters_Patient ON gh.Encounters(PatientId, StartedAt DESC);

CREATE TABLE gh.VitalSigns
(
    VitalSignId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_VitalSigns PRIMARY KEY,
    EncounterId bigint NOT NULL,
    RecordedByUserId int NULL,
    RecordedAt datetime2(0) NOT NULL CONSTRAINT DF_VitalSigns_Recorded DEFAULT SYSUTCDATETIME(),
    TemperatureC decimal(4,1) NULL,
    Pulse smallint NULL,
    RespiratoryRate smallint NULL,
    SystolicBp smallint NULL,
    DiastolicBp smallint NULL,
    OxygenSaturation decimal(5,2) NULL,
    WeightKg decimal(5,2) NULL,
    HeightCm decimal(5,2) NULL,
    CONSTRAINT FK_VitalSigns_Encounter FOREIGN KEY (EncounterId) REFERENCES gh.Encounters(EncounterId),
    CONSTRAINT FK_VitalSigns_Recorder FOREIGN KEY (RecordedByUserId) REFERENCES gh.UserAccounts(UserId)
);

CREATE TABLE gh.Diagnoses
(
    DiagnosisId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Diagnoses PRIMARY KEY,
    EncounterId bigint NOT NULL,
    Icd10Code nvarchar(20) NULL,
    DiagnosisName nvarchar(300) NOT NULL,
    IsPrimary bit NOT NULL CONSTRAINT DF_Diagnoses_Primary DEFAULT 0,
    DiagnosedAt datetime2(0) NOT NULL CONSTRAINT DF_Diagnoses_At DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Diagnoses_Encounter FOREIGN KEY (EncounterId) REFERENCES gh.Encounters(EncounterId)
);

CREATE TABLE gh.MedicationCatalog
(
    MedicationId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicationCatalog PRIMARY KEY,
    MedicationCode nvarchar(40) NOT NULL,
    MedicationName nvarchar(200) NOT NULL,
    Form nvarchar(60) NULL,
    Strength nvarchar(60) NULL,
    UnitPrice decimal(12,2) NOT NULL CONSTRAINT DF_MedicationCatalog_Price DEFAULT 0,
    IsActive bit NOT NULL CONSTRAINT DF_MedicationCatalog_Active DEFAULT 1,
    CONSTRAINT UQ_MedicationCatalog_Code UNIQUE(MedicationCode),
    CONSTRAINT UQ_MedicationCatalog_Name UNIQUE(MedicationName)
);

CREATE TABLE gh.PrescriptionOrders
(
    PrescriptionOrderId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PrescriptionOrders PRIMARY KEY,
    EncounterId bigint NOT NULL,
    PatientId int NOT NULL,
    PrescribedByDoctorId int NOT NULL,
    Status nvarchar(20) NOT NULL CONSTRAINT DF_PrescriptionOrders_Status DEFAULT 'Pending',
    Notes nvarchar(1000) NULL,
    PrescribedAt datetime2(0) NOT NULL CONSTRAINT DF_PrescriptionOrders_At DEFAULT SYSUTCDATETIME(),
    DispensedAt datetime2(0) NULL,
    CONSTRAINT CK_PrescriptionOrders_Status CHECK (Status IN ('Pending','PartiallyDispensed','Dispensed','Cancelled')),
    CONSTRAINT FK_PrescriptionOrders_Encounter FOREIGN KEY (EncounterId) REFERENCES gh.Encounters(EncounterId),
    CONSTRAINT FK_PrescriptionOrders_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId),
    CONSTRAINT FK_PrescriptionOrders_Doctor FOREIGN KEY (PrescribedByDoctorId) REFERENCES dbo.doctorTbl(doctorID)
);
CREATE TABLE gh.PrescriptionItems
(
    PrescriptionItemId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PrescriptionItems PRIMARY KEY,
    PrescriptionOrderId bigint NOT NULL,
    MedicationId int NOT NULL,
    Dose nvarchar(100) NOT NULL,
    Frequency nvarchar(100) NOT NULL,
    DurationDays smallint NULL,
    Quantity decimal(12,2) NOT NULL,
    Instructions nvarchar(500) NULL,
    CONSTRAINT CK_PrescriptionItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT FK_PrescriptionItems_Order FOREIGN KEY (PrescriptionOrderId) REFERENCES gh.PrescriptionOrders(PrescriptionOrderId),
    CONSTRAINT FK_PrescriptionItems_Medication FOREIGN KEY (MedicationId) REFERENCES gh.MedicationCatalog(MedicationId)
);

CREATE TABLE gh.LabCatalog
(
    LabTestId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LabCatalog PRIMARY KEY,
    TestCode nvarchar(40) NOT NULL,
    TestName nvarchar(200) NOT NULL,
    SpecimenType nvarchar(100) NULL,
    ReferenceRange nvarchar(250) NULL,
    Unit nvarchar(40) NULL,
    Price decimal(12,2) NOT NULL CONSTRAINT DF_LabCatalog_Price DEFAULT 0,
    IsActive bit NOT NULL CONSTRAINT DF_LabCatalog_Active DEFAULT 1,
    CONSTRAINT UQ_LabCatalog_Code UNIQUE(TestCode)
);
CREATE TABLE gh.LabOrders
(
    LabOrderId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_LabOrders PRIMARY KEY,
    EncounterId bigint NOT NULL,
    PatientId int NOT NULL,
    OrderedByDoctorId int NOT NULL,
    Status nvarchar(20) NOT NULL CONSTRAINT DF_LabOrders_Status DEFAULT 'Ordered',
    OrderedAt datetime2(0) NOT NULL CONSTRAINT DF_LabOrders_Ordered DEFAULT SYSUTCDATETIME(),
    CollectedAt datetime2(0) NULL,
    ReviewedAt datetime2(0) NULL,
    ReviewedByDoctorId int NULL,
    CONSTRAINT CK_LabOrders_Status CHECK (Status IN ('Ordered','Collected','Processing','Completed','Cancelled')),
    CONSTRAINT FK_LabOrders_Encounter FOREIGN KEY (EncounterId) REFERENCES gh.Encounters(EncounterId),
    CONSTRAINT FK_LabOrders_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId),
    CONSTRAINT FK_LabOrders_Orderer FOREIGN KEY (OrderedByDoctorId) REFERENCES dbo.doctorTbl(doctorID),
    CONSTRAINT FK_LabOrders_Reviewer FOREIGN KEY (ReviewedByDoctorId) REFERENCES dbo.doctorTbl(doctorID)
);
CREATE TABLE gh.LabOrderItems
(
    LabOrderItemId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_LabOrderItems PRIMARY KEY,
    LabOrderId bigint NOT NULL,
    LabTestId int NOT NULL,
    ResultValue nvarchar(250) NULL,
    ResultUnit nvarchar(40) NULL,
    ReferenceRange nvarchar(250) NULL,
    ResultFlag nvarchar(15) NULL,
    ResultStatus nvarchar(20) NOT NULL CONSTRAINT DF_LabOrderItems_Status DEFAULT 'Pending',
    ResultedAt datetime2(0) NULL,
    ResultedByUserId int NULL,
    CONSTRAINT CK_LabOrderItems_Flag CHECK (ResultFlag IS NULL OR ResultFlag IN ('Normal','Low','High','Critical','Abnormal')),
    CONSTRAINT CK_LabOrderItems_Status CHECK (ResultStatus IN ('Pending','Processing','Final','Corrected')),
    CONSTRAINT FK_LabOrderItems_Order FOREIGN KEY (LabOrderId) REFERENCES gh.LabOrders(LabOrderId),
    CONSTRAINT FK_LabOrderItems_Test FOREIGN KEY (LabTestId) REFERENCES gh.LabCatalog(LabTestId),
    CONSTRAINT FK_LabOrderItems_ResultBy FOREIGN KEY (ResultedByUserId) REFERENCES gh.UserAccounts(UserId)
);
CREATE INDEX IX_LabOrders_Worklist ON gh.LabOrders(Status, OrderedAt) INCLUDE (PatientId);

CREATE TABLE gh.Suppliers
(
    SupplierId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    SupplierName nvarchar(160) NOT NULL CONSTRAINT UQ_Suppliers_Name UNIQUE,
    ContactName nvarchar(160) NULL, Phone nvarchar(30) NULL, Email nvarchar(254) NULL, IsActive bit NOT NULL CONSTRAINT DF_Suppliers_Active DEFAULT 1
);
CREATE TABLE gh.MedicationBatches
(
    MedicationBatchId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicationBatches PRIMARY KEY,
    MedicationId int NOT NULL,
    SupplierId int NULL,
    BatchNumber nvarchar(80) NOT NULL,
    ExpirationDate date NOT NULL,
    QuantityReceived decimal(12,2) NOT NULL,
    QuantityOnHand decimal(12,2) NOT NULL,
    MinimumStock decimal(12,2) NOT NULL CONSTRAINT DF_MedicationBatches_Min DEFAULT 0,
    UnitCost decimal(12,2) NOT NULL CONSTRAINT DF_MedicationBatches_Cost DEFAULT 0,
    ReceivedAt datetime2(0) NOT NULL CONSTRAINT DF_MedicationBatches_Received DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_MedicationBatches UNIQUE(MedicationId, BatchNumber),
    CONSTRAINT CK_MedicationBatches_Quantity CHECK (QuantityOnHand >= 0 AND QuantityReceived >= 0),
    CONSTRAINT FK_MedicationBatches_Medication FOREIGN KEY (MedicationId) REFERENCES gh.MedicationCatalog(MedicationId),
    CONSTRAINT FK_MedicationBatches_Supplier FOREIGN KEY (SupplierId) REFERENCES gh.Suppliers(SupplierId)
);
CREATE INDEX IX_MedicationBatches_Stock ON gh.MedicationBatches(MedicationId, ExpirationDate) INCLUDE (QuantityOnHand, MinimumStock);
CREATE TABLE gh.Dispensations
(
    DispensationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Dispensations PRIMARY KEY,
    PrescriptionItemId bigint NOT NULL,
    MedicationBatchId bigint NOT NULL,
    Quantity decimal(12,2) NOT NULL,
    DispensedByUserId int NOT NULL,
    DispensedAt datetime2(0) NOT NULL CONSTRAINT DF_Dispensations_At DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Dispensations_Quantity CHECK (Quantity > 0),
    CONSTRAINT FK_Dispensations_Item FOREIGN KEY (PrescriptionItemId) REFERENCES gh.PrescriptionItems(PrescriptionItemId),
    CONSTRAINT FK_Dispensations_Batch FOREIGN KEY (MedicationBatchId) REFERENCES gh.MedicationBatches(MedicationBatchId),
    CONSTRAINT FK_Dispensations_User FOREIGN KEY (DispensedByUserId) REFERENCES gh.UserAccounts(UserId)
);

CREATE TABLE gh.InsuranceProviders
(
    InsuranceProviderId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InsuranceProviders PRIMARY KEY,
    ProviderName nvarchar(160) NOT NULL CONSTRAINT UQ_InsuranceProviders_Name UNIQUE,
    ProviderCode nvarchar(40) NULL CONSTRAINT UQ_InsuranceProviders_Code UNIQUE,
    Phone nvarchar(30) NULL, Email nvarchar(254) NULL, IsActive bit NOT NULL CONSTRAINT DF_InsuranceProviders_Active DEFAULT 1
);
ALTER TABLE gh.PatientProfiles ADD CONSTRAINT FK_PatientProfiles_Insurance FOREIGN KEY (InsuranceProviderId) REFERENCES gh.InsuranceProviders(InsuranceProviderId);
CREATE TABLE gh.InsurancePolicies
(
    InsurancePolicyId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_InsurancePolicies PRIMARY KEY,
    PatientId int NOT NULL, InsuranceProviderId int NOT NULL, PolicyNumber nvarchar(80) NOT NULL,
    CoveragePercent decimal(5,2) NOT NULL, ValidFrom date NULL, ValidTo date NULL, IsActive bit NOT NULL CONSTRAINT DF_InsurancePolicies_Active DEFAULT 1,
    CONSTRAINT UQ_InsurancePolicies UNIQUE(InsuranceProviderId, PolicyNumber),
    CONSTRAINT CK_InsurancePolicies_Coverage CHECK (CoveragePercent BETWEEN 0 AND 100),
    CONSTRAINT FK_InsurancePolicies_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId),
    CONSTRAINT FK_InsurancePolicies_Provider FOREIGN KEY (InsuranceProviderId) REFERENCES gh.InsuranceProviders(InsuranceProviderId)
);

CREATE TABLE gh.Invoices
(
    InvoiceId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
    InvoiceNumber nvarchar(30) NOT NULL CONSTRAINT UQ_Invoices_Number UNIQUE,
    PatientId int NOT NULL, EncounterId bigint NULL, AppointmentId bigint NULL,
    Status nvarchar(20) NOT NULL CONSTRAINT DF_Invoices_Status DEFAULT 'Open',
    TotalAmount decimal(12,2) NOT NULL CONSTRAINT DF_Invoices_Total DEFAULT 0,
    PaidAmount decimal(12,2) NOT NULL CONSTRAINT DF_Invoices_Paid DEFAULT 0,
    DueDate date NULL, CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Invoices_Created DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Invoices_Status CHECK (Status IN ('Draft','Open','PartiallyPaid','Paid','Voided')),
    CONSTRAINT CK_Invoices_Amounts CHECK (TotalAmount >= 0 AND PaidAmount >= 0 AND PaidAmount <= TotalAmount),
    CONSTRAINT FK_Invoices_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId),
    CONSTRAINT FK_Invoices_Encounter FOREIGN KEY (EncounterId) REFERENCES gh.Encounters(EncounterId),
    CONSTRAINT FK_Invoices_Appointment FOREIGN KEY (AppointmentId) REFERENCES gh.Appointments(AppointmentId)
);
CREATE TABLE gh.InvoiceItems
(
    InvoiceItemId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_InvoiceItems PRIMARY KEY,
    InvoiceId bigint NOT NULL, ItemType nvarchar(30) NOT NULL, Description nvarchar(300) NOT NULL,
    Quantity decimal(12,2) NOT NULL CONSTRAINT DF_InvoiceItems_Qty DEFAULT 1, UnitPrice decimal(12,2) NOT NULL,
    LineTotal AS CONVERT(decimal(12,2), Quantity * UnitPrice) PERSISTED,
    CONSTRAINT CK_InvoiceItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT FK_InvoiceItems_Invoice FOREIGN KEY (InvoiceId) REFERENCES gh.Invoices(InvoiceId)
);
CREATE TABLE gh.Payments
(
    PaymentId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    InvoiceId bigint NOT NULL, Amount decimal(12,2) NOT NULL, Method nvarchar(30) NOT NULL,
    ReferenceNumber nvarchar(80) NULL, ReceivedByUserId int NULL, ReceivedAt datetime2(0) NOT NULL CONSTRAINT DF_Payments_Received DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
    CONSTRAINT FK_Payments_Invoice FOREIGN KEY (InvoiceId) REFERENCES gh.Invoices(InvoiceId),
    CONSTRAINT FK_Payments_User FOREIGN KEY (ReceivedByUserId) REFERENCES gh.UserAccounts(UserId)
);
CREATE TABLE gh.InsuranceClaims
(
    InsuranceClaimId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_InsuranceClaims PRIMARY KEY,
    InvoiceId bigint NOT NULL, InsurancePolicyId bigint NOT NULL, ClaimNumber nvarchar(80) NOT NULL CONSTRAINT UQ_InsuranceClaims_Number UNIQUE,
    ClaimedAmount decimal(12,2) NOT NULL, ApprovedAmount decimal(12,2) NULL, Status nvarchar(20) NOT NULL CONSTRAINT DF_InsuranceClaims_Status DEFAULT 'Submitted',
    SubmittedAt datetime2(0) NOT NULL CONSTRAINT DF_InsuranceClaims_Submitted DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_InsuranceClaims_Status CHECK (Status IN ('Draft','Submitted','UnderReview','Approved','Rejected','Paid')),
    CONSTRAINT FK_InsuranceClaims_Invoice FOREIGN KEY (InvoiceId) REFERENCES gh.Invoices(InvoiceId),
    CONSTRAINT FK_InsuranceClaims_Policy FOREIGN KEY (InsurancePolicyId) REFERENCES gh.InsurancePolicies(InsurancePolicyId)
);

CREATE TABLE gh.Wards
(
    WardId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Wards PRIMARY KEY, DepartmentId int NULL, WardName nvarchar(100) NOT NULL,
    WardType nvarchar(30) NOT NULL, IsActive bit NOT NULL CONSTRAINT DF_Wards_Active DEFAULT 1,
    CONSTRAINT UQ_Wards_Name UNIQUE(WardName), CONSTRAINT FK_Wards_Department FOREIGN KEY (DepartmentId) REFERENCES gh.Departments(DepartmentId)
);
CREATE TABLE gh.Rooms
(
    RoomId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rooms PRIMARY KEY, WardId int NOT NULL, RoomNumber nvarchar(30) NOT NULL,
    RoomType nvarchar(30) NOT NULL, IsActive bit NOT NULL CONSTRAINT DF_Rooms_Active DEFAULT 1,
    CONSTRAINT UQ_Rooms UNIQUE(WardId, RoomNumber), CONSTRAINT FK_Rooms_Ward FOREIGN KEY (WardId) REFERENCES gh.Wards(WardId)
);
CREATE TABLE gh.Beds
(
    BedId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Beds PRIMARY KEY, RoomId int NOT NULL, BedNumber nvarchar(30) NOT NULL,
    Status nvarchar(20) NOT NULL CONSTRAINT DF_Beds_Status DEFAULT 'Available',
    CONSTRAINT UQ_Beds UNIQUE(RoomId, BedNumber), CONSTRAINT CK_Beds_Status CHECK (Status IN ('Available','Occupied','Cleaning','OutOfService')),
    CONSTRAINT FK_Beds_Room FOREIGN KEY (RoomId) REFERENCES gh.Rooms(RoomId)
);
CREATE TABLE gh.Admissions
(
    AdmissionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Admissions PRIMARY KEY, PatientId int NOT NULL, BedId int NULL,
    AttendingDoctorId int NULL, AdmittedAt datetime2(0) NOT NULL CONSTRAINT DF_Admissions_At DEFAULT SYSUTCDATETIME(), DischargedAt datetime2(0) NULL,
    Status nvarchar(20) NOT NULL CONSTRAINT DF_Admissions_Status DEFAULT 'Admitted', AdmissionReason nvarchar(500) NULL, DischargeSummary nvarchar(max) NULL,
    CONSTRAINT CK_Admissions_Status CHECK (Status IN ('Admitted','Transferred','Discharged','Cancelled')),
    CONSTRAINT FK_Admissions_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId),
    CONSTRAINT FK_Admissions_Bed FOREIGN KEY (BedId) REFERENCES gh.Beds(BedId),
    CONSTRAINT FK_Admissions_Doctor FOREIGN KEY (AttendingDoctorId) REFERENCES dbo.doctorTbl(doctorID)
);
CREATE UNIQUE INDEX UX_Admissions_ActiveBed ON gh.Admissions(BedId) WHERE DischargedAt IS NULL AND BedId IS NOT NULL;
CREATE TABLE gh.TriageAssessments
(
    TriageAssessmentId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TriageAssessments PRIMARY KEY, PatientId int NOT NULL,
    AssessedByUserId int NULL, AcuityLevel tinyint NOT NULL, ChiefComplaint nvarchar(1000) NULL, AssessedAt datetime2(0) NOT NULL CONSTRAINT DF_Triage_At DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Triage_Acuity CHECK (AcuityLevel BETWEEN 1 AND 5), CONSTRAINT FK_Triage_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId),
    CONSTRAINT FK_Triage_User FOREIGN KEY (AssessedByUserId) REFERENCES gh.UserAccounts(UserId)
);

CREATE TABLE gh.Documents
(
    DocumentId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Documents PRIMARY KEY, PatientId int NOT NULL, EncounterId bigint NULL,
    FileName nvarchar(260) NOT NULL, ContentType nvarchar(100) NOT NULL, StorageKey nvarchar(500) NOT NULL, UploadedByUserId int NULL,
    UploadedAt datetime2(0) NOT NULL CONSTRAINT DF_Documents_At DEFAULT SYSUTCDATETIME(), IsActive bit NOT NULL CONSTRAINT DF_Documents_Active DEFAULT 1,
    CONSTRAINT FK_Documents_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId),
    CONSTRAINT FK_Documents_Encounter FOREIGN KEY (EncounterId) REFERENCES gh.Encounters(EncounterId),
    CONSTRAINT FK_Documents_User FOREIGN KEY (UploadedByUserId) REFERENCES gh.UserAccounts(UserId)
);
CREATE TABLE gh.Notifications
(
    NotificationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY, UserId int NOT NULL, PatientId int NULL,
    NotificationType nvarchar(40) NOT NULL, Subject nvarchar(200) NOT NULL, Message nvarchar(1000) NOT NULL,
    ScheduledFor datetime2(0) NULL, SentAt datetime2(0) NULL, ReadAt datetime2(0) NULL, CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Notifications_At DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES gh.UserAccounts(UserId),
    CONSTRAINT FK_Notifications_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId)
);
CREATE INDEX IX_Notifications_Inbox ON gh.Notifications(UserId, ReadAt, CreatedAt DESC);

COMMIT TRANSACTION;
GO
CREATE OR ALTER PROCEDURE gh.BookAppointment
    @PatientId int, @DoctorId int, @DepartmentId int = NULL, @ScheduledStart datetime2(0), @ScheduledEnd datetime2(0),
    @Reason nvarchar(500) = NULL, @BookedByUserId int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @ScheduledEnd <= @ScheduledStart THROW 51001, 'Appointment end must be after its start.', 1;
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM gh.Appointments WITH (UPDLOCK, HOLDLOCK)
               WHERE DoctorId = @DoctorId AND Status NOT IN ('Cancelled','NoShow')
               AND @ScheduledStart < ScheduledEnd AND @ScheduledEnd > ScheduledStart)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51002, 'The selected doctor is already booked for that time.', 1;
    END;
    INSERT INTO gh.Appointments(PatientId, DoctorId, DepartmentId, ScheduledStart, ScheduledEnd, Reason, Status, BookedByUserId)
    VALUES(@PatientId, @DoctorId, @DepartmentId, @ScheduledStart, @ScheduledEnd, @Reason, 'Booked', @BookedByUserId);
    DECLARE @AppointmentId bigint = SCOPE_IDENTITY();
    COMMIT TRANSACTION;
    SELECT @AppointmentId AS AppointmentId;
END;
GO
CREATE OR ALTER VIEW gh.vwPatientTimeline
AS
    SELECT p.patientId AS PatientId, p.registeredAt AS EventAt, CAST('Registration' AS nvarchar(40)) AS EventType, CAST(p.patientName AS nvarchar(500)) AS Detail
    FROM dbo.patientTbl p
    UNION ALL SELECT e.PatientId, e.StartedAt, 'Encounter', ISNULL(e.ChiefComplaint, 'Clinical encounter') FROM gh.Encounters e
    UNION ALL SELECT a.PatientId, a.ScheduledStart, 'Appointment', ISNULL(a.Reason, a.Status) FROM gh.Appointments a
    UNION ALL SELECT l.PatientId, l.OrderedAt, 'Laboratory order', l.Status FROM gh.LabOrders l;
GO
