using System;
using System.Data;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using HospitalMSWebApp.Services;

namespace HospitalMSWebApp.Infrastructure
{
    // Shared presentation and request validation; each workspace owns its own content and actions.
    public abstract class WorkspacePage : AppPage
    {
        protected readonly Database Db = new Database();
        protected readonly HospitalWorkflowService Workflows = new HospitalWorkflowService();
        protected abstract string Role { get; }
        protected abstract string Heading { get; }
        protected abstract string Content();
        protected abstract void Submit();
        // Derived workflows may retain a validated record selection across post/redirect/get.
        // Never redirect to an arbitrary submitted return URL.
        protected virtual string SubmissionReturnUrl { get { return Request.Path; } }
        private bool authorized;
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            authorized = RequireRoles(Role);
            if (!authorized) return;
            if (Session["WorkspaceCsrf"] == null) Session["WorkspaceCsrf"] = Guid.NewGuid().ToString("N");
            if (Request.HttpMethod != "POST") return;
            try
            {
                if (Request.Form["csrf"] != Convert.ToString(Session["WorkspaceCsrf"])) throw new ArgumentException("Security validation failed. Refresh and retry.");
                var submissions=Session["WorkspaceSubmissions"] as Dictionary<string,DateTime>;
                DateTime expires;
                var submission=Request.Form["submission"]??"";
                if(submissions==null || !submissions.TryGetValue(submission,out expires) || expires<DateTime.UtcNow)throw new ArgumentException("This form was already submitted or has expired. Please use the refreshed form.");
                submissions.Remove(submission);
                Submit();
                Session["WorkspaceNotice"] = "Changes saved.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Session["WorkspaceError"] = ex is ArgumentException || ex is InvalidOperationException ? ex.Message : "The change could not be saved. Check the selected records and try again.";
            }
            Response.Redirect(SubmissionReturnUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
        protected override void Render(HtmlTextWriter writer)
        {
            if (!authorized || Response.IsRequestBeingRedirected) return;
            var b = new StringBuilder(GuardianUi.Begin(CurrentUser,Heading,Request.Path));
            foreach (var key in new[] { "WorkspaceNotice", "WorkspaceError" })
            {
                if (Session[key] != null) b.Append("<div role='alert' class='alert ").Append(key == "WorkspaceError" ? "alert-danger" : "alert-success").Append("'>").Append(H(Convert.ToString(Session[key]))).Append("</div>");
                Session.Remove(key);
            }
            b.Append(Content()).Append(GuardianUi.End());
            writer.Write(b.ToString());
        }
        protected new string Form(string action, string fields, string button)
        {
            var submissions=Session["WorkspaceSubmissions"] as Dictionary<string,DateTime>;
            if(submissions==null){submissions=new Dictionary<string,DateTime>();Session["WorkspaceSubmissions"]=submissions;}
            if(submissions.Count>200)submissions.Clear();
            var submission=Guid.NewGuid().ToString("N");submissions[submission]=DateTime.UtcNow.AddMinutes(30);
            return "<form method='post' class='row g-3'><input type='hidden' name='submission' value='"+submission+"'><input type='hidden' name='csrf' value='" + H(Convert.ToString(Session["WorkspaceCsrf"])) + "'><input type='hidden' name='action' value='" + H(action) + "'>" + fields + "<div class='col-12'><button class='btn btn-primary'>" + H(button) + "</button></div></form>";
        }
        protected static string Panel(string title, string body) { return GuardianUi.Panel(title,body); }
        protected static string H(string s) { return HttpUtility.HtmlEncode(s ?? ""); }
        protected static string Field(string label, string name, string type = "text", bool required = true)
        {
            var id=name+"_"+Guid.NewGuid().ToString("N");
            return "<div class='col-md-4'><label class='form-label' for='" + H(id) + "'>" + H(label) + "</label><input class='form-control' id='" + H(id) + "' name='" + H(name) + "' type='" + H(type) + "' " + (type == "number" ? "step='any' " : "") + (required ? "required" : "") + "></div>";
        }
        protected string Options(string label, string name, string sql, params System.Data.SqlClient.SqlParameter[] parameters)
        {
            return SelectOptions(label,name,Db.Query(sql,parameters),true,"Choose…");
        }
        protected string OptionalOptions(string label,string name,string sql,string placeholder)
        {
            return SelectOptions(label,name,Db.Query(sql),false,placeholder);
        }
        private static string SelectOptions(string label,string name,DataTable options,bool required,string placeholder)
        {
            var b = new StringBuilder("<div class='col-md-4'><label class='form-label'>" + H(label) + "<select class='form-select' "+(required?"required ":"")+"name='" + H(name) + "'><option value=''>"+H(placeholder)+"</option>");
            foreach (DataRow row in options.Rows) b.Append("<option value='").Append(H(Convert.ToString(row[0]))).Append("'>").Append(H(Convert.ToString(row[1]))).Append("</option>");
            return b.Append("</select></label></div>").ToString();
        }
        protected static string Choice(string label, string name, params string[] choices)
        {
            bool explicitChoice=name=="flag"||name=="acuity";
            var b = new StringBuilder("<div class='col-md-4'><label class='form-label'>" + H(label) + "<select class='form-select' "+(explicitChoice?"required ":"")+"name='" + H(name) + "'>");
            if(explicitChoice)b.Append("<option value=''>Choose…</option>");
            foreach (var c in choices) b.Append("<option value='").Append(H(c)).Append("'>").Append(H(System.Text.RegularExpressions.Regex.Replace(c,"([a-z])([A-Z])","$1 $2"))).Append("</option>");
            return b.Append("</select></label></div>").ToString();
        }
        protected static string Table(DataTable table) { return GuardianUi.Table(table); }
        protected string Metric(string label, string sql, params System.Data.SqlClient.SqlParameter[] parameters)
        {
            var value=Db.Scalar(sql,parameters);
            return "<div class='col-6 col-lg-3'><div class='gh-stat'><div class='gh-stat-label'>"+H(label)+"</div><div class='gh-stat-value'>"+H(value is decimal?((decimal)value).ToString("N2",CultureInfo.InvariantCulture):Convert.ToString(value))+"</div><div class='gh-stat-note'>Live hospital data</div></div></div>";
        }
        protected string V(string key) { return (Request.Form[key] ?? "").Trim(); }
        protected string Required(string key) { var value = V(key); if (value.Length == 0) throw new ArgumentException("Enter " + key + "."); return value; }
        protected long Id(string key) { long n; if (!long.TryParse(V(key), out n) || n < 1) throw new ArgumentException("Select a valid " + key + "."); return n; }
        protected DateTime Date(string key) { DateTime d; if (!DateTime.TryParse(V(key), CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) throw new ArgumentException("Enter a valid " + key + "."); return d; }
        protected void Audit(string action, string entity, object id) { AuditService.Write(CurrentUser, action, entity, Convert.ToString(id), action, Request); }
        protected static System.Data.SqlClient.SqlParameter P(string key, object value) { return Database.Parameter(key,value); }
        protected string PatientOptions() { return Options("Patient", "patientId", "SELECT patientId,MedicalRecordNumber+' — '+patientName FROM dbo.patientTbl WHERE IsActive=1 ORDER BY patientName"); }
        // Both reception and the patient portal use the published schedule and the existing
        // availability procedure. The booking service remains the authority at submission.
        protected string AppointmentBooking(bool choosePatient)
        {
            int scheduleId;int.TryParse(Request.QueryString["scheduleId"],out scheduleId);
            var schedules=Db.Query(@"SELECT TOP 100 s.DoctorScheduleId,s.DoctorId,s.ScheduleDate,s.StartTime,s.EndTime,s.SlotMinutes,d.doctorName FROM gh.DoctorSchedules s JOIN dbo.doctorTbl d ON d.doctorID=s.DoctorId WHERE s.IsAvailable=1 AND d.doctorStatus='Active' AND s.ScheduleDate>=CAST(SYSUTCDATETIME() AS date) ORDER BY s.ScheduleDate,s.StartTime,d.doctorName");
            var b=new StringBuilder("<p class='gh-form-help'>Choose a published clinic session, then an available appointment time. Times are shown in UTC. Availability is checked again when you book.</p><form method='get' action='#book-appointment' class='row g-3 gh-slot-search'><div class='col-md-8'><label class='form-label'>Clinic session<select name='scheduleId' class='form-select' required><option value=''>Choose a clinic session…</option>");
            DataRow selected=null;
            foreach(DataRow row in schedules.Rows){
                bool match=Convert.ToInt32(row["DoctorScheduleId"])==scheduleId;if(match)selected=row;
                b.Append("<option value='").Append(row["DoctorScheduleId"]).Append(match?"' selected>":"'>").Append(H(Convert.ToString(row["doctorName"])+" · "+Convert.ToDateTime(row["ScheduleDate"]).ToString("dd MMM yyyy",CultureInfo.InvariantCulture)+" · "+((TimeSpan)row["StartTime"]).ToString(@"hh\:mm")+"–"+((TimeSpan)row["EndTime"]).ToString(@"hh\:mm")+" UTC")).Append("</option>");
            }
            b.Append("</select></label></div><div class='col-md-4 align-self-end'><button class='btn btn-outline-primary'>Find available times</button></div></form>");
            if(schedules.Rows.Count==0)return b.Append("<div class='gh-empty'><strong>No clinic sessions published</strong><p>").Append(choosePatient?"Ask hospital administration to publish clinician availability before booking.":"Contact reception for help arranging your appointment.").Append("</p></div>").ToString();
            if(selected==null)return b.ToString();
            var slots=Db.Query("EXEC gh.GetAvailableDoctorSlots @DoctorId,@ScheduleDate",P("@DoctorId",selected["DoctorId"]),P("@ScheduleDate",selected["ScheduleDate"]));
            var options=new StringBuilder("<div class='col-md-4'><label class='form-label'>Available time (UTC)<select class='form-select' name='start' required><option value=''>Choose a time…</option>");int count=0;
            foreach(DataRow slot in slots.Rows){var start=Convert.ToDateTime(slot[0]);var end=Convert.ToDateTime(slot[1]);if(start<=DateTime.UtcNow||start.TimeOfDay<(TimeSpan)selected["StartTime"]||end.TimeOfDay>(TimeSpan)selected["EndTime"])continue;count++;options.Append("<option value='").Append(start.ToString("yyyy-MM-ddTHH:mm:ss",CultureInfo.InvariantCulture)).Append("'>").Append(start.ToString("HH:mm")).Append(" – ").Append(end.ToString("HH:mm")).Append(" UTC</option>");}
            options.Append("</select></label></div>");
            if(count==0)return b.Append("<div class='gh-empty'><strong>No available times in this session</strong><p>Select another clinic session or contact reception.</p></div>").ToString();
            b.Append("<div class='gh-booking-summary'><strong>").Append(H(Convert.ToString(selected["doctorName"]))).Append("</strong><span>").Append(Convert.ToDateTime(selected["ScheduleDate"]).ToString("dd MMM yyyy",CultureInfo.InvariantCulture)).Append(" · ").Append(selected["SlotMinutes"]).Append(" minute appointments</span></div>");
            return b.Append(Form("book","<input type='hidden' name='scheduleId' value='"+scheduleId+"'>"+(choosePatient?PatientOptions():"")+options+Field("Reason for your visit","reason","text",false),"Book appointment")).ToString();
        }
        protected long BookSelectedAppointment(int patientId)
        {
            var schedule=Db.Query("SELECT DoctorId,SlotMinutes,ScheduleDate,StartTime,EndTime FROM gh.DoctorSchedules WHERE DoctorScheduleId=@Id AND IsAvailable=1",P("@Id",Id("scheduleId")));
            if(schedule.Rows.Count!=1)throw new ArgumentException("Choose a published clinic session.");
            var start=Date("start");
            var end=start.AddMinutes(Convert.ToInt32(schedule.Rows[0]["SlotMinutes"]));
            if(start.Date!=Convert.ToDateTime(schedule.Rows[0]["ScheduleDate"]).Date||start.TimeOfDay<(TimeSpan)schedule.Rows[0]["StartTime"]||end.Date!=start.Date||end.TimeOfDay>(TimeSpan)schedule.Rows[0]["EndTime"])
                throw new ArgumentException("Choose an available time within the selected clinic session.");
            return Workflows.BookAppointment(patientId,Convert.ToInt32(schedule.Rows[0]["DoctorId"]),start,end,V("reason"),CurrentUser.UserId);
        }
    }
    public static class WorkspaceNavigation
    {
        public static string Destination(string role,string module)
        {
            if(module=="search"&&role!="Patient")return "~/Views/Search/Index.aspx";
            if(role=="Admin")return module=="operations"||module=="reports"?"~/Views/Admin/Operations.aspx":"~/Views/Admin/Workspace.aspx";
            if(role=="Receptionist")return module=="billing"?"~/Views/Receptionist/Finance.aspx":"~/Views/Receptionist/Workspace.aspx";
            if(role=="Doctor")return "~/Views/Doctors/ClinicalWorkspace.aspx";
            if(role=="Nurse")return module=="operations"?"~/Views/Nurse/Emergency.aspx":"~/Views/Nurse/Workspace.aspx";
            if(role=="Pharmacist")return "~/Views/Pharmacist/Workspace.aspx";
            if(role=="Laboratorian")return "~/Views/Laboratorian/Workspace.aspx";
            if(role=="Patient")return "~/Views/Patient/Workspace.aspx";
            return "~/Views/login.aspx";
        }
    }
}
