using System;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;

namespace HospitalMSWebApp.Views.Doctors
{
    public class ClinicalWorkspace : AppPage
    {
        private readonly Database db = new Database();
        private readonly HospitalWorkflowService workflows = new HospitalWorkflowService();
        private string csrf;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("Doctor")) return;
            csrf = Convert.ToString(Session["ClinicalCsrf"]);
            if (string.IsNullOrEmpty(csrf)) { csrf = Guid.NewGuid().ToString("N"); Session["ClinicalCsrf"] = csrf; }
            if (Request.HttpMethod == "POST") Save();
        }
        private void Save()
        {
            if (Request.Form["csrf"] != csrf) { Redirect("Security validation failed.", true); return; }
            try
            {
                Session["ClinicalCsrf"]=Guid.NewGuid().ToString("N");
                var doctor = CurrentUser.RoleEntityId.GetValueOrDefault(); var patient = Int("patientId");
                if (doctor < 1 || patient < 1) throw new ArgumentException("Select a patient.");
                var id = workflows.RecordEncounter(patient, doctor, Long("appointmentId"), V("complaint"), V("assessment"), V("plan"), Date("followUp"), Decimal("temperature"), IntN("pulse"), IntN("systolic"), IntN("diastolic"), V("diagnosis"), IntN("medicationId"), V("dose"), V("frequency"), Decimal("quantity"), IntN("labTestId"));
                AuditService.Write(CurrentUser, "EncounterCreated", "Encounter", id.ToString(), "Clinical consultation saved from doctor workspace", Request);
                Redirect("Consultation saved; linked orders and notifications were created.", false);
            }
            catch (Exception ex) { Redirect(ex is ArgumentException || ex is InvalidOperationException ? ex.Message : "Consultation could not be saved.", true); }
        }
        protected override void Render(HtmlTextWriter writer)
        { if(Response.IsRequestBeingRedirected || CurrentUser==null)return;
            int doctor = CurrentUser.RoleEntityId.GetValueOrDefault(), patient; int.TryParse(Request.QueryString["patientId"], out patient);
            var b=new StringBuilder(GuardianUi.Begin(CurrentUser,"Clinical workspace",Request.Path));
            Notice(b); if(patient>0)b.Append(Timeline(patient)); b.Append("<div class='row'>").Append(GuardianUi.Stat("Today's appointments",db.Scalar("SELECT COUNT(*) FROM gh.Appointments WHERE DoctorId=@D AND CAST(ScheduledStart AS date)=CAST(SYSUTCDATETIME() AS date)",Database.Parameter("@D",doctor)))).Append(GuardianUi.Stat("Waiting patients",db.Scalar("SELECT COUNT(*) FROM gh.Appointments WHERE DoctorId=@D AND Status='CheckedIn'",Database.Parameter("@D",doctor)))).Append(GuardianUi.Stat("Results to review",db.Scalar("SELECT COUNT(*) FROM gh.LabOrders WHERE OrderedByDoctorId=@D AND Status='Completed' AND ReviewedAt IS NULL",Database.Parameter("@D",doctor)))).Append(GuardianUi.Stat("Open consultations",db.Scalar("SELECT COUNT(*) FROM gh.Encounters WHERE DoctorId=@D AND Status='Open'",Database.Parameter("@D",doctor)))).Append("</div>"); b.Append("<section class='panel'><h2>My consultation queue</h2>").Append(RenderQueue(doctor)).Append("</section>");
            b.Append("<section class='panel'><h2>Patient record</h2><form method='get' class='form-grid'>").Append(Options("Patient","patientId","SELECT patientId AS Id,MedicalRecordNumber+' — '+patientName AS Name FROM dbo.patientTbl WHERE IsActive=1",true)).Append("<button>Open record</button></form></section>");if(patient>0)b.Append(MedicalTimeline(patient)); b.Append(RenderConsultationForm(doctor)); b.Append(GuardianUi.End()); writer.Write(b.ToString());
        }
        private string Timeline(int p)
        {
            var summary=GuardianUi.PatientSummary(db.Query("SELECT p.MedicalRecordNumber AS MRN,p.patientName AS Patient,p.patientDOB AS BirthDate,p.patientAllergies AS Allergies,pp.BloodType,pp.ChronicConditions FROM dbo.patientTbl p LEFT JOIN gh.PatientProfiles pp ON pp.PatientId=p.patientId WHERE p.patientId=@P",Database.Parameter("@P",p)));
            return summary;
        }
        private string MedicalTimeline(int patient)
        {
            return GuardianUi.Panel("Medical timeline",Table(db.Query("SELECT TOP 50 EventAt,EventType AS Event,Detail FROM gh.vwPatientTimeline WHERE PatientId=@P ORDER BY EventAt DESC",Database.Parameter("@P",patient))));
        }
        private string RenderQueue(int doctor)
        {
            var queue=db.Query(@"SELECT TOP 30 a.AppointmentId,p.patientId,p.MedicalRecordNumber,p.patientName,a.ScheduledStart,a.Status FROM gh.Appointments a JOIN dbo.patientTbl p ON p.patientId=a.PatientId WHERE a.DoctorId=@D AND a.Status IN ('Booked','Confirmed','RescheduleRequested','CheckedIn','InProgress') ORDER BY a.ScheduledStart",Database.Parameter("@D",doctor));
            if(queue.Rows.Count==0)return GuardianUi.Table(queue);
            var b=new StringBuilder("<div class='gh-table-wrap' tabindex='0' role='region' aria-label='Consultation queue'><table class='gh-table'><thead><tr><th scope='col'>Patient / MRN</th><th scope='col'>Appointment (UTC)</th><th scope='col'>Status</th><th scope='col'>Next action</th></tr></thead><tbody>");
            foreach(DataRow row in queue.Rows)
            {
                string patient=Convert.ToString(row["patientId"],System.Globalization.CultureInfo.InvariantCulture),appointment=Convert.ToString(row["AppointmentId"],System.Globalization.CultureInfo.InvariantCulture);
                bool arrived=Convert.ToString(row["Status"])=="CheckedIn"||Convert.ToString(row["Status"])=="InProgress";
                b.Append("<tr><td><strong>").Append(H(Convert.ToString(row["patientName"]))).Append("</strong><small>").Append(H(Convert.ToString(row["MedicalRecordNumber"]))).Append("</small></td><td>").Append(H(Convert.ToDateTime(row["ScheduledStart"]).ToString("dd MMM yyyy · HH:mm",System.Globalization.CultureInfo.InvariantCulture))).Append("</td><td>").Append(GuardianUi.Badge(Convert.ToString(row["Status"]))).Append("</td><td>");
                if(arrived)b.Append("<a class='btn btn-outline-primary' href='ClinicalWorkspace.aspx?patientId=").Append(H(patient)).Append("&amp;appointmentId=").Append(H(appointment)).Append("#record-consultation'>Open consultation</a>");
                else {if(Convert.ToString(row["Status"])=="RescheduleRequested")b.Append("New time requested; original appointment reserved · ");b.Append("Awaiting reception check-in · <a href='../Records/Index.aspx?patientId=").Append(H(patient)).Append("'>Review record</a>");}
                b.Append("</td></tr>");
            }
            return b.Append("</tbody></table></div>").ToString();
        }
        private string RenderConsultationForm(int doctor)
        {
            return "<section class='panel form-panel'><h2>Record consultation</h2><p class='gh-form-help'>Document your assessment, then add observations and orders. Required clinical details are checked before saving.</p><form method='post' class='form-grid three'><input type='hidden' name='csrf' value='"+H(csrf)+"'>"
                +Options("Patient","patientId","SELECT patientId AS Id,MedicalRecordNumber+' — '+patientName AS Name FROM dbo.patientTbl WHERE IsActive=1 ORDER BY patientName",true)
                +Options("Appointment","appointmentId","SELECT a.AppointmentId AS Id,p.patientName+' — '+CONVERT(varchar(16),a.ScheduledStart,120) AS Name FROM gh.Appointments a JOIN dbo.patientTbl p ON p.patientId=a.PatientId WHERE a.DoctorId="+doctor+" AND a.Status IN ('CheckedIn','InProgress')",false)
                +Input("Chief complaint","complaint",true)
                +"<fieldset class='gh-fieldset'><legend>Assessment & care plan</legend><div class='form-grid'>"+Area("Assessment","assessment")+Area("Treatment plan","plan")+Input("Primary diagnosis","diagnosis",false)+Input("Follow-up date","followUp",false,"date")+"</div></fieldset>"
                +"<fieldset class='gh-fieldset'><legend>Vital signs</legend><div class='form-grid'>"+Input("Temperature °C","temperature",false,"number")+Input("Pulse / minute","pulse",false,"number")+Input("Systolic BP (mmHg)","systolic",false,"number")+Input("Diastolic BP (mmHg)","diastolic",false,"number")+"</div></fieldset>"
                +"<fieldset class='gh-fieldset'><legend>Medication & laboratory orders</legend><div class='form-grid'>"+Options("Medication","medicationId","SELECT MedicationId AS Id,MedicationName AS Name FROM gh.MedicationCatalog WHERE IsActive=1 ORDER BY MedicationName",false)+Input("Dose","dose",false)+Input("Frequency","frequency",false)+Input("Quantity","quantity",false,"number")+Options("Laboratory test","labTestId","SELECT LabTestId AS Id,TestName AS Name FROM gh.LabCatalog WHERE IsActive=1 ORDER BY TestName",false)+"</div></fieldset><button>Save consultation</button></form></section>";
        }
        private void Notice(StringBuilder b){var n=Request.QueryString["notice"];var e=Request.QueryString["error"];if(!string.IsNullOrEmpty(n))b.Append("<div class='notice success'>✓ ").Append(H(n)).Append("</div>");if(!string.IsNullOrEmpty(e))b.Append("<div class='notice error'>! ").Append(H(e)).Append("</div>");}
        private string Options(string l,string n,string q,bool r){var t=db.Query(q);var b=new StringBuilder("<label>"+H(l)+"<select name='"+H(n)+"'"+(r?" required":"")+"><option value=''>Select</option>");foreach(DataRow x in t.Rows)b.Append("<option value='").Append(H(Convert.ToString(x["Id"]))).Append((n=="patientId"||n=="appointmentId")&&Convert.ToString(x["Id"])==Request.QueryString[n]?"' selected>":"'>").Append(H(Convert.ToString(x["Name"]))).Append("</option>");return b.Append("</select></label>").ToString();} private static string Input(string l,string n,bool r,string t="text"){return "<label>"+H(l)+"<input name='"+H(n)+"' type='"+H(t)+"'"+(t=="number"?" step='any'":"")+""+(r?" required":"")+"></label>";} private static string Area(string l,string n){return "<label>"+H(l)+"<textarea name='"+H(n)+"'></textarea></label>";} private static string H(string x){return HttpUtility.HtmlEncode(x??"");}
        static string Table(DataTable t){return GuardianUi.Table(t);}
        private string V(string k){return (Request.Form[k]??"").Trim();} private int Int(string k){var n=IntN(k);return n.GetValueOrDefault();} private int? IntN(string k){int n;if(V(k)=="")return null;if(!int.TryParse(V(k),out n))throw new ArgumentException("Enter a valid "+k+".");return n;} private long? Long(string k){long n;if(V(k)=="")return null;if(!long.TryParse(V(k),out n))throw new ArgumentException("Select a valid "+k+".");return n;} private decimal? Decimal(string k){decimal n;if(V(k)=="")return null;if(!decimal.TryParse(V(k),System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out n))throw new ArgumentException("Enter a valid "+k+".");return n;} private DateTime? Date(string k){DateTime d;if(V(k)=="")return null;if(!DateTime.TryParse(V(k),out d))throw new ArgumentException("Enter a valid follow-up date.");return d;} private string SubmissionContext { get { int patient;long appointment;return (int.TryParse(V("patientId"),out patient)&&patient>0?"&patientId="+patient:"")+(long.TryParse(V("appointmentId"),out appointment)&&appointment>0?"&appointmentId="+appointment:"")+"#record-consultation"; } }
        private void Redirect(string m,bool bad){Response.Redirect("ClinicalWorkspace.aspx?"+(bad?"error":"notice")+"="+HttpUtility.UrlEncode(m)+SubmissionContext,false);Context.ApplicationInstance.CompleteRequest();}
    }
}
