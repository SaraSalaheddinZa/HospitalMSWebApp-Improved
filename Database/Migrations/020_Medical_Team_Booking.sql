/* Additive directory metadata; no existing staff identities or credentials are changed. */
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID('gh.DoctorPublicProfiles','U') IS NULL
CREATE TABLE gh.DoctorPublicProfiles(
    DoctorId int NOT NULL CONSTRAINT PK_DoctorPublicProfiles PRIMARY KEY REFERENCES dbo.doctorTbl(doctorId),
    DepartmentId int NULL REFERENCES gh.Departments(DepartmentId),
    UpdatedAt datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedByUserId int NOT NULL REFERENCES gh.UserAccounts(UserId)
);
GO
CREATE OR ALTER VIEW gh.vwBookableDoctorSlots AS
WITH Digits AS (SELECT n FROM (VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) d(n)),
Numbers AS (SELECT a.n+10*b.n+100*c.n AS n FROM Digits a CROSS JOIN Digits b CROSS JOIN Digits c),
Slots AS (
 SELECT s.DoctorScheduleId,s.DoctorId,COALESCE(s.DepartmentId,p.DepartmentId) DepartmentId,s.ScheduleDate,s.SlotMinutes,
 DATEADD(MINUTE,n.n*s.SlotMinutes,DATEADD(SECOND,DATEDIFF(SECOND,CAST('00:00' AS time),s.StartTime),CAST(s.ScheduleDate AS datetime2))) SlotStart,
 DATEADD(SECOND,DATEDIFF(SECOND,CAST('00:00' AS time),s.EndTime),CAST(s.ScheduleDate AS datetime2)) WindowEnd
 FROM gh.DoctorSchedules s JOIN dbo.doctorTbl d ON d.doctorId=s.DoctorId
 LEFT JOIN gh.DoctorPublicProfiles p ON p.DoctorId=s.DoctorId
 CROSS JOIN Numbers n WHERE s.IsAvailable=1 AND d.doctorStatus='Active'
 AND s.ScheduleDate>=CAST(SYSUTCDATETIME() AS date)
 AND NOT EXISTS(SELECT 1 FROM gh.UserAccounts u WHERE u.RoleName='Doctor' AND u.LegacyUserId=d.doctorId AND u.IsActive=0)
)
SELECT DoctorScheduleId,DoctorId,DepartmentId,ScheduleDate,SlotMinutes,SlotStart,DATEADD(MINUTE,SlotMinutes,SlotStart) SlotEnd
FROM Slots s WHERE SlotStart>SYSUTCDATETIME() AND DATEADD(MINUTE,SlotMinutes,SlotStart)<=WindowEnd
AND NOT EXISTS(SELECT 1 FROM gh.Appointments a WHERE a.DoctorId=s.DoctorId AND a.Status NOT IN('Cancelled','NoShow')
AND s.SlotStart<a.ScheduledEnd AND DATEADD(MINUTE,s.SlotMinutes,s.SlotStart)>a.ScheduledStart);
GO
CREATE OR ALTER PROCEDURE gh.BookAppointment
 @PatientId int,@DoctorId int,@DepartmentId int=NULL,@ScheduledStart datetime2(0),@ScheduledEnd datetime2(0),
 @Reason nvarchar(500)=NULL,@BookedByUserId int=NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @ScheduledStart<=SYSUTCDATETIME() OR @ScheduledEnd<=@ScheduledStart OR CAST(@ScheduledStart AS date)<>CAST(@ScheduledEnd AS date)
 OR DATEPART(SECOND,@ScheduledStart)<>0 OR DATEPART(SECOND,@ScheduledEnd)<>0
 THROW 51001,'Choose a future same-day appointment using whole minutes.',1;
 BEGIN TRANSACTION;
 IF NOT EXISTS(SELECT 1 FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@BookedByUserId AND IsActive=1
 AND (RoleName IN('Admin','Receptionist') OR (RoleName='Patient' AND PatientId=@PatientId)))
 THROW 51006,'This account cannot book for the selected patient.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.patientTbl WHERE patientId=@PatientId AND IsActive=1)
 THROW 51003,'The selected patient is not active.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.doctorTbl WHERE doctorId=@DoctorId AND doctorStatus='Active')
 OR EXISTS(SELECT 1 FROM gh.UserAccounts WHERE RoleName='Doctor' AND LegacyUserId=@DoctorId AND IsActive=0)
 THROW 51004,'The selected clinician is not active.',1;
 DECLARE @ScheduleId int;
 SELECT @ScheduleId=DoctorScheduleId,@DepartmentId=DepartmentId FROM gh.DoctorSchedules WITH(UPDLOCK,HOLDLOCK)
 WHERE DoctorId=@DoctorId AND IsAvailable=1 AND ScheduleDate=CAST(@ScheduledStart AS date)
 AND CAST(@ScheduledStart AS time)>=StartTime AND CAST(@ScheduledEnd AS time)<=EndTime
 AND DATEDIFF(MINUTE,StartTime,CAST(@ScheduledStart AS time))%SlotMinutes=0
 AND DATEDIFF(MINUTE,@ScheduledStart,@ScheduledEnd)%SlotMinutes=0;
 IF @ScheduleId IS NULL THROW 51005,'Choose a time aligned to published clinician availability.',1;
 IF @DepartmentId IS NULL SELECT @DepartmentId=DepartmentId FROM gh.DoctorPublicProfiles WHERE DoctorId=@DoctorId;
 IF EXISTS(SELECT 1 FROM gh.Appointments WITH(UPDLOCK,HOLDLOCK) WHERE Status NOT IN('Cancelled','NoShow')
 AND (DoctorId=@DoctorId OR PatientId=@PatientId) AND @ScheduledStart<ScheduledEnd AND @ScheduledEnd>ScheduledStart)
 THROW 51002,'The clinician or patient is already booked for that time.',1;
 INSERT INTO gh.Appointments(PatientId,DoctorId,DepartmentId,ScheduledStart,ScheduledEnd,Reason,Status,BookedByUserId)
 VALUES(@PatientId,@DoctorId,@DepartmentId,@ScheduledStart,@ScheduledEnd,@Reason,'Booked',@BookedByUserId);
 DECLARE @Id bigint=SCOPE_IDENTITY();
 COMMIT TRANSACTION; SELECT @Id AS AppointmentId;
END;
GO
