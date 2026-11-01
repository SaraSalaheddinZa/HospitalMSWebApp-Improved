SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('gh.DocumentContents','U') IS NULL
CREATE TABLE gh.DocumentContents(DocumentId bigint NOT NULL PRIMARY KEY REFERENCES gh.Documents(DocumentId), Content varbinary(max) NOT NULL, CONSTRAINT CK_DocumentContents_Size CHECK(DATALENGTH(Content) BETWEEN 1 AND 5242880));
COMMIT;
