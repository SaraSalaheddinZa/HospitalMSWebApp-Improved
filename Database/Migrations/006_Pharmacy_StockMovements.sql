SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('gh.StockMovements','U') IS NULL
BEGIN
 CREATE TABLE gh.StockMovements(StockMovementId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockMovements PRIMARY KEY,MedicationBatchId bigint NOT NULL,MovementType nvarchar(30) NOT NULL,Quantity decimal(12,2) NOT NULL,ReferenceType nvarchar(40) NULL,ReferenceId nvarchar(80) NULL,RecordedByUserId int NULL,RecordedAt datetime2(0) NOT NULL CONSTRAINT DF_StockMovements_At DEFAULT SYSUTCDATETIME(),CONSTRAINT CK_StockMovements_Type CHECK(MovementType IN('Received','Dispensed','Adjustment','Expired')),CONSTRAINT CK_StockMovements_Quantity CHECK(Quantity<>0),CONSTRAINT FK_StockMovements_Batch FOREIGN KEY(MedicationBatchId) REFERENCES gh.MedicationBatches(MedicationBatchId),CONSTRAINT FK_StockMovements_User FOREIGN KEY(RecordedByUserId) REFERENCES gh.UserAccounts(UserId));
 CREATE INDEX IX_StockMovements_Batch ON gh.StockMovements(MedicationBatchId,RecordedAt DESC);
END;
COMMIT TRANSACTION;
