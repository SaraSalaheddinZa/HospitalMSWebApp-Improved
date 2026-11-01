using System;
using System.Data;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Services
{
    public sealed partial class HospitalWorkflowService
    {
        public DataTable FindMedicalTeam(int actor,string name=null,string specialty=null,int? department=null,DateTime? date=null,int? doctor=null,TimeZoneInfo displayZone=null)
        {
            var zone=displayZone??TimeZoneInfo.Utc;
            name=(name??"").Trim();specialty=(specialty??"").Trim();
            if(name.Length>100||specialty.Length>50||date.HasValue&&(date.Value.Year==9999||date.Value.Date<HospitalTime.Local(DateTime.UtcNow,zone).Date))throw new ArgumentException("Use a name up to 100 characters and a current or future date.");
            DateTime? from=date.HasValue?(DateTime?)HospitalTime.ToUtc(date.Value.Date,zone):null;
            DateTime? until=date.HasValue?(DateTime?)HospitalTime.ToUtc(date.Value.Date.AddDays(1),zone):null;
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Patient","Receptionist","Admin","Doctor");
                return ReadTable(c,t,@"SELECT d.doctorId AS DoctorId,d.doctorName AS DoctorName,d.doctorSpec AS Specialty,
                p.DepartmentId,dep.DepartmentName,MIN(s.SlotStart) AS NextAvailable,COUNT(s.DoctorScheduleId) AS AvailableSlots
                FROM dbo.doctorTbl d LEFT JOIN gh.DoctorPublicProfiles p ON p.DoctorId=d.doctorId
                LEFT JOIN gh.Departments dep ON dep.DepartmentId=p.DepartmentId
                LEFT JOIN gh.vwBookableDoctorSlots s ON s.DoctorId=d.doctorId AND (@Date IS NULL OR (s.SlotStart>=@Date AND s.SlotStart<@Until))
                WHERE d.doctorStatus='Active' AND (@Doctor IS NULL OR d.doctorId=@Doctor)
                AND NOT EXISTS(SELECT 1 FROM gh.UserAccounts u WHERE u.RoleName='Doctor' AND u.LegacyUserId=d.doctorId AND u.IsActive=0)
                AND (@Name='' OR CHARINDEX(@Name,d.doctorName)>0) AND (@Specialty='' OR d.doctorSpec=@Specialty)
                AND (@Department IS NULL OR p.DepartmentId=@Department)
                GROUP BY d.doctorId,d.doctorName,d.doctorSpec,p.DepartmentId,dep.DepartmentName
                HAVING @Date IS NULL OR COUNT(s.DoctorScheduleId)>0 ORDER BY d.doctorName",
                P("@Name",name),P("@Specialty",specialty),P("@Department",department),P("@Date",from),P("@Until",until),P("@Doctor",doctor));
            });
        }
        public DataTable GetMedicalTeamSlots(int doctor,DateTime date,int actor,TimeZoneInfo displayZone=null)
        {
            var zone=displayZone??TimeZoneInfo.Utc;
            if(date.Year==9999||date.Date<HospitalTime.Local(DateTime.UtcNow,zone).Date)throw new ArgumentException("Choose a current or future appointment date.");
            var from=HospitalTime.ToUtc(date.Date,zone);var until=HospitalTime.ToUtc(date.Date.AddDays(1),zone);
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Patient","Receptionist","Admin","Doctor");
                return ReadTable(c,t,"SELECT DoctorScheduleId,SlotStart,SlotEnd,SlotMinutes FROM gh.vwBookableDoctorSlots WHERE DoctorId=@D AND SlotStart>=@From AND SlotStart<@Until ORDER BY SlotStart",P("@D",doctor),P("@From",from),P("@Until",until));
            });
        }
        public long BookMedicalTeamSlot(int scheduleId,DateTime start,string reason,int actor)
        {
            if((reason??"").Length>500)throw new ArgumentException("Keep the appointment reason within 500 characters.");
            return database.InTransaction((c,t)=>{
                    RequireActor(c,t,actor,"Patient");
                    var selection=ReadTable(c,t,@"SELECT u.PatientId,s.DoctorId,s.SlotEnd FROM gh.UserAccounts u
                        CROSS JOIN gh.vwBookableDoctorSlots s WHERE u.UserId=@U AND u.RoleName='Patient'
                        AND s.DoctorScheduleId=@S AND s.SlotStart=@Start",P("@U",actor),P("@S",scheduleId),P("@Start",start));
                if(selection.Rows.Count!=1||selection.Rows[0]["PatientId"]==DBNull.Value)throw new InvalidOperationException("This appointment time is no longer available. Choose another time.");
                var row=selection.Rows[0];
                return BookAppointmentCore(c,t,Convert.ToInt32(row["PatientId"]),Convert.ToInt32(row["DoctorId"]),start,Convert.ToDateTime(row["SlotEnd"]),reason,actor);
            });
        }
        public DataTable GetPatientBookingConfirmation(long appointment,int actor)
        {
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Patient");
                return ReadTable(c,t,@"SELECT a.AppointmentId,d.doctorName AS DoctorName,d.doctorSpec AS Specialty,a.ScheduledStart,a.ScheduledEnd,a.Status
                FROM gh.Appointments a JOIN dbo.doctorTbl d ON d.doctorId=a.DoctorId JOIN gh.UserAccounts u ON u.PatientId=a.PatientId
                WHERE u.UserId=@U AND a.AppointmentId=@A",P("@U",actor),P("@A",appointment));
            });
        }
        public void SetDoctorDepartment(int doctor,int? department,int actor)
        {
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin");
                if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM dbo.doctorTbl WHERE doctorId=@D",P("@D",doctor)))!=1)throw new ArgumentException("Select an existing doctor.");
                if(department.HasValue&&Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.Departments WHERE DepartmentId=@D AND IsActive=1",P("@D",department)))!=1)throw new ArgumentException("Select an active department.");
                var previous=Scalar(c,t,"SELECT DepartmentId FROM gh.DoctorPublicProfiles WITH(UPDLOCK,HOLDLOCK) WHERE DoctorId=@D",P("@D",doctor));
                Execute(c,t,@"UPDATE gh.DoctorPublicProfiles SET DepartmentId=@P,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@U WHERE DoctorId=@D;
                IF @@ROWCOUNT=0 INSERT INTO gh.DoctorPublicProfiles(DoctorId,DepartmentId,UpdatedByUserId) VALUES(@D,@P,@U)",P("@D",doctor),P("@P",department),P("@U",actor));
                History(c,t,"DoctorDirectory",doctor,actor,"DepartmentUpdated","Previous department: "+Convert.ToString(previous)+"; new department: "+Convert.ToString(department));return 0;
            });
        }
    }
}
