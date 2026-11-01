SET XACT_ABORT ON;
IF OBJECT_ID('gh.SyntheticDemoRuns','U') IS NULL
CREATE TABLE gh.SyntheticDemoRuns(
 RunKey varchar(40) NOT NULL CONSTRAINT PK_SyntheticDemoRuns PRIMARY KEY,
 CreatedAt datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
 AnchorDate date NOT NULL,
 Status varchar(12) NOT NULL CHECK(Status IN('Ready','Cleaned')),
 OutsideFingerprint varchar(64) NOT NULL,
 CONSTRAINT CK_SyntheticDemoRunKey CHECK(RunKey='guardian-synthetic-v1')
);
IF OBJECT_ID('gh.SyntheticDemoRecords','U') IS NULL
CREATE TABLE gh.SyntheticDemoRecords(
 RunKey varchar(40) NOT NULL REFERENCES gh.SyntheticDemoRuns(RunKey),
 TableName nvarchar(128) NOT NULL,
 KeyColumn sysname NOT NULL,
 RecordId bigint NOT NULL,
 Fingerprint varchar(64) NOT NULL,
 CONSTRAINT PK_SyntheticDemoRecords PRIMARY KEY(RunKey,TableName,RecordId)
);
