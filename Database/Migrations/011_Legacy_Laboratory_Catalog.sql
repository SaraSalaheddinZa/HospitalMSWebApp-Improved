/* Retain existing catalog definitions when moving laboratory operations to the dedicated workspace.
   A legacy label is not a verified clinical reference range. No ranges or units are invented. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT INTO gh.LabCatalog(TestCode,TestName,Price,IsActive)
SELECT 'LEGACY-'+CONVERT(varchar(20),l.testId),l.testName,
       CASE WHEN l.testCost>=0 THEN l.testCost ELSE 0 END,1
FROM dbo.labTestTbl l
WHERE NOT EXISTS(SELECT 1 FROM gh.LabCatalog c WHERE c.TestCode='LEGACY-'+CONVERT(varchar(20),l.testId))
  AND NOT EXISTS(SELECT 1 FROM gh.LabCatalog c WHERE c.TestName=l.testName);
COMMIT;
