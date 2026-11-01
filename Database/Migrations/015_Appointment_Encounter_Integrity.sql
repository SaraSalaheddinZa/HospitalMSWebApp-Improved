SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF EXISTS(SELECT AppointmentId FROM gh.Encounters WHERE AppointmentId IS NOT NULL GROUP BY AppointmentId HAVING COUNT(*)>1)
    THROW 51130,'Existing duplicate appointment consultations require manual review; no records have been removed.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('gh.Encounters') AND name='UX_Encounters_Appointment')
    CREATE UNIQUE INDEX UX_Encounters_Appointment ON gh.Encounters(AppointmentId) WHERE AppointmentId IS NOT NULL;
COMMIT;
