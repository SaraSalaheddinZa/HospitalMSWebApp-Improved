using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Transactions;
using System.Web;
using System.Web.UI;
using System.Web.SessionState;
using System.Collections.Specialized;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
class StaffTaskExperience
{
    static Database db=new Database();static HospitalWorkflowService work=new HospitalWorkflowService();
    static System.Data.SqlClient.SqlParameter P(string n,object v){return Database.Parameter(n,v);}
    static int I(string sql,params System.Data.SqlClient.SqlParameter[] p){return Convert.ToInt32(db.Scalar(sql,p));}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    static void Reject(Action action,string message){bool rejected=false;try{action();}catch(InvalidOperationException){rejected=true;}catch(ArgumentException){rejected=true;}Check(rejected,message);}
    static string Render(string role,string page,string query,string method="Content",string form=null,bool failed=false){
        var account=db.Query("SELECT * FROM gh.UserAccounts WHERE Email LIKE 'dev.%' AND RoleName=@R",P("@R",role)).Rows[0];
        string route=(role=="Doctor"?"Doctors":role)+"."+page;
        var context=new HttpContext(new HttpRequest("","http://localhost/Views/"+route.Replace('.','/')+".aspx",query),new HttpResponse(new StringWriter()));HttpContext.Current=context;
        if(form!=null){
            // Isolated request fixture only: simulate POST fields without submitting live data.
            var values=HttpUtility.ParseQueryString(form);
            typeof(NameObjectCollectionBase).GetProperty("IsReadOnly",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(context.Request.Form,false,null);
            foreach(string key in values)context.Request.Form[key]=values[key];
        }
        var session=new HttpSessionStateContainer("qa",new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false);SessionStateUtility.AddHttpSessionStateToContext(context,session);
        session["accountId"]=account["UserId"];session["role"]=role;session["uid"]=account["LegacyUserId"];session["patientId"]=account["PatientId"];session["userName"]=account["DisplayName"];session["WorkspaceCsrf"]="qa";
        if(failed)session["WorkspaceError"]="Validation rejected";
        var type=typeof(WorkspacePage).Assembly.GetType("HospitalMSWebApp.Views."+route);var instance=Activator.CreateInstance(type);
        for(var owner=type;owner!=null;owner=owner.BaseType)foreach(var field in owner.GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly)){
            if(field.FieldType==typeof(HttpContext))field.SetValue(instance,context);if(field.FieldType==typeof(HttpRequest))field.SetValue(instance,context.Request);if(field.FieldType==typeof(HttpResponse))field.SetValue(instance,context.Response);if(field.FieldType==typeof(HttpSessionState))field.SetValue(instance,context.Session);
        }
        try{return method.StartsWith("@")?(string)type.GetProperty(method.Substring(1),BindingFlags.Instance|BindingFlags.NonPublic).GetValue(instance,null):(string)type.GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(instance,null);}finally{HttpContext.Current=null;}
    }
    static void Main(){
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        int admin=I("SELECT UserId FROM gh.UserAccounts WHERE RoleName='Admin' AND Email LIKE 'dev.%'"),reception=I("SELECT UserId FROM gh.UserAccounts WHERE RoleName='Receptionist' AND Email LIKE 'dev.%'"),lab=I("SELECT UserId FROM gh.UserAccounts WHERE RoleName='Laboratorian' AND Email LIKE 'dev.%'"),pharm=I("SELECT UserId FROM gh.UserAccounts WHERE RoleName='Pharmacist' AND Email LIKE 'dev.%'"),nurse=I("SELECT UserId FROM gh.UserAccounts WHERE RoleName='Nurse' AND Email LIKE 'dev.%'"),doctor=I("SELECT LegacyUserId FROM gh.UserAccounts WHERE Email='dev.doctor@guardianhealth.test'"),clinician=I("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.doctor@guardianhealth.test'");
        using(var fixture=new TransactionScope()){
            string code="QA-TASK-"+Guid.NewGuid().ToString("N").Substring(0,8);
            int patient=work.RegisterPatient(code,"","Other",new DateTime(1990,1,1),"QA","<script>allergy()</script>","",null,null,null,null,null,reception);
            var date=new DateTime(2095,6,14,9,0,0);work.AddDoctorSchedule(doctor,null,date.Date,TimeSpan.FromHours(9),TimeSpan.FromHours(11),20,admin);
            long appointment=work.BookAppointment(patient,doctor,date,date.AddMinutes(20),"QA",reception);
            string html=Render("Receptionist","Workspace","appointmentId="+appointment);
            Check(html.Contains(code)&&html.Contains("observedStart")&&html.Contains("observedStatus"),"Reception selected record binds patient and observed appointment state");
            Check(html.Contains("value='"+appointment+"' selected"),"Reception selectors retain the opened appointment");
            Check(!html.Contains("value='CheckedIn'")&&!html.Contains("value='InProgress'"),"Future reception task offers neither premature arrival nor clinical-start shortcut");
            work.TransitionAppointment(appointment,"Cancelled",reception);
            html=Render("Receptionist","Workspace","appointmentId="+appointment);
            Check(html.Contains("No reception attendance changes")&&!html.Contains("value='context-status'"),"Terminal appointment removes reception mutation form");
            int test=I("INSERT INTO gh.LabCatalog(TestCode,TestName,SpecimenType,ReferenceRange) VALUES(@C,@C,'QA sample','1-5');SELECT CAST(SCOPE_IDENTITY() AS int)",P("@C",code));
            int med=I("INSERT INTO gh.MedicationCatalog(MedicationCode,MedicationName) VALUES(@C,@C);SELECT CAST(SCOPE_IDENTITY() AS int)",P("@C",code));
            int otherMed=I("INSERT INTO gh.MedicationCatalog(MedicationCode,MedicationName) VALUES(@C,@C);SELECT CAST(SCOPE_IDENTITY() AS int)",P("@C",code+"-other"));
            long encounter=work.RecordEncounter(patient,doctor,null,"QA","QA assessment","QA plan",null,null,null,null,null,null,med,"1 unit","daily",2,test,clinician);
            int order=I("SELECT LabOrderId FROM gh.LabOrders WHERE EncounterId=@E",P("@E",encounter)),item=I("SELECT LabOrderItemId FROM gh.LabOrderItems WHERE LabOrderId=@O",P("@O",order));
            html=Render("Laboratorian","Workspace","item="+item);
            Check(html.Contains("Record sample collected")&&!html.Contains("name='result'"),"Ordered lab task offers collection, not premature result entry");
            Check(html.Contains("QA sample")&&html.Contains(code)&&html.Contains("1-5"),"Lab task carries patient, specimen and reference context");
            work.ProgressLab(order,"Collected",lab);html=Render("Laboratorian","Workspace","item="+item);
            Check(html.Contains("Start processing")&&!html.Contains("Record sample collected"),"Collected lab task offers exactly its next stage");
            work.ProgressLab(order,"Processing",lab);html=Render("Laboratorian","Workspace","item="+item);
            Check(html.Contains("name='result'")&&html.Contains("Finalize result and notify care team"),"Processing item opens contextual final result entry");
            work.FinalizeLabResult(item,"2","unit","Normal",lab);
            html=Render("Laboratorian","Workspace","item="+item);
            Check(!html.Contains("name='result'")&&html.Contains("Review finalized result for correction"),"Final result routes to amendment, never overwriting original entry");
            Check(!html.Contains("Workspace.aspx?item="+item+"#selected-laboratory-task'"),"Completed lab item leaves the default active worklist");
            Check(Render("Laboratorian","Workspace","stage=Completed").Contains("Workspace.aspx?item="+item+"#selected-laboratory-task'"),"Completed lab work remains accessible through explicit stage filter");
            work.ReceiveStock(med,code+"-valid",DateTime.UtcNow.AddYears(1),5,1,1,null,pharm);
            work.ReceiveStock(med,code+"-expired",DateTime.UtcNow.AddYears(2),5,1,1,null,pharm);
            db.Execute("UPDATE gh.MedicationBatches SET ExpirationDate=@D WHERE BatchNumber=@B",P("@D",DateTime.UtcNow.AddDays(-1).Date),P("@B",code+"-expired"));
            work.ReceiveStock(otherMed,code+"-wrong",DateTime.UtcNow.AddYears(1),5,1,1,null,pharm);
            int rx=I("SELECT i.PrescriptionItemId FROM gh.PrescriptionItems i JOIN gh.PrescriptionOrders o ON o.PrescriptionOrderId=i.PrescriptionOrderId WHERE o.EncounterId=@E",P("@E",encounter));
            html=Render("Pharmacist","Workspace","itemId="+rx,"DispensingTask");
            Check(html.Contains(code+"-valid")&&!html.Contains(code+"-expired")&&!html.Contains(code+"-wrong"),"Dispensing selector excludes expired and unrelated medication batches");
            Check(html.Contains("&lt;script&gt;allergy()&lt;/script&gt;")&&!html.Contains("<script>allergy()"),"Selected dispensing task safely encodes patient allergy context");
            Check(html.Contains("value='"+rx+"' selected"),"Pharmacy prescription picker retains the selected task");
            int batch=I("SELECT MedicationBatchId FROM gh.MedicationBatches WHERE BatchNumber=@B",P("@B",code+"-valid")),wrong=I("SELECT MedicationBatchId FROM gh.MedicationBatches WHERE BatchNumber=@B",P("@B",code+"-wrong"));
            work.DispensePrescriptionItem(rx,batch,2,pharm);
            Check(!Render("Pharmacist","Workspace","itemId="+rx,"DispensingTask").Contains("name='batchId'"),"Completed prescription removes dispensing form");
            Check(I("SELECT QuantityOnHand FROM gh.MedicationBatches WHERE MedicationBatchId=@B",P("@B",batch))==3&&I("SELECT SUM(Quantity) FROM gh.StockMovements WHERE MedicationBatchId=@B",P("@B",batch))==3,"Contextual dispensing preserves stock and ledger agreement");
            int ward=work.AddWard(code,"General",null,admin),room=work.AddRoom(ward,code,"General",admin),bed=work.AddBed(room,code,admin);
            long admission=work.AdmitPatient(patient,bed,doctor,"QA",admin);
            html=Render("Nurse","Workspace","patientId="+patient,"RenderAssessmentForm");
            Check(html.Contains("name='admissionId' value='"+admission+"'")&&!html.Contains("select name='admissionId'"),"Nursing assessment binds the selected patient's actual active admission");
            work.RecordNursingNote(patient,admission,nurse,"Escalated","QA assessment",37,70,120,80);
            html=Render("Nurse","Workspace","patientId="+patient,"RenderAssessmentForm");
            Check(html.Contains("Escalated")&&html.Contains("Latest recorded care status")&&html.Contains("last recorded assessment"),"Nursing context distinguishes attributed recorded care status from a new evaluation");
            work.DischargePatient(admission,"QA",admin);
            Check(!Render("Nurse","Workspace","patientId="+patient,"RenderAssessmentForm").Contains("method='post'"),"Discharged patient has history but no active-admission assessment form");
            html=Render("Admin","CommandCenter","queue=Beds&status=Cleaning");
            Check(html.Contains("value='Cleaning' selected")&&html.Contains("Status filtering applies to this snapshot"),"Admin worklist filter shows actual bed state and explicit result-limit scope");
            Check(Render("Admin","CommandCenter","queue=unknown").Contains("Unknown worklist"),"Admin worklist rejects unknown queue selection");
            Check(Render("Admin","CommandCenter","queue=Beds").Contains("Only rules with affected records"),"Command center defaults to active operational exceptions");
            // A rejected local transaction rolls back the surrounding fixture. Keep rejection last.
            Reject(()=>work.RecordNursingNote(patient,admission,nurse,"Stable","QA",null,null,null,null),"Stale nursing submission after discharge is rejected");
        }
        Check(I("SELECT COUNT(*) FROM dbo.patientTbl WHERE patientName LIKE 'QA-TASK-%'")==0,"Staff task fixtures roll back without patient residue");
        var links=new DataTable();links.Columns.Add("Key",typeof(long));links.Columns.Add("Name",typeof(string));links.Rows.Add(12,"<script>unsafe()</script>");
        string table=GuardianUi.Table(links,"Key","Workspace.aspx?item=","Open task","#selected-task");
        Check(table.Contains("class='d-inline-block'")&&table.Contains("Workspace.aspx?item=12#selected-task")&&!table.Contains(">Key</th>"),"Shared worklist link has a rectangular hit target and task fragment without a raw ID column");
        Check(table.Contains("&lt;script&gt;")&&!table.Contains("<script>unsafe"),"Worklist row actions preserve HTML encoding");
        Check(Render("Receptionist","Workspace","","@SubmissionReturnUrl","action=reschedule&appointmentId=12")=="Workspace.aspx?appointmentId=12#reschedule-appointment","Reception rejected rescheduling retains its selected appointment and task tab");
        Check(Render("Receptionist","Workspace","","@SubmissionReturnUrl","action=decline-request&appointmentId=12")=="Workspace.aspx?appointmentId=12#decline-rescheduling-request","Reception decline returns to the matching task");
        Check(Render("Laboratorian","Workspace","","@SubmissionReturnUrl","action=amend&itemId=12")=="Workspace.aspx?correctItem=12#correct-a-finalized-result","Lab correction retains selected result and corrections tab");
        Check(Render("Laboratorian","Workspace","","@SubmissionReturnUrl","action=result&selectedItem=12")=="Workspace.aspx?item=12#selected-laboratory-task","Lab result submission retains selected active task");
        Check(Render("Laboratorian","Workspace","","@SubmissionReturnUrl","action=amend&itemId=//other.example")=="Workspace.aspx","Malformed return identifiers cannot create external redirects");
        Check(Render("Admin","Workspace","","@SubmissionReturnUrl","action=setting").EndsWith("#system-configuration"),"Admin configuration feedback retains its workflow tab");
        Check(Render("Patient","Appointments","","@SubmissionReturnUrl","action=request&appointmentId=12",true)=="Appointments.aspx?appointment=12&edit=request","Rejected patient request reopens the selected request form");
        Check(Render("Patient","Appointments","","@SubmissionReturnUrl","action=request&appointmentId=12")=="Appointments.aspx?appointment=12","Successful patient request returns to details without reopening an action");
        Check(Render("Doctor","ClinicalWorkspace","","@SubmissionContext","patientId=12&appointmentId=34")=="&patientId=12&appointmentId=34#record-consultation","Doctor validation retains patient and linked appointment context");
        Check(Render("Doctor","ClinicalWorkspace","","@SubmissionContext","patientId=bad&appointmentId=-1")=="#record-consultation","Doctor return context rejects invalid identifiers");
    }
}
