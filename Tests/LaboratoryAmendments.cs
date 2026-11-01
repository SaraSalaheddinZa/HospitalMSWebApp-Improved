using System;
using System.Data;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
class LaboratoryAmendments
{
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    static int Version(Database db,long order){return Convert.ToInt32(db.Scalar("SELECT ResultVersion FROM gh.LabOrders WHERE LabOrderId=@O",Database.Parameter("@O",order)));}
    static int PatientRows(DataTable table,int patient){string mrn=Convert.ToString(new Database().Scalar("SELECT MedicalRecordNumber FROM dbo.patientTbl WHERE patientId=@P",Database.Parameter("@P",patient)));int count=0;foreach(DataRow row in table.Rows)if(Convert.ToString(row["MRN"])==mrn)count++;return count;}
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        var db=new Database();var service=new HospitalWorkflowService();
        int lab=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.lab@guardianhealth.test'"));
        int reception=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.reception@guardianhealth.test'"));
        int admin=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.admin@guardianhealth.test'"));
        var doctorRow=db.Query("SELECT UserId,LegacyUserId FROM gh.UserAccounts WHERE Email='dev.doctor@guardianhealth.test'").Rows[0];
        int clinician=Convert.ToInt32(doctorRow["UserId"]),doctor=Convert.ToInt32(doctorRow["LegacyUserId"]);
        var patientRow=db.Query("SELECT UserId,PatientId FROM gh.UserAccounts WHERE Email='dev.patient@guardianhealth.test'").Rows[0];
        int patientActor=Convert.ToInt32(patientRow["UserId"]),patient=Convert.ToInt32(patientRow["PatientId"]);
        int before=Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.LabResultAmendments"));
        foreach(string scenario in new[]{"valid","unfinished","cancelled","amend-role","snapshot-role","stale-revision","replayed-amendment","review-stale","review-role","review-wrong-doctor","review-snapshot-role","review-snapshot-owner","history-role","history-other-patient","empty-value","long-unit","long-range","empty-reason","long-reason","invalid-flag","unchanged","missing-item"})
        using(var fixture=new TransactionScope()){
            string code="QA-"+Guid.NewGuid().ToString("N").Substring(0,12);
            int test=Convert.ToInt32(db.Scalar("INSERT INTO gh.LabCatalog(TestCode,TestName,ReferenceRange) VALUES(@C,@C,'1-5');SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@C",code)));
            long encounter=service.RecordEncounter(patient,doctor,null,"QA amendment","QA assessment","QA plan",null,null,null,null,null,null,null,null,null,null,null,clinician);
            long order=service.AddClinicalOrder(encounter,doctor,clinician,"Laboratory",test,null,null,0);
            long item=Convert.ToInt64(db.Scalar("SELECT LabOrderItemId FROM gh.LabOrderItems WHERE LabOrderId=@O",Database.Parameter("@O",order)));
            service.ProgressLab(order,"Collected",lab);service.ProgressLab(order,"Processing",lab);
            if(scenario!="unfinished")service.FinalizeLabResult(item,"4","unit","Normal",lab);
            int observedVersion=Version(db,order);
            if(scenario=="valid"){
                var original=db.Query("SELECT * FROM gh.LabOrderItems WHERE LabOrderItemId=@I",Database.Parameter("@I",item)).Rows[0];
                var snapshot=service.GetLabReviewSnapshot(order,doctor,clinician);
                Assert(snapshot.Rows.Count==1&&Convert.ToString(snapshot.Rows[0]["Result"])=="4","Doctor review snapshot contains current results and order version");
                service.ReviewLab(order,doctor,clinician,observedVersion);
                Assert(PatientRows(service.GetOperationalWorklist("Laboratory",admin),patient)==0,"Reviewed original result leaves the laboratory worklist");
                service.AmendLabResult(item,0,"9","mmol/L","2-6","Critical","Corrected transcription",lab);
                var current=service.GetLabCorrectionSnapshot(item,lab).Rows[0];
                Assert(Convert.ToString(current["ResultValue"])=="9"&&Convert.ToString(current["ReferenceRange"])=="2-6"&&Convert.ToString(current["ResultFlag"])=="Critical"&&Convert.ToInt32(current["Revision"])==1,"Correction projection shows the actual new value, range, flag and revision");
                var retained=db.Query("SELECT * FROM gh.LabOrderItems WHERE LabOrderItemId=@I",Database.Parameter("@I",item)).Rows[0];
                Assert(Convert.ToString(retained["ResultValue"])=="4"&&Convert.ToString(retained["ResultFlag"])=="Normal"&&Equals(retained["ResultedAt"],original["ResultedAt"])&&Equals(retained["ResultedByUserId"],original["ResultedByUserId"]),"Original finalized clinical values, author and time are not overwritten");
                Assert(db.Scalar("SELECT ReviewedAt FROM gh.LabOrders WHERE LabOrderId=@O",Database.Parameter("@O",order))==DBNull.Value,"Amendment clears prior clinical review");
                Assert(Version(db,order)==observedVersion+1,"Correction advances the order review version");
                Assert(PatientRows(service.GetOperationalWorklist("Laboratory",admin),patient)==1,"Corrected result reappears in the outstanding worklist");
                Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.Notifications WHERE PatientId=@P AND Subject='Critical corrected laboratory result — review required' AND UserId=@U",Database.Parameter("@P",patient),Database.Parameter("@U",clinician)))==1,"Critical correction notifies its ordering clinician");
                Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.Notifications WHERE PatientId=@P AND Subject='Laboratory result corrected' AND UserId=@U",Database.Parameter("@P",patient),Database.Parameter("@U",patientActor)))==1,"Correction notifies the owning patient");
                Assert(Convert.ToInt32(service.GetOperationalAlerts(admin).Rows[0]["AffectedRecords"])>=1,"Critical-result alert uses the current corrected flag");
                var history=service.GetLabAmendmentHistory(patient,patientActor);
                Assert(history.Rows.Count==1&&Convert.ToString(history.Rows[0]["PreviousValue"])=="4"&&Convert.ToString(history.Rows[0]["CorrectedValue"])=="9","Patient can see attributed before-and-after correction history");
                Assert(Convert.ToString(db.Scalar("SELECT Detail FROM gh.vwPatientEvents WHERE PatientId=@P AND EventType='Laboratory result' AND Detail LIKE @Test",Database.Parameter("@P",patient),Database.Parameter("@Test",code+":%")))==code+": Normal","Original timeline result event retains its historical flag");
                Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.vwPatientEvents WHERE PatientId=@P AND EventType='Laboratory amendment' AND ActorUserId=@U AND Detail LIKE @Test",Database.Parameter("@P",patient),Database.Parameter("@U",lab),Database.Parameter("@Test",code+":%")))==1,"Amendment adds a separate dated and attributed patient event");
                service.AmendLabResult(item,1,"5","mmol/L","2-6","Normal","Corrected validated value",lab);
                history=service.GetLabAmendmentHistory(patient,clinician);
                Assert(history.Rows.Count==2&&Convert.ToString(history.Rows[0]["PreviousValue"])=="9"&&Convert.ToString(history.Rows[0]["CorrectedValue"])=="5","Subsequent amendment preserves the preceding revision without editing it");
                service.ReviewLab(order,doctor,clinician,Version(db,order));
                Assert(PatientRows(service.GetOperationalWorklist("Laboratory",admin),patient)==0,"Re-review of latest corrected results clears outstanding review");
                Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.WorkflowHistory WHERE EntityType='LabResult' AND EntityId=@I AND NewStatus='Amended' AND ActorUserId=@U",Database.Parameter("@I",item),Database.Parameter("@U",lab)))==2,"Each correction has transactional workflow accountability");
                continue;
            }
            if(scenario=="cancelled")db.Execute("UPDATE gh.LabOrders SET Status='Cancelled' WHERE LabOrderId=@O",Database.Parameter("@O",order));
            if(scenario=="stale-revision"||scenario=="replayed-amendment"||scenario=="review-stale")service.AmendLabResult(item,0,"9","unit","1-5","High","QA first correction",lab);
            bool rejected=false;
            try{
                if(scenario=="snapshot-role")service.GetLabCorrectionSnapshot(item,patientActor);
                else if(scenario=="review-stale"||scenario=="review-role"||scenario=="review-wrong-doctor")service.ReviewLab(order,scenario=="review-wrong-doctor"?doctor+100000:doctor,scenario=="review-role"?reception:clinician,observedVersion);
                else if(scenario=="review-snapshot-role")service.GetLabReviewSnapshot(order,doctor,reception);
                else if(scenario=="review-snapshot-owner"){
                    db.Execute("UPDATE gh.LabOrders SET OrderedByDoctorId=(SELECT TOP 1 doctorID FROM dbo.doctorTbl WHERE doctorID<>@D) WHERE LabOrderId=@O",Database.Parameter("@D",doctor),Database.Parameter("@O",order));
                    Assert(service.GetLabReviewSnapshot(order,doctor,clinician).Rows.Count==0,"Doctor cannot open another clinician's review snapshot");continue;
                }
                else if(scenario=="history-role"||scenario=="history-other-patient")service.GetLabAmendmentHistory(scenario=="history-other-patient"?patient+100000:patient,scenario=="history-role"?reception:patientActor);
                else service.AmendLabResult(scenario=="missing-item"?long.MaxValue:item,0,scenario=="empty-value"?"":scenario=="unchanged"?"4":"9",scenario=="long-unit"?new string('x',41):"unit",scenario=="long-range"?new string('x',251):"1-5",scenario=="invalid-flag"?"Okay":scenario=="unchanged"?"Normal":"High",scenario=="empty-reason"?"":scenario=="long-reason"?new string('x',701):"QA correction",scenario=="amend-role"?reception:lab);
            }catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}
            Assert(rejected,"Laboratory amendment guard: "+scenario);
        }
        Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.LabResultAmendments"))==before,"Laboratory correction fixtures leave no retained amendment records");
    }
}
