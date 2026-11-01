SET XACT_ABORT ON;
BEGIN TRANSACTION;
-- Do not invent supplier delivery references for historical stock movements.
IF OBJECT_ID('gh.StockReceipts','U') IS NULL
BEGIN
    CREATE TABLE gh.StockReceipts
    (
        StockReceiptId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockReceipts PRIMARY KEY,
        SupplierId int NOT NULL,
        ReceiptReference nvarchar(80) NOT NULL,
        MedicationBatchId bigint NOT NULL,
        Quantity decimal(12,2) NOT NULL,
        UnitCost decimal(12,2) NOT NULL,
        StockMovementId bigint NOT NULL,
        ReceivedByUserId int NOT NULL,
        ReceivedAt datetime2(0) NOT NULL CONSTRAINT DF_StockReceipts_At DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_StockReceipts_Reference UNIQUE(SupplierId,ReceiptReference),
        CONSTRAINT UQ_StockReceipts_Movement UNIQUE(StockMovementId),
        CONSTRAINT CK_StockReceipts_Values CHECK(Quantity>0 AND UnitCost>=0 AND LEN(LTRIM(RTRIM(ReceiptReference)))>0),
        CONSTRAINT FK_StockReceipts_Supplier FOREIGN KEY(SupplierId) REFERENCES gh.Suppliers(SupplierId),
        CONSTRAINT FK_StockReceipts_Batch FOREIGN KEY(MedicationBatchId) REFERENCES gh.MedicationBatches(MedicationBatchId),
        CONSTRAINT FK_StockReceipts_Movement FOREIGN KEY(StockMovementId) REFERENCES gh.StockMovements(StockMovementId),
        CONSTRAINT FK_StockReceipts_User FOREIGN KEY(ReceivedByUserId) REFERENCES gh.UserAccounts(UserId)
    );
    CREATE INDEX IX_StockReceipts_Batch ON gh.StockReceipts(MedicationBatchId,ReceivedAt DESC);
END;
COMMIT;
