/* Guardian Health migration 004: schedule-aware appointment booking. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'gh.DoctorSchedules', N'U') IS NULL OR OBJECT_ID(N'gh.Appointments', N'U') IS NULL
    THROW 51010, 'Doctor schedule and appointment tables are required before applying migration 004.', 1;
GO

-- These persisted procedure settings are required by the filtered appointment index.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'gh.DoctorSchedules') AND name=N'IX_DoctorSchedules_Availability')
    CREATE INDEX IX_DoctorSchedules_Availability ON gh.DoctorSchedules(DoctorId, ScheduleDate, IsAvailable) INCLUDE(StartTime, EndTime, SlotMinutes);
GO

CREATE OR ALTER PROCEDURE gh.BookAppointment
    @PatientId int, @DoctorId int, @DepartmentId int = NULL, @ScheduledStart datetime2(0), @ScheduledEnd datetime2(0),
    @Reason nvarchar(500) = NULL, @BookedByUserId int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @ScheduledEnd <= @ScheduledStart THROW 51001, 'Appointment end must be after its start.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.patientTbl WHERE patientId=@PatientId AND IsActive=1)
        THROW 51003, 'The selected patient is not active.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.doctorTbl WHERE doctorID=@DoctorId AND doctorStatus='Active')
        THROW 51004, 'The selected clinician is not active.', 1;

    BEGIN TRANSACTION;
    -- When schedules have been published for the day, bookings must fit an available slot.
    IF EXISTS (SELECT 1 FROM gh.DoctorSchedules WITH (UPDLOCK,HOLDLOCK) WHERE DoctorId=@DoctorId AND ScheduleDate=CAST(@ScheduledStart AS date))
       AND NOT EXISTS (SELECT 1 FROM gh.DoctorSchedules WITH (UPDLOCK,HOLDLOCK)
                       WHERE DoctorId=@DoctorId AND ScheduleDate=CAST(@ScheduledStart AS date) AND IsAvailable=1
                       AND CAST(@ScheduledStart AS time) >= StartTime AND CAST(@ScheduledEnd AS time) <= EndTime
                       AND DATEDIFF(MINUTE, StartTime, CAST(@ScheduledStart AS time)) % SlotMinutes = 0)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51005, 'The selected time is outside the clinician''s published availability.', 1;
    END;
    IF EXISTS (SELECT 1 FROM gh.Appointments WITH (UPDLOCK,HOLDLOCK)
               WHERE DoctorId=@DoctorId AND Status NOT IN ('Cancelled','NoShow')
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

CREATE OR ALTER PROCEDURE gh.GetAvailableDoctorSlots
    @DoctorId int,
    @ScheduleDate date
AS
BEGIN
    SET NOCOUNT ON;
    ;WITH ScheduleWindows AS
    (
        SELECT DoctorId, SlotMinutes,
            CAST(CONVERT(char(10), ScheduleDate, 23) + ' ' + CONVERT(char(8), StartTime) AS datetime2(0)) AS SlotStart,
            CAST(CONVERT(char(10), ScheduleDate, 23) + ' ' + CONVERT(char(8), EndTime) AS datetime2(0)) AS WindowEnd
        FROM gh.DoctorSchedules
        WHERE DoctorId=@DoctorId AND ScheduleDate=@ScheduleDate AND IsAvailable=1
    ), Slots AS
    (
        SELECT DoctorId, SlotMinutes, SlotStart, WindowEnd FROM ScheduleWindows
        UNION ALL
        SELECT DoctorId, SlotMinutes, DATEADD(MINUTE, SlotMinutes, SlotStart), WindowEnd
        FROM Slots WHERE DATEADD(MINUTE, SlotMinutes * 2, SlotStart) <= WindowEnd
    )
    SELECT SlotStart AS [Available start], DATEADD(MINUTE, SlotMinutes, SlotStart) AS [Available end]
    FROM Slots s
    WHERE DATEADD(MINUTE, SlotMinutes, SlotStart) <= WindowEnd
      AND NOT EXISTS (SELECT 1 FROM gh.Appointments a WHERE a.DoctorId=s.DoctorId AND a.Status NOT IN ('Cancelled','NoShow')
                      AND s.SlotStart < a.ScheduledEnd AND DATEADD(MINUTE, s.SlotMinutes, s.SlotStart) > a.ScheduledStart)
    ORDER BY SlotStart
    OPTION (MAXRECURSION 1000);
END;
GO
