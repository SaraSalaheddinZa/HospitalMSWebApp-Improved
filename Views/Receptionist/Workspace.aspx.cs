using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Receptionist
{
    public class Workspace : WorkspacePage
    {
        protected override string Role { get { return "Receptionist"; } }
        protected override string Heading { get { return "Reception & appointments"; } }
        protected override string SubmissionReturnUrl { get { long id;string task=V("action")=="reschedule"?"reschedule-appointment":V("action")=="decline-request"?"decline-rescheduling-request":"selected-appointment";return "Workspace.aspx"+(long.TryParse(V("appointmentId"),out id)&&id>0?"?appointmentId="+id+"#"+task:""); } }
        protected override void Submit()
        {
            var action=V("action");
            if(action=="access"){
                string error;if(!PasswordSecurity.IsAcceptable(Required("password"),out error))throw new ArgumentException(error);
                var id=Workflows.CreatePatientAccount((int)Id("patientId"),Required("email"),V("password"));Audit("PatientPortalAccessCreated","UserAccount",id);return;
            }
            if(action=="book"){
                var id=BookSelectedAppointment((int)Id("patientId"));
                Audit("AppointmentBooked","Appointment",id);
            } else if(action=="context-status"){
                var id=Id("appointmentId");Workflows.TransitionAppointment(id,Required("status"),CurrentUser.UserId,null,null,null,null,HospitalTime.ParseUtc(V("observedStart")),Required("observedStatus"),HospitalTime.ParseUtc(V("observedEnd")));Audit("AppointmentStatusChanged","Appointment",id);
            } else if(action=="status"){
                var id=Id("appointmentId");Workflows.TransitionAppointment(id,Required("status"),CurrentUser.UserId);Audit("AppointmentStatusChanged","Appointment",id);
            } else if(action=="reschedule"){
                var id=Id("appointmentId");Workflows.RescheduleAppointment(id,Date("start"),Date("end"),CurrentUser.UserId);Audit("AppointmentRescheduled","Appointment",id);
            } else if(action=="decline-request"){
                var id=Id("appointmentId");Workflows.DeclineRescheduleRequest(id,Required("reason"),CurrentUser.UserId);Audit("RescheduleRequestDeclined","Appointment",id);
            } else throw new ArgumentException("Unknown action.");
        }
        protected override string Content()
        {
            var q=(Request.QueryString["q"]??"").Trim();
            var appointments=Db.Query(@"SELECT a.AppointmentId AS ID,p.MedicalRecordNumber AS MRN,p.patientName AS Patient,d.doctorName AS Clinician,a.ScheduledStart AS Starts,a.Status
                FROM gh.Appointments a JOIN dbo.patientTbl p ON p.patientId=a.PatientId JOIN dbo.doctorTbl d ON d.doctorID=a.DoctorId
                WHERE (@Q='' AND CAST(a.ScheduledStart AS date)=CAST(SYSUTCDATETIME() AS date)) OR (@Q<>'' AND (p.patientName LIKE '%'+@Q+'%' OR p.MedicalRecordNumber LIKE '%'+@Q+'%'))
                ORDER BY a.ScheduledStart",P("@Q",q));
            var select=Options("Appointment","appointmentId",@"SELECT a.AppointmentId,p.MedicalRecordNumber+' · '+p.patientName+' · '+d.doctorName+' · '+CONVERT(varchar(16),a.ScheduledStart,120)+' UTC · '+a.Status FROM gh.Appointments a JOIN dbo.patientTbl p ON p.patientId=a.PatientId JOIN dbo.doctorTbl d ON d.doctorId=a.DoctorId WHERE a.Status NOT IN('Completed','Cancelled','NoShow') ORDER BY a.ScheduledStart");
            long selected;if(long.TryParse(Request.QueryString["appointmentId"],out selected)&&selected>0)select=select.Replace("value='"+selected+"'","value='"+selected+"' selected");
            return "<div class='row'>"+Metric("Today's appointments","SELECT COUNT(*) FROM gh.Appointments WHERE CAST(ScheduledStart AS date)=CAST(SYSUTCDATETIME() AS date)")
                +Metric("Waiting","SELECT COUNT(*) FROM gh.Appointments WHERE Status='CheckedIn'")
                +Metric("In consultation","SELECT COUNT(*) FROM gh.Appointments WHERE Status='InProgress'")
                +Metric("Active patients","SELECT COUNT(*) FROM dbo.patientTbl WHERE IsActive=1")+"</div>"
                +Panel("Today's appointments and patient search","<form method='get' class='input-group mb-3'><input class='form-control' type='search' name='q' aria-label='Search all appointments by name or MRN' value='"+H(q)+"' placeholder='Search all appointments by name or MRN'><button class='btn btn-primary'>Search</button></form>"+GuardianUi.Table(appointments,"ID","Workspace.aspx?appointmentId=","Open appointment","#selected-appointment"))
                +SelectedAppointment()
                +Panel("Book appointment",AppointmentBooking(true))
                +Panel("Published availability",Table(Db.Query("SELECT d.doctorName AS Clinician,s.ScheduleDate AS Date,s.StartTime AS Starts,s.EndTime AS Ends,s.SlotMinutes AS Minutes FROM gh.DoctorSchedules s JOIN dbo.doctorTbl d ON d.doctorID=s.DoctorId WHERE s.IsAvailable=1 AND s.ScheduleDate>=CAST(SYSUTCDATETIME() AS date) ORDER BY s.ScheduleDate")))
                +Panel("Queue and appointment status","<p>Open an appointment to see its patient, reserved time, current state and available actions. Check-in is available on the appointment date (UTC); consultation completion stays with the doctor.</p><form method='get' action='Workspace.aspx#selected-appointment'>"+select+"<button class='btn btn-primary'>Open appointment</button></form>")
                +Panel("Patient rescheduling requests",Table(Db.Query("SELECT a.AppointmentId AS ID,p.patientName AS Patient,a.ScheduledStart AS CurrentTime,h.Detail AS RequestedChange FROM gh.Appointments a JOIN dbo.patientTbl p ON p.patientId=a.PatientId OUTER APPLY(SELECT TOP 1 Detail FROM gh.WorkflowHistory WHERE EntityType='Appointment' AND EntityId=a.AppointmentId AND NewStatus='RescheduleRequested' ORDER BY HistoryId DESC)h WHERE a.Status='RescheduleRequested' ORDER BY a.ScheduledStart")))
                +Panel("Reschedule appointment",Form("reschedule",select+Field("New start (UTC)","start","datetime-local")+Field("New end (UTC)","end","datetime-local"),"Reschedule"))
                +Panel("Decline rescheduling request",Form("decline-request",Options("Pending request","appointmentId","SELECT a.AppointmentId,p.patientName+' · '+CONVERT(varchar(16),a.ScheduledStart,120) FROM gh.Appointments a JOIN dbo.patientTbl p ON p.patientId=a.PatientId WHERE a.Status='RescheduleRequested'")+Field("Reason shared with patient","reason"),"Keep original appointment"))
                +Panel("Create patient portal access",Form("access",PatientOptions()+Field("Email","email","email")+Field("Temporary password","password","password"),"Create patient access"))
                +Panel("Recent appointment history",Table(Db.Query("SELECT TOP 30 EntityId AS Appointment,PreviousStatus,NewStatus,Detail,OccurredAt FROM gh.WorkflowHistory WHERE EntityType='Appointment' ORDER BY HistoryId DESC")));
        }
        private string SelectedAppointment()
        {
            long id;if(!long.TryParse(Request.QueryString["appointmentId"],out id)||id<1)return "";
            var table=Db.Query(@"SELECT a.AppointmentId,p.patientName,p.MedicalRecordNumber,d.doctorName,a.ScheduledStart,a.ScheduledEnd,a.Status
                FROM gh.Appointments a JOIN dbo.patientTbl p ON p.patientId=a.PatientId JOIN dbo.doctorTbl d ON d.doctorId=a.DoctorId WHERE a.AppointmentId=@I",P("@I",id));
            if(table.Rows.Count==0)return Panel("Selected appointment","Appointment not found. Search again.");
            var r=table.Rows[0];var start=Convert.ToDateTime(r["ScheduledStart"]);string status=Convert.ToString(r["Status"]);
            var actions=new System.Collections.Generic.List<string>();bool booked=status=="Booked"||status=="Confirmed"||status=="RescheduleRequested";
            if(status=="Booked")actions.Add("Confirmed");if(booked&&start.Date==DateTime.UtcNow.Date)actions.Add("CheckedIn");
            if(booked&&start<DateTime.UtcNow)actions.Add("NoShow");if(booked)actions.Add("Cancelled");
            string content="<p><strong>"+H(Convert.ToString(r["patientName"]))+" · "+H(Convert.ToString(r["MedicalRecordNumber"]))+"</strong></p><p>"+H(Convert.ToString(r["doctorName"]))+" · "+start.ToString("dd MMM yyyy HH:mm")+" UTC</p>"+GuardianUi.Badge(status);
            if(status=="RescheduleRequested")content+="<p>Original time remains reserved until a new time is confirmed. Use the Rescheduling tab to accept a time or decline the request.</p>";
            if(actions.Count>0){string options="<label class='form-label'>Next action<select name='status' required class='form-select'><option value=''>Choose an action…</option>";foreach(string a in actions)options+="<option value='"+a+"'>"+H(a=="CheckedIn"?"Check in":a=="NoShow"?"Record no-show":a=="Confirmed"?"Confirm attendance":"Cancel appointment")+"</option>";options+="</select></label>";
                content+=Form("context-status","<input type='hidden' name='appointmentId' value='"+id+"'><input type='hidden' name='observedStart' value='"+HospitalTime.Token(start)+"'><input type='hidden' name='observedEnd' value='"+HospitalTime.Token(Convert.ToDateTime(r["ScheduledEnd"]))+"'><input type='hidden' name='observedStatus' value='"+H(status)+"'>"+options,"Apply appointment action");
            }else content+="<p>No reception attendance changes are available in this state. Clinical completion remains with the doctor.</p>";
            return Panel("Selected appointment",content+"<p><a href='Workspace.aspx'>Back to today's schedule</a> · <a href='Finance.aspx'>Billing and insurance</a></p>");
        }
    }
}
