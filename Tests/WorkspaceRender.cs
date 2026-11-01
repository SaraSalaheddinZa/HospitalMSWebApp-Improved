using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.SessionState;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
class WorkspaceRender
{
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        var db=new Database();
        foreach(var route in new[]{"Receptionist.Workspace","Laboratorian.Workspace","Admin.Workspace","Patient.Workspace","Patient.MedicalTeam","Patient.Appointments","Admin.Operations","Admin.Reports","Admin.CommandCenter","Admin.AuditReview","Receptionist.Finance","Pharmacist.Suppliers","Documents.Index","Records.Index","Search.Index","Notifications.Index","Nurse.Emergency","Doctors.Notes","Doctors.Results"})
        using(var fixture=new TransactionScope())
        {
            var role=route.Split('.')[0];if(role=="Documents"||role=="Records"||role=="Notifications")role="Patient";if(role=="Search")role="Receptionist";if(role=="Doctors")role="Doctor";
            var account=db.Query("SELECT UserId,LegacyUserId,PatientId,DisplayName,Email FROM gh.UserAccounts WHERE RoleName=@Role AND Email LIKE 'dev.%'",Database.Parameter("@Role",role)).Rows[0];
            string query="";
            long pastAppointment=0,futureAppointment=0;
            if(route=="Admin.Workspace"){
                string code="QA-"+Guid.NewGuid().ToString("N").Substring(0,12);
                int staff=new HospitalMSWebApp.Services.HospitalWorkflowService().CreateStaffAccount(code,"Nurse","<script>profile()</script>",code+"@example.test","",null,"FixtureProfile123!",Convert.ToInt32(account["UserId"]));
                query="staffUser="+staff;
            }
            if(route=="Laboratorian.Workspace"||route=="Doctors.Results"){
                var service=new HospitalMSWebApp.Services.HospitalWorkflowService();
                var clinician=db.Query("SELECT UserId,LegacyUserId FROM gh.UserAccounts WHERE Email='dev.doctor@guardianhealth.test'").Rows[0];
                int patient=Convert.ToInt32(db.Scalar("SELECT PatientId FROM gh.UserAccounts WHERE Email='dev.patient@guardianhealth.test'")),lab=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.lab@guardianhealth.test'"));
                string code="QA-"+Guid.NewGuid().ToString("N").Substring(0,12);
                int test=Convert.ToInt32(db.Scalar("INSERT INTO gh.LabCatalog(TestCode,TestName) VALUES(@C,@C);SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@C",code)));
                long encounter=service.RecordEncounter(patient,Convert.ToInt32(clinician["LegacyUserId"]),null,"QA render","QA assessment","QA plan",null,null,null,null,null,null,null,null,null,null,test,Convert.ToInt32(clinician["UserId"]));
                long order=Convert.ToInt64(db.Scalar("SELECT LabOrderId FROM gh.LabOrders WHERE EncounterId=@E",Database.Parameter("@E",encounter))),item=Convert.ToInt64(db.Scalar("SELECT LabOrderItemId FROM gh.LabOrderItems WHERE LabOrderId=@O",Database.Parameter("@O",order)));
                service.ProgressLab(order,"Collected",lab);service.ProgressLab(order,"Processing",lab);service.FinalizeLabResult(item,"1","unit","Normal",lab);
                service.AmendLabResult(item,0,"<script>bad()</script>","unit","1-5","High","QA amendment",lab);
                query=route=="Laboratorian.Workspace"?"correctItem="+item:"reviewOrder="+order;
            }
            if(route=="Pharmacist.Suppliers"){
                string code="QA-"+Guid.NewGuid().ToString("N").Substring(0,12);
                int medication=Convert.ToInt32(db.Scalar("INSERT INTO gh.MedicationCatalog(MedicationCode,MedicationName) VALUES(@C,@C); SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@C",code)));
                new HospitalMSWebApp.Services.HospitalWorkflowService().ReceiveStock(medication,code,DateTime.UtcNow.AddYears(1),10,0,1,null,Convert.ToInt32(account["UserId"]));
                query="countBatch="+Convert.ToString(db.Scalar("SELECT MedicationBatchId FROM gh.MedicationBatches WHERE MedicationId=@M",Database.Parameter("@M",medication)));
            }
            if(route=="Patient.Workspace"||route=="Patient.Appointments"){
                int doctor=Convert.ToInt32(db.Scalar("SELECT LegacyUserId FROM gh.UserAccounts WHERE Email='dev.doctor@guardianhealth.test'"));
                foreach(int days in new[]{-2,2}){
                    long id=Convert.ToInt64(db.Scalar("INSERT INTO gh.Appointments(PatientId,DoctorId,ScheduledStart,ScheduledEnd,Status) VALUES(@P,@D,@S,@E,'Booked'); SELECT CAST(SCOPE_IDENTITY() AS bigint)",Database.Parameter("@P",account["PatientId"]),Database.Parameter("@D",doctor),Database.Parameter("@S",DateTime.UtcNow.AddDays(days)),Database.Parameter("@E",DateTime.UtcNow.AddDays(days).AddMinutes(20))));
                    if(days<0)pastAppointment=id;else futureAppointment=id;
                }
            }
            if(route=="Doctors.Notes"){
                int patient=Convert.ToInt32(db.Scalar("SELECT PatientId FROM gh.UserAccounts WHERE Email='dev.patient@guardianhealth.test'"));
                var id=new HospitalMSWebApp.Services.HospitalWorkflowService().RecordEncounter(patient,Convert.ToInt32(account["LegacyUserId"]),null,"QA render","<script>alert('encoded')</script>","QA plan",null,null,null,null,null,null,null,null,null,null,null,Convert.ToInt32(account["UserId"]));
                query="edit="+id;
            }
            if(route=="Patient.Workspace"||route=="Receptionist.Workspace")query="scheduleId="+Convert.ToString(db.Scalar("SELECT TOP 1 DoctorScheduleId FROM gh.DoctorSchedules WHERE IsAvailable=1 AND ScheduleDate>=CAST(SYSUTCDATETIME() AS date) ORDER BY ScheduleDate"));
            var context=new HttpContext(new HttpRequest("","http://localhost/Views/"+role+"/Workspace.aspx",query),new HttpResponse(new StringWriter()));
            HttpContext.Current=context;
            var session=new HttpSessionStateContainer("qa",new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false);
            SessionStateUtility.AddHttpSessionStateToContext(context,session);
            session["accountId"]=account["UserId"];session["role"]=role;session["uid"]=account["LegacyUserId"];session["patientId"]=account["PatientId"];session["userName"]=account["DisplayName"];session["userEmail"]=account["Email"];session["WorkspaceCsrf"]="qa";
            var type=typeof(WorkspacePage).Assembly.GetType("HospitalMSWebApp.Views."+route);
            var page=Activator.CreateInstance(type);
            for(var owner=type;owner!=null;owner=owner.BaseType)
            foreach(var field in owner.GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
            {
                if(field.FieldType==typeof(HttpContext))field.SetValue(page,context);
                if(field.FieldType==typeof(HttpRequest))field.SetValue(page,context.Request);
                if(field.FieldType==typeof(HttpResponse))field.SetValue(page,context.Response);
                if(field.FieldType==typeof(HttpSessionState))field.SetValue(page,context.Session);
            }
            string html=(string)type.GetMethod("Content",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(page,null);
            if(html.Length<100)throw new Exception("Workspace returned no content.");
            if(route=="Doctors.Notes"&&(!html.Contains("Save draft")||!html.Contains("&lt;script&gt;")||html.Contains("<script>alert('encoded')</script>")))throw new Exception("Draft editor did not render safely encoded existing clinical data.");
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(html,"\\bid=['\"]([^'\"]+)['\"]"))if(!ids.Add(match.Groups[1].Value))throw new Exception("Duplicate rendered element ID: "+match.Groups[1].Value);
            if(route=="Receptionist.Workspace"&&!html.Contains("Find available times"))throw new Exception("Published-slot reception booking interface is missing.");
            if(route=="Patient.Workspace"&&(!html.Contains("Next appointment")||html.Contains("Clinic session")||html.Contains("name='appointmentId'")))throw new Exception("Patient dashboard must summarize care without duplicate booking/change forms.");
            if(route=="Patient.Appointments"&&(html.Contains("appointment="+pastAppointment+"'")||!html.Contains("appointment="+futureAppointment+"'")))throw new Exception("Upcoming list must exclude past appointments and include future appointments.");
            if(route=="Records.Index"&&!html.Contains("action='#timeline'"))throw new Exception("Timeline filtering must retain the timeline tab.");
            if(route=="Pharmacist.Suppliers"&&(!html.Contains("name='movementVersion'")||!html.Contains("name='observedQuantity'")||!html.Contains("Recount if stock moves")))throw new Exception("Reconciliation must submit the observed batch/ledger version.");
            if((route=="Laboratorian.Workspace"||route=="Doctors.Results")&&(!html.Contains("&lt;script&gt;bad()&lt;/script&gt;")||html.Contains("<script>bad()</script>")))throw new Exception("Corrected clinical values must be safely encoded.");
            if(route=="Laboratorian.Workspace"&&!html.Contains("name='revision'"))throw new Exception("Correction form must bind the observed revision.");
            if(route=="Doctors.Results"&&!html.Contains("name='resultVersion'"))throw new Exception("Review form must bind the displayed results version.");
            if(route=="Admin.Workspace"&&(!html.Contains("&lt;script&gt;profile()&lt;/script&gt;")||html.Contains("<script>profile()</script>")||!html.Contains("name='profileVersion'")||!html.Contains("Save staff profile")))throw new Exception("Staff profile editor must safely render selected details and submit an observed revision.");
            Console.WriteLine("PASS: "+route+" renders database-backed content ("+html.Length+" characters)");
            HttpContext.Current=null;
        }
    }
}
