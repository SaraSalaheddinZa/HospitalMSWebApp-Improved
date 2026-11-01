using System;
using System.Data;
namespace HospitalMSWebApp.Services
{
    public sealed partial class HospitalWorkflowService
    {
        public DataTable PatientAppointments(int actor, long? appointment = null)
        {
            return database.InTransaction((c,t)=> {
                RequireActor(c,t,actor,"Patient");
                return ReadTable(c,t,@"SELECT a.AppointmentId,a.ScheduledStart,a.ScheduledEnd,a.Status,a.Reason,
                    d.doctorName AS DoctorName,d.doctorSpec AS Specialty,dep.DepartmentName
                    FROM gh.Appointments a JOIN gh.UserAccounts u ON u.PatientId=a.PatientId
                    JOIN dbo.doctorTbl d ON d.doctorId=a.DoctorId
                    LEFT JOIN gh.Departments dep ON dep.DepartmentId=a.DepartmentId
                    WHERE u.UserId=@U AND (@A IS NULL OR a.AppointmentId=@A)
                    ORDER BY a.ScheduledStart,a.AppointmentId",P("@U",actor),P("@A",appointment));
            });
        }
        public DataTable PatientAppointmentHistory(long appointment,int actor)
        {
            return database.InTransaction((c,t)=> {
                RequireActor(c,t,actor,"Patient");
                return ReadTable(c,t,@"SELECT h.OccurredAt,h.PreviousStatus,h.NewStatus,h.Detail
                    FROM gh.WorkflowHistory h JOIN gh.Appointments a ON a.AppointmentId=h.EntityId
                    JOIN gh.UserAccounts u ON u.PatientId=a.PatientId
                    WHERE h.EntityType='Appointment' AND h.EntityId=@A AND u.UserId=@U
                    ORDER BY h.OccurredAt DESC,h.HistoryId DESC",P("@A",appointment),P("@U",actor));
            });
        }
    }
}
