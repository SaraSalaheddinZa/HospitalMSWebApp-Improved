using System;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
class WorkflowGuards
{
    static int Actor(Database db,string role){return Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE RoleName=@R AND Email LIKE 'dev.%@guardianhealth.test'",Database.Parameter("@R",role)));}
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        var db=new Database();var wf=new HospitalWorkflowService();
        int patient=Convert.ToInt32(db.Scalar("SELECT PatientId FROM gh.UserAccounts WHERE Email='dev.patient@guardianhealth.test'"));
        int doctor=Convert.ToInt32(db.Scalar("SELECT LegacyUserId FROM gh.UserAccounts WHERE Email='dev.doctor@guardianhealth.test'"));
        int actor=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.lab@guardianhealth.test'"));
        int clinician=Actor(db,"Doctor"),receptionist=Actor(db,"Receptionist"),nurse=Actor(db,"Nurse"),pharmacist=Actor(db,"Pharmacist"),admin=Actor(db,"Admin"),portal=Actor(db,"Patient");
        foreach(var scenario in new[]{"lab-before-processing","lab-overwrite","lab-review-replay","clinical-ownership","clinical-sign-replay","triage-skip","patient-account-duplicate","document-upload-role","pharmacy-overdispense","invoice-overpayment","queue-backwards","clinical-role","lab-role","pharmacy-role","nursing-role","triage-role","claim-role","claim-replay","claim-premature-settlement","claim-reservation","claim-line-freeze","claim-duplicate","claim-rejection-reason","payment-method","payment-precision","draft-stale","draft-signed","patient-scope-forgery","patient-double-booking","admission-inactive-room","bed-occupied-ready","journey-ownership","alerts-role","order-duplicate","order-signed","order-collected-cancel","supplier-inactive","tasks-ownership","events-ownership","events-role","worklist-role","worklist-allowlist","timeline-date-range","audit-role","audit-source","audit-page","setting-role","setting-key","appointment-future-checkin","appointment-before-checkin","appointment-past-self-change","handoff-waiting","handoff-other-patient","handoff-resolved","handoff-duplicate","handoff-role"})
        using(var scope=new TransactionScope())
        {
            string code="QA-"+Guid.NewGuid().ToString("N").Substring(0,12);
            int medication=Convert.ToInt32(db.Scalar("INSERT INTO gh.MedicationCatalog(MedicationCode,MedicationName) VALUES(@C,@C);SELECT CAST(SCOPE_IDENTITY() AS int);",Database.Parameter("@C",code)));
            int test=Convert.ToInt32(db.Scalar("INSERT INTO gh.LabCatalog(TestCode,TestName) VALUES(@C,@C);SELECT CAST(SCOPE_IDENTITY() AS int);",Database.Parameter("@C",code)));
            long encounter=wf.RecordEncounter(patient,doctor,null,"Test","Test","Test",null,null,null,null,null,"Test",medication,"1","daily",1,test,clinician);
            long order=Convert.ToInt64(db.Scalar("SELECT LabOrderId FROM gh.LabOrders WHERE EncounterId=@E",Database.Parameter("@E",encounter)));
            long item=Convert.ToInt64(db.Scalar("SELECT LabOrderItemId FROM gh.LabOrderItems WHERE LabOrderId=@O",Database.Parameter("@O",order)));
            Action invalid;
            if(scenario=="lab-before-processing")invalid=()=>wf.FinalizeLabResult(item,"1","unit","Normal",actor);
            else if(scenario=="document-upload-role")invalid=()=>wf.StoreDocument(patient,"test.pdf",System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF"),actor);
            else if(scenario=="clinical-ownership")invalid=()=>wf.FinalizeEncounter(encounter,doctor+100000,clinician);
            else if(scenario=="clinical-sign-replay"){
                wf.FinalizeEncounter(encounter,doctor,clinician);invalid=()=>wf.FinalizeEncounter(encounter,doctor,clinician);
            }else if(scenario=="triage-skip"){
                long triage=wf.RecordTriage(patient,3,"Test",nurse);invalid=()=>wf.ProgressTriage(triage,"Resolved",nurse);
            }else if(scenario=="patient-account-duplicate")invalid=()=>wf.CreatePatientAccount(patient,code+"@example.test","Guardian!2026",receptionist);
            else if(scenario=="lab-review-replay"){
                wf.ProgressLab(order,"Collected",actor);wf.ProgressLab(order,"Processing",actor);wf.FinalizeLabResult(item,"1","unit","Normal",actor);wf.ReviewLab(order,doctor,clinician,Convert.ToInt32(db.Scalar("SELECT ResultVersion FROM gh.LabOrders WHERE LabOrderId=@O",Database.Parameter("@O",order))));
                invalid=()=>wf.ReviewLab(order,doctor,clinician,Convert.ToInt32(db.Scalar("SELECT ResultVersion FROM gh.LabOrders WHERE LabOrderId=@O",Database.Parameter("@O",order))));
            }
            else if(scenario=="lab-overwrite"){
                wf.ProgressLab(order,"Collected",actor);wf.ProgressLab(order,"Processing",actor);wf.FinalizeLabResult(item,"1","unit","Normal",actor);
                invalid=()=>wf.FinalizeLabResult(item,"2","unit","Normal",actor);
            }else if(scenario=="pharmacy-overdispense"){
                wf.ReceiveStock(medication,code,DateTime.UtcNow.AddYears(1),10,0,0,null,pharmacist);
                long batch=Convert.ToInt64(db.Scalar("SELECT MedicationBatchId FROM gh.MedicationBatches WHERE MedicationId=@M",Database.Parameter("@M",medication)));
                long rx=Convert.ToInt64(db.Scalar("SELECT i.PrescriptionItemId FROM gh.PrescriptionItems i JOIN gh.PrescriptionOrders o ON o.PrescriptionOrderId=i.PrescriptionOrderId WHERE o.EncounterId=@E",Database.Parameter("@E",encounter)));
                wf.DispensePrescriptionItem(rx,batch,1,pharmacist);invalid=()=>wf.DispensePrescriptionItem(rx,batch,1,pharmacist);
            }else if(scenario=="invoice-overpayment"){
                long invoice=wf.CreateInvoice(patient,"Test",10,null,receptionist);invalid=()=>wf.TakePayment(invoice,11,"Cash","Test",receptionist);
            }else if(scenario=="clinical-role")invalid=()=>wf.FinalizeEncounter(encounter,doctor,actor);
            else if(scenario=="lab-role")invalid=()=>wf.ProgressLab(order,"Collected",clinician);
            else if(scenario=="pharmacy-role")invalid=()=>wf.ReceiveStock(medication,code,DateTime.UtcNow.AddYears(1),1,0,0,null,actor);
            else if(scenario=="nursing-role")invalid=()=>wf.RecordNursingNote(patient,null,actor,"Stable","test",null,null,null,null);
            else if(scenario=="triage-role")invalid=()=>wf.RecordTriage(patient,3,"Test",actor);
            else if(scenario=="journey-ownership")invalid=()=>wf.GetPatientJourney(patient+100000,portal);
            else if(scenario=="alerts-role")invalid=()=>wf.GetOperationalAlerts(portal);
            else if(scenario=="tasks-ownership")invalid=()=>wf.GetPatientTasks(patient+100000,portal);
            else if(scenario=="events-ownership")invalid=()=>wf.GetPatientEvents(patient+100000,portal);
            else if(scenario=="events-role")invalid=()=>wf.GetPatientEvents(patient,receptionist);
            else if(scenario=="worklist-role")invalid=()=>wf.GetOperationalWorklist("Clinical",clinician);
            else if(scenario=="worklist-allowlist")invalid=()=>wf.GetOperationalWorklist("Appointments; SELECT 1",admin);
            else if(scenario=="timeline-date-range")invalid=()=>wf.GetPatientEvents(patient,portal,null,DateTime.UtcNow.AddDays(1),DateTime.UtcNow.Date);
            else if(scenario=="audit-role")invalid=()=>wf.GetAuditHistory(portal,"Workflow",null,null,null,null,null);
            else if(scenario=="audit-source")invalid=()=>wf.GetAuditHistory(admin,"unknown",null,null,null,null,null);
            else if(scenario=="audit-page")invalid=()=>wf.GetAuditHistory(admin,"Access",null,null,null,null,null,-1);
            else if(scenario=="setting-role")invalid=()=>wf.SaveSystemSetting("ReceptionContact","Test",portal);
            else if(scenario=="setting-key")invalid=()=>wf.SaveSystemSetting("ConnectionString","Test",admin);
            else if(scenario.StartsWith("handoff-")){
                int ward=wf.AddWard(code,"Emergency",null,admin),room=wf.AddRoom(ward,"1","General",admin),bed=wf.AddBed(room,"1",admin);
                int triagePatient=scenario=="handoff-other-patient"?Convert.ToInt32(db.Scalar("SELECT TOP 1 patientId FROM dbo.patientTbl WHERE patientId<>@P",Database.Parameter("@P",patient))):patient;
                long triage=wf.RecordTriage(triagePatient,3,"QA handoff",nurse);
                if(scenario!="handoff-waiting")wf.ProgressTriage(triage,"UnderCare",nurse);
                if(scenario=="handoff-resolved")wf.ProgressTriage(triage,"Resolved",nurse);
                if(scenario=="handoff-duplicate"){
                    long admission=wf.AdmitPatient(patient,bed,doctor,"Test",admin,triage);wf.DischargePatient(admission,"Test",admin);wf.ReadyBed(bed,admin);
                }
                invalid=()=>wf.AdmitPatient(patient,bed,doctor,"Test",scenario=="handoff-role"?nurse:admin,triage);
            }
            else if(scenario.StartsWith("appointment-")){
                var start=scenario=="appointment-past-self-change"?DateTime.UtcNow.AddDays(-2):DateTime.UtcNow.AddDays(2);
                long appointment=Convert.ToInt64(db.Scalar("INSERT INTO gh.Appointments(PatientId,DoctorId,ScheduledStart,ScheduledEnd,Status) VALUES(@P,@D,@S,@E,'Booked');SELECT CAST(SCOPE_IDENTITY() AS bigint)",Database.Parameter("@P",patient),Database.Parameter("@D",doctor),Database.Parameter("@S",start),Database.Parameter("@E",start.AddMinutes(20))));
                if(scenario=="appointment-future-checkin")invalid=()=>wf.TransitionAppointment(appointment,"CheckedIn",receptionist);
                else if(scenario=="appointment-past-self-change")invalid=()=>wf.TransitionAppointment(appointment,"Cancelled",portal);
                else invalid=()=>wf.RecordEncounter(patient,doctor,appointment,"Test","Assessment","Plan",null,null,null,null,null,null,null,null,null,null,null,clinician);
            }
            else if(scenario=="order-duplicate")invalid=()=>wf.AddClinicalOrder(encounter,doctor,clinician,"Prescription",medication,"1","daily",1);
            else if(scenario=="order-signed"){
                wf.FinalizeEncounter(encounter,doctor,clinician);invalid=()=>wf.AddClinicalOrder(encounter,doctor,clinician,"Laboratory",test,null,null,0);
            }else if(scenario=="order-collected-cancel"){
                wf.ProgressLab(order,"Collected",actor);invalid=()=>wf.CancelClinicalOrder(order,doctor,clinician,"Laboratory","Test");
            }else if(scenario=="supplier-inactive"){
                int supplier=wf.SaveSupplier(null,code,"Contact","","",pharmacist);wf.SetSupplierActive(supplier,false,pharmacist);
                invalid=()=>wf.ReceiveStock(medication,code,DateTime.UtcNow.AddYears(1),1,0,0,supplier,pharmacist);
            }
            else if(scenario=="draft-stale"){
                wf.UpdateDraftEncounter(encounter,doctor,clinician,0,"Test","First","Plan",null);
                invalid=()=>wf.UpdateDraftEncounter(encounter,doctor,clinician,0,"Test","Stale","Plan",null);
            }else if(scenario=="draft-signed"){
                wf.FinalizeEncounter(encounter,doctor,clinician);
                invalid=()=>wf.UpdateDraftEncounter(encounter,doctor,clinician,0,"Test","Change","Plan",null);
            }else if(scenario.StartsWith("claim-")||scenario.StartsWith("payment-")){
                long invoice=wf.CreateInvoice(patient,"Guard",100,null,receptionist);
                int provider=Convert.ToInt32(db.Scalar("INSERT INTO gh.InsuranceProviders(ProviderName,ProviderCode) VALUES(@C,@C);SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@C",code)));
                long policy=wf.AddPolicy(patient,provider,code,80,DateTime.UtcNow.Date,DateTime.UtcNow.AddYears(1),receptionist);
                long claim=wf.SubmitClaim(invoice,policy,receptionist);
                if(scenario=="claim-role")invalid=()=>wf.DecideClaim(claim,"Approved",80,receptionist);
                else if(scenario=="claim-replay"){
                    wf.DecideClaim(claim,"Approved",80,admin);wf.DecideClaim(claim,"Paid",0,admin,null,code);
                    invalid=()=>wf.DecideClaim(claim,"Paid",0,admin,null,code);
                }else if(scenario=="claim-premature-settlement")invalid=()=>wf.DecideClaim(claim,"Paid",0,admin,null,code);
                else if(scenario=="claim-reservation")invalid=()=>wf.TakePayment(invoice,21,"Cash",code,receptionist);
                else if(scenario=="claim-line-freeze")invalid=()=>wf.AddInvoiceLine(invoice,"Change",1,1,receptionist);
                else if(scenario=="claim-duplicate")invalid=()=>wf.SubmitClaim(invoice,policy,receptionist);
                else if(scenario=="claim-rejection-reason")invalid=()=>wf.DecideClaim(claim,"Rejected",0,admin);
                else if(scenario=="payment-method")invalid=()=>wf.TakePayment(invoice,1,"Insurance",code,receptionist);
                else invalid=()=>wf.TakePayment(invoice,0.001m,"Cash",code,receptionist);
            }else if(scenario=="patient-scope-forgery"){
                int other=Convert.ToInt32(db.Scalar("SELECT TOP 1 patientId FROM dbo.patientTbl WHERE patientId<>@P",Database.Parameter("@P",patient)));
                long appointment=Convert.ToInt64(db.Scalar("INSERT INTO gh.Appointments(PatientId,DoctorId,ScheduledStart,ScheduledEnd,Status) VALUES(@P,@D,'2098-02-01T10:00:00','2098-02-01T10:20:00','Booked');SELECT CAST(SCOPE_IDENTITY() AS bigint)",Database.Parameter("@P",other),Database.Parameter("@D",doctor)));
                invalid=()=>wf.TransitionAppointment(appointment,"Cancelled",portal,other);
            }else if(scenario=="patient-double-booking"){
                int another=Convert.ToInt32(db.Scalar("SELECT TOP 1 doctorID FROM dbo.doctorTbl WHERE doctorID<>@D AND doctorStatus='Active'",Database.Parameter("@D",doctor)));
                var day=new DateTime(2098,2,1);wf.AddDoctorSchedule(doctor,null,day,TimeSpan.FromHours(10),TimeSpan.FromHours(11),20,admin);wf.AddDoctorSchedule(another,null,day,TimeSpan.FromHours(10),TimeSpan.FromHours(11),20,admin);
                wf.BookAppointment(patient,doctor,day.AddHours(10),day.AddHours(10).AddMinutes(20),"Test",receptionist);
                invalid=()=>wf.BookAppointment(patient,another,day.AddHours(10),day.AddHours(10).AddMinutes(20),"Test",receptionist);
            }else if(scenario=="admission-inactive-room"||scenario=="bed-occupied-ready"){
                int ward=wf.AddWard(code,"General",null,admin),room=wf.AddRoom(ward,"1","General",admin),bed=wf.AddBed(room,"1",admin);
                if(scenario=="admission-inactive-room"){
                    db.Execute("UPDATE gh.Rooms SET IsActive=0 WHERE RoomId=@R",Database.Parameter("@R",room));invalid=()=>wf.AdmitPatient(patient,bed,doctor,"Test",admin);
                }else{wf.AdmitPatient(patient,bed,doctor,"Test",admin);invalid=()=>wf.ReadyBed(bed,admin);}
            }else{
                long ap=Convert.ToInt64(db.Scalar("INSERT INTO gh.Appointments(PatientId,DoctorId,ScheduledStart,ScheduledEnd,Status) VALUES(@P,@D,'2098-01-01T10:00:00','2098-01-01T10:20:00','InProgress');SELECT CAST(SCOPE_IDENTITY() AS bigint);",Database.Parameter("@P",patient),Database.Parameter("@D",doctor)));
                invalid=()=>wf.TransitionAppointment(ap,"Confirmed",receptionist);
            }
            bool rejected=false;try{invalid();}catch(InvalidOperationException){rejected=true;}catch(ArgumentException){rejected=true;}
            if(!rejected)throw new Exception("Guard failed: "+scenario);
            Console.WriteLine("PASS: "+scenario+" rejected; fixture rolled back");
        }
        foreach(string scenario in new[]{"duplicate-payment-reference","card-missing-reference","transfer-missing-reference"})
        using(var fixture=new TransactionScope()){
            long invoice=wf.CreateInvoice(patient,"QA payment integrity",100,null,receptionist);
            if(scenario=="duplicate-payment-reference")wf.TakePayment(invoice,10,"Card","QA-RECEIPT",receptionist);
            bool rejected=false;try{wf.TakePayment(invoice,10,scenario=="transfer-missing-reference"?"Transfer":"Card",scenario=="duplicate-payment-reference"?" QA-RECEIPT ":"",receptionist);}
            catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}
            if(!rejected)throw new Exception("Guard failed: "+scenario);Console.WriteLine("PASS: "+scenario+" rejected; fixture rolled back");
        }
        foreach(string scenario in new[]{"stock-receipt-precision","dispense-precision","duplicate-stock-receipt"})
        using(var fixture=new TransactionScope()){
            string code="QA-"+Guid.NewGuid().ToString("N").Substring(0,12);
            int medication=Convert.ToInt32(db.Scalar("INSERT INTO gh.MedicationCatalog(MedicationCode,MedicationName) VALUES(@C,@C);SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@C",code)));
            if(scenario=="duplicate-stock-receipt")wf.ReceiveStock(medication,code,DateTime.UtcNow.AddYears(1),1,0,0,null,pharmacist);
            bool rejected=false;try{
                if(scenario=="dispense-precision")wf.DispensePrescriptionItem(0,0,0.001m,pharmacist);
                else wf.ReceiveStock(medication,code,DateTime.UtcNow.AddYears(1),scenario=="stock-receipt-precision"?0.001m:1m,0,0,null,pharmacist);
            }catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}
            if(!rejected)throw new Exception("Guard failed: "+scenario);Console.WriteLine("PASS: "+scenario+" rejected; fixture rolled back");
        }
    }
}
