/* Additive nursing documentation. Safe to apply to an existing Guardian Health database. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('gh.NursingNotes','U') IS NULL
BEGIN
    CREATE TABLE gh.NursingNotes
    (
        NursingNoteId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_NursingNotes PRIMARY KEY,
        PatientId int NOT NULL,
        AdmissionId bigint NULL,
        RecordedByUserId int NOT NULL,
        CareStatus nvarchar(40) NOT NULL CONSTRAINT DF_NursingNotes_CareStatus DEFAULT 'Stable',
        Note nvarchar(2000) NOT NULL,
        RecordedAt datetime2(0) NOT NULL CONSTRAINT DF_NursingNotes_RecordedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_NursingNotes_CareStatus CHECK (CareStatus IN ('Stable','NeedsReview','Escalated','DischargeReady')),
        CONSTRAINT FK_NursingNotes_Patient FOREIGN KEY (PatientId) REFERENCES dbo.patientTbl(patientId),
        CONSTRAINT FK_NursingNotes_Admission FOREIGN KEY (AdmissionId) REFERENCES gh.Admissions(AdmissionId),
        CONSTRAINT FK_NursingNotes_User FOREIGN KEY (RecordedByUserId) REFERENCES gh.UserAccounts(UserId)
    );
    CREATE INDEX IX_NursingNotes_Patient ON gh.NursingNotes(PatientId, RecordedAt DESC);
END;
COMMIT TRANSACTION;
