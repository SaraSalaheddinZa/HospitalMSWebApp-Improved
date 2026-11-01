using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Transactions;
using System.Web;
using System.Web.SessionState;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
class PatientExperience
{
    static Database db=new Database();static HospitalWorkflowService work=new HospitalWorkflowService();
    static int admin,reception,portal,patient,doctor;static DateTime date=new DateTime(2096,7,10,9,0,0);
    static SqlParameter P(string name,object value){return Database.Parameter(name,value);}
    static int I(string sql,params SqlParameter[] p){return Convert.ToInt32(db.Scalar(sql,p));}
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    static void Reject(Action action,string message){bool fail=false;try{action();}catch(ArgumentException){fail=true;}catch(InvalidOperationException){fail=true;}Assert(fail,message);}
    static string DoctorQueue(){var type=typeof(WorkspacePage).Assembly.GetType("HospitalMSWebApp.Views.Doctors.ClinicalWorkspace");return HttpUtility.HtmlDecode((string)type.GetMethod("RenderQueue",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(Activator.CreateInstance(type),new object[]{doctor}));}
    static string Render(string route,string query)
    {
        var account=db.Query("SELECT * FROM gh.UserAccounts WHERE UserId=@U",P("@U",portal)).Rows[0];
        var context=new HttpContext(new HttpRequest("","http://localhost/Views/Patient/"+route+".aspx",query),new HttpResponse(new StringWriter()));HttpContext.Current=context;
        var state=new HttpSessionStateContainer("qa",new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false);
        SessionStateUtility.AddHttpSessionStateToContext(context,state);
        state["accountId"]=portal;state["role"]="Patient";state["patientId"]=patient;state["userName"]=account["DisplayName"];state["WorkspaceCsrf"]="qa";
        var type=typeof(WorkspacePage).Assembly.GetType("HospitalMSWebApp.Views.Patient."+route);var page=Activator.CreateInstance(type);
        for(var owner=type;owner!=null;owner=owner.BaseType)foreach(var field in owner.GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly)){
            if(field.FieldType==typeof(HttpContext))field.SetValue(page,context);if(field.FieldType==typeof(HttpRequest))field.SetValue(page,context.Request);if(field.FieldType==typeof(HttpResponse))field.SetValue(page,context.Response);if(field.FieldType==typeof(HttpSessionState))field.SetValue(page,context.Session);
        }
        try{return (string)type.GetMethod("Content",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(page,null);}finally{HttpContext.Current=null;}
    }
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        admin=I("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.admin@guardianhealth.test'");reception=I("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.reception@guardianhealth.test'");portal=I("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.patient@guardianhealth.test'");patient=I("SELECT PatientId FROM gh.UserAccounts WHERE UserId=@U",P("@U",portal));doctor=I("SELECT LegacyUserId FROM gh.UserAccounts WHERE Email='dev.doctor@guardianhealth.test'");
        var east=TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");var dst=TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        Assert(HospitalTime.Local(new DateTime(2026,10,16,22,0,0),east)==new DateTime(2026,10,17,1,0,0),"Hospital-local display crosses UTC midnight without changing storage");
        Assert(HospitalTime.ToUtc(new DateTime(2026,10,17,1,0,0),east)==new DateTime(2026,10,16,22,0,0),"Local input normalizes to its actual UTC instant");
        Assert(HospitalTime.Format(new DateTime(2026,7,1,12,0,0),dst).Contains("UTC-04:00")&&HospitalTime.Format(new DateTime(2026,1,1,12,0,0),dst).Contains("UTC-05:00"),"Display offset follows DST for the appointment date");
        Reject(()=>HospitalTime.ToUtc(new DateTime(2026,3,8,2,30,0),dst),"Nonexistent spring-forward input is rejected");
        Reject(()=>HospitalTime.ToUtc(new DateTime(2026,11,1,1,30,0),dst),"Ambiguous fall-back input is rejected rather than guessed");
        var culture=System.Threading.Thread.CurrentThread.CurrentCulture;
        try{System.Threading.Thread.CurrentThread.CurrentCulture=new CultureInfo("ar-SA");Assert(HospitalTime.ParseUtc(HospitalTime.Token(date))==date,"Canonical slot token ignores browser/server display culture");Assert(HospitalTime.ParseLocal("2096-07-10T09:00")==HospitalTime.ToUtc(date,HospitalTime.Zone),"Local date input uses explicit ISO format");}finally{System.Threading.Thread.CurrentThread.CurrentCulture=culture;}
        Reject(()=>HospitalTime.ParseUtc("07/10/2096 09:00"),"Ambiguous localized slot token is rejected");Reject(()=>HospitalTime.ParseLocal("07/10/2096 09:00"),"Ambiguous localized request input is rejected");
        using(var fixture=new TransactionScope()){
            int schedule=work.AddDoctorSchedule(doctor,null,date.Date,TimeSpan.FromHours(9),TimeSpan.FromHours(11),20,admin);
            string review=Render("MedicalTeam","doctor="+doctor+"&date=2096-07-10&schedule="+schedule+"&slot="+HospitalTime.Token(date));
            Assert(review.Contains("Review your appointment")&&review.Contains("Confirm appointment")&&review.Contains("20 minutes"),"Booking review shows actual duration and explicit confirmation");
            Assert(!review.Contains("Choose this time"),"Review is a separate task, not many competing booking forms");
            long id=work.BookMedicalTeamSlot(schedule,date,"<script>unsafe()</script>",portal);
            var detail=work.PatientAppointments(portal,id);Assert(detail.Rows.Count==1&&Convert.ToInt32(detail.Rows[0]["AppointmentId"])==id,"Owner can read persisted appointment details");
            Assert(work.PatientAppointmentHistory(id,portal).Rows.Count==1,"Appointment history includes real booking event");
            string html=Render("Appointments","appointment="+id+"&edit=cancel");
            Assert(html.Contains("Confirm cancellation")&&html.Contains("Keep appointment"),"Cancellation requires its own appointment-specific confirmation");
            Assert(html.Contains("observedStart")&&html.Contains("observedEnd")&&html.Contains("observedStatus"),"Action form binds observed appointment time and status");
            Assert(html.Contains("&lt;script&gt;unsafe()&lt;/script&gt;")&&!html.Contains("<script>unsafe()</script>"),"Appointment reason is HTML encoded");
            Assert(Render("MedicalTeam","doctor="+doctor+"&date=2096-07-10&schedule="+schedule+"&slot="+HospitalTime.Token(date)).Contains("Time no longer available"),"Stale booking review cannot offer an occupied time");
            Assert(Render("Appointments","appointment=9223372036854775807").Contains("Appointment unavailable"),"Unknown appointment produces a generic recovery state");
            work.TransitionAppointment(id,"RescheduleRequested",portal,null,date.AddHours(1),date.AddHours(1).AddMinutes(20),"QA window",date,"Booked",date.AddMinutes(20));
            Assert(Convert.ToDateTime(work.PatientAppointments(portal,id).Rows[0]["ScheduledStart"])==date,"Rescheduling request retains original reservation");
            Assert(Render("Appointments","appointment="+id).Contains("Rescheduling request pending"),"Patient sees explicit pending-request state");
            var clinical=typeof(WorkspacePage).Assembly.GetType("HospitalMSWebApp.Views.Doctors.ClinicalWorkspace");
            string queue=(string)clinical.GetMethod("RenderQueue",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(Activator.CreateInstance(clinical),new object[]{doctor});
            Assert(queue.Contains("New time requested; original appointment reserved"),"Doctor queue retains appointments awaiting a rescheduling decision");
            work.DeclineRescheduleRequest(id,"Requested time unavailable",reception);
            Assert(Render("Appointments","appointment="+id).Contains("Reschedule declined: Requested time unavailable"),"Patient can see recorded request outcome");
            Assert(DoctorQueue().Contains("10 Jul 2096 · 09:00"),"Declined request retains the original clinician queue reservation");
            work.TransitionAppointment(id,"Cancelled",portal,null,null,null,null,date,"Booked",date.AddMinutes(20));
            Assert(!Render("Appointments","appointment="+id).Contains("edit=cancel"),"Cancelled appointment cannot offer another cancellation");
            Assert(Render("Appointments","view=past").Contains("appointment="+id+"'"),"Cancelled appointment appears in history");
            Assert(!Render("Appointments","").Contains("appointment="+id+"'"),"Cancelled appointment is absent from upcoming list");
            Assert(!DoctorQueue().Contains("10 Jul 2096 · 09:00"),"Cancelled appointment leaves the clinician queue");
            Assert(I("SELECT COUNT(*) FROM gh.Notifications WHERE PatientId=@P AND NotificationType='Appointment' AND CreatedAt>=DATEADD(minute,-1,SYSUTCDATETIME())",P("@P",patient))>0,"Appointment lifecycle continues to create real inbox events");
        }
        using(var fixture=new TransactionScope()){
            int schedule=work.AddDoctorSchedule(doctor,null,date.Date,TimeSpan.FromHours(9),TimeSpan.FromHours(11),20,admin);
            long id=work.BookMedicalTeamSlot(schedule,date,"QA approved request",portal);
            work.TransitionAppointment(id,"RescheduleRequested",portal,null,date.AddMinutes(20),date.AddMinutes(40),"QA requested time");
            work.RescheduleAppointment(id,date.AddMinutes(20),date.AddMinutes(40),reception);
            var row=work.PatientAppointments(portal,id).Rows[0];
            Assert(Convert.ToDateTime(row["ScheduledStart"])==date.AddMinutes(20)&&Convert.ToString(row["Status"])=="Booked","Approved request persists new reserved time and clears pending status");
            Assert(DoctorQueue().Contains("10 Jul 2096 · 09:20")&&!DoctorQueue().Contains("10 Jul 2096 · 09:00"),"Clinician queue moves to accepted time without a duplicate reservation");
            Assert(Render("Appointments","appointment="+id).Contains("Rescheduled to"),"Patient history exposes real accepted request outcome");
            // Move only this rolled-back fixture to its attendance day; do not change the server clock.
            db.Execute("UPDATE gh.Appointments SET ScheduledStart=@S,ScheduledEnd=@E WHERE AppointmentId=@I",P("@S",DateTime.UtcNow.Date.AddHours(9)),P("@E",DateTime.UtcNow.Date.AddHours(9).AddMinutes(20)),P("@I",id));
            work.TransitionAppointment(id,"CheckedIn",reception);
            int clinician=I("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.doctor@guardianhealth.test'");
            long encounter=work.RecordEncounter(patient,doctor,id,"QA consultation","QA assessment","QA plan",null,null,null,null,null,null,null,null,null,null,null,clinician);
            Assert(Convert.ToString(work.PatientAppointments(portal,id).Rows[0]["Status"])=="Completed","Previously rescheduled visit completes through a checked-in consultation");
            Assert(!DoctorQueue().Contains("appointmentId="+id+"#record-consultation"),"Completed consultation leaves the actionable clinician queue");
            Reject(()=>work.RescheduleAppointment(id,date.AddMinutes(40),date.AddHours(1),reception),"Completed appointment cannot be rescheduled again");
        }
        foreach(string scenario in new[]{"stale-time","stale-status","stale-end","replay","foreign-read","foreign-history","foreign-write","staff-read","staff-history"})using(var fixture=new TransactionScope()){
            int schedule=work.AddDoctorSchedule(doctor,null,date.Date,TimeSpan.FromHours(9),TimeSpan.FromHours(11),20,admin);long id=work.BookMedicalTeamSlot(schedule,date,"QA",portal);
            if(scenario.StartsWith("foreign")){
                int other=work.RegisterPatient("QA-UX-"+Guid.NewGuid().ToString("N").Substring(0,8),"","Other",new DateTime(1990,1,1),"QA","","",null,null,null,null,null,reception);
                long otherId=work.BookAppointment(other,doctor,date.AddMinutes(20),date.AddMinutes(40),"QA",reception);
                if(scenario=="foreign-read")Assert(work.PatientAppointments(portal,otherId).Rows.Count==0,"Other patient's appointment is not exposed");
                else if(scenario=="foreign-history")Assert(work.PatientAppointmentHistory(otherId,portal).Rows.Count==0,"Other patient's history is not exposed");
                else Reject(()=>work.TransitionAppointment(otherId,"Cancelled",portal,other),"Supplied patient ID cannot bypass actual account ownership");
            }else if(scenario=="staff-read")Reject(()=>work.PatientAppointments(reception),"Staff role cannot call patient-only appointment read service");
            else if(scenario=="staff-history")Reject(()=>work.PatientAppointmentHistory(id,reception),"Staff role cannot call patient-only history service");
            else{
                if(scenario=="stale-time")work.RescheduleAppointment(id,date.AddMinutes(20),date.AddMinutes(40),reception);
                if(scenario=="stale-status")work.TransitionAppointment(id,"Confirmed",reception);
                if(scenario=="stale-end")work.RescheduleAppointment(id,date,date.AddMinutes(40),reception);
                if(scenario=="replay")work.TransitionAppointment(id,"Cancelled",portal);
                Reject(()=>work.TransitionAppointment(id,"Cancelled",portal,null,null,null,null,date,"Booked",date.AddMinutes(20)),"Appointment mutation rejects "+scenario);
            }
        }
        using(var fixture=new TransactionScope()){
            work.AddDoctorSchedule(doctor,null,date.Date,TimeSpan.FromHours(22),TimeSpan.FromHours(23),20,admin);
            Assert(work.GetMedicalTeamSlots(doctor,date.Date.AddDays(1),portal,east).Rows.Count==3,"Local-day availability includes previous UTC day's evening slots");
            Assert(work.FindMedicalTeam(portal,date:date.Date.AddDays(1),doctor:doctor,displayZone:east).Rows.Count==1,"Date-filtered discovery and local-day slot picker agree");
            Assert(work.GetMedicalTeamSlots(doctor,date.Date,portal,east).Rows.Count==0,"Local-day availability excludes slots belonging to the following local date");
        }
        Assert(I("SELECT COUNT(*) FROM dbo.patientTbl WHERE patientName LIKE 'QA-UX-%'")==0,"Patient experience fixtures are rolled back");
    }
}
