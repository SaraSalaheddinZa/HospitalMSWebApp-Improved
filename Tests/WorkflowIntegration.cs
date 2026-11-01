using System;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
class WorkflowIntegration
{
    static void Assert(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    static void Reject(Action action,string message){try{action();}catch(InvalidOperationException){Console.WriteLine("PASS: "+message);return;}throw new Exception("Expected rejection: "+message);}
    static int Actor(Database db,string role){return Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE RoleName=@R AND Email LIKE 'dev.%@guardianhealth.test'",Database.Parameter("@R",role)));}
    static int PatientRows(System.Data.DataTable rows,int patient){return rows.Select("MRN='GH-"+patient.ToString("D8")+"'").Length;}
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        var db=new Database();var wf=new HospitalWorkflowService();
        int patient=Convert.ToInt32(db.Scalar("SELECT PatientId FROM gh.UserAccounts WHERE Email='dev.patient@guardianhealth.test'"));
        int doctor=Convert.ToInt32(db.Scalar("SELECT LegacyUserId FROM gh.UserAccounts WHERE Email='dev.doctor@guardianhealth.test'"));
        int user=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.lab@guardianhealth.test'"));
        int clinician=Actor(db,"Doctor"),receptionist=Actor(db,"Receptionist"),nurse=Actor(db,"Nurse"),pharmacist=Actor(db,"Pharmacist"),admin=Actor(db,"Admin"),portalUser=Actor(db,"Patient");
        var accounts=db.Query("SELECT RoleName,PasswordHash,IsActive FROM gh.UserAccounts WHERE Email LIKE 'dev.%@guardianhealth.test'");
        Assert(accounts.Rows.Count==7,"All seven development accounts are preserved");
        foreach(System.Data.DataRow a in accounts.Rows) {
            string role=Convert.ToString(a["RoleName"]);
            Console.WriteLine("Checking development credential for "+role);
            string key="HOSPITALMS_TEST_"+role.ToUpperInvariant()+"_PASSWORD";
            string password=Environment.GetEnvironmentVariable(key);
            if(string.IsNullOrEmpty(password)) {
                if(Environment.GetEnvironmentVariable("HOSPITALMS_ALLOW_PARTIAL_CREDENTIAL_TESTS")!="1")throw new InvalidOperationException("Private credential required: "+key+". No authentication was attempted.");
                Console.WriteLine("BLOCKED: WorkflowIntegration credential check for "+role+"; "+key+" unavailable. Workflow fixtures continue independently.");
                continue;
            }
            Assert(Convert.ToBoolean(a["IsActive"])&&PasswordSecurity.Verify(password,Convert.ToString(a["PasswordHash"])),"Active development credential verifies for "+role);
        }
        using(var scope=new TransactionScope(TransactionScopeOption.Required,new TransactionOptions{IsolationLevel=IsolationLevel.Serializable}))
        {
            string code="QA-"+Guid.NewGuid().ToString("N").Substring(0,12);
            int legacyReception=Convert.ToInt32(db.Scalar("SELECT LegacyUserId FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",receptionist)));
            patient=wf.RegisterPatient("QA Patient "+code,"555-0199","Other",new DateTime(1990,1,1),"Rollback fixture","No known allergies",code+"@example.test","QA contact","555-0198","O+","",legacyReception,receptionist);
            portalUser=wf.CreatePatientAccount(patient,code+"@example.test","Guardian!2026",receptionist);
            Assert(Convert.ToString(db.Scalar("SELECT MedicalRecordNumber FROM dbo.patientTbl WHERE patientId=@P",Database.Parameter("@P",patient)))=="GH-"+patient.ToString("D8"),"Registration produces a unique MRN and linked portal account");
            int medication=Convert.ToInt32(db.Scalar("INSERT INTO gh.MedicationCatalog(MedicationCode,MedicationName) VALUES(@C,@C); SELECT CAST(SCOPE_IDENTITY() AS int);",Database.Parameter("@C",code)));
            int test=Convert.ToInt32(db.Scalar("INSERT INTO gh.LabCatalog(TestCode,TestName) VALUES(@C,@C);SELECT CAST(SCOPE_IDENTITY() AS int);",Database.Parameter("@C",code)));
            var clinicDate=new DateTime(2098,1,1);
            wf.AddDoctorSchedule(doctor,null,clinicDate,new TimeSpan(9,0,0),new TimeSpan(10,0,0),20,admin);
            var available=db.Query("EXEC gh.GetAvailableDoctorSlots @D,@Date",Database.Parameter("@D",doctor),Database.Parameter("@Date",clinicDate));
            Assert(available.Rows.Count==3,"Published clinic session exposes three real appointment slots");
            long ap=wf.BookAppointment(patient,doctor,clinicDate.AddHours(9),clinicDate.AddHours(9).AddMinutes(20),"Integration booking",portalUser);
            wf.TransitionAppointment(ap,"RescheduleRequested",portalUser,null,clinicDate.AddHours(9).AddMinutes(20),clinicDate.AddHours(9).AddMinutes(40),"Please move my visit");
            wf.RescheduleAppointment(ap,clinicDate.AddHours(9).AddMinutes(20),clinicDate.AddHours(9).AddMinutes(40),receptionist);
            Assert(db.Query("EXEC gh.GetAvailableDoctorSlots @D,@Date",Database.Parameter("@D",doctor),Database.Parameter("@Date",clinicDate)).Rows.Count==2,"Rescheduled appointment reserves exactly one slot");
            // Advance this rolled-back fixture to the attendance day without changing the application's clock.
            db.Execute("UPDATE gh.Appointments SET ScheduledStart=@S,ScheduledEnd=@E WHERE AppointmentId=@A",Database.Parameter("@S",DateTime.UtcNow.Date.AddHours(9)),Database.Parameter("@E",DateTime.UtcNow.Date.AddHours(9).AddMinutes(20)),Database.Parameter("@A",ap));
            wf.TransitionAppointment(ap,"Confirmed",receptionist);wf.TransitionAppointment(ap,"CheckedIn",receptionist);wf.TransitionAppointment(ap,"InProgress",receptionist);
            long encounter=wf.RecordEncounter(patient,doctor,ap,"Integration test","","",null,37,70,120,80,"Test",medication,"1 unit","daily",2,test,clinician);
            Assert(Convert.ToInt32(db.Scalar("SELECT RecordedByUserId FROM gh.VitalSigns WHERE EncounterId=@E",Database.Parameter("@E",encounter)))==clinician,"Consultation observations retain the recording clinician identity");
            Assert(PatientRows(wf.GetOperationalWorklist("Clinical",admin),patient)==1,"Open consultation appears in administrative clinical accountability worklist");
            Assert(PatientRows(wf.GetOperationalWorklist("Laboratory",admin),patient)==1,"New clinician order appears in the pending laboratory worklist");
            Assert(PatientRows(wf.GetOperationalWorklist("Pharmacy",admin),patient)==1,"New prescription appears in the pharmacy worklist");
            Assert(Convert.ToString(db.Scalar("SELECT Status FROM gh.Appointments WHERE AppointmentId=@A",Database.Parameter("@A",ap)))=="Completed","Linked consultation completes the appointment");
            wf.UpdateDraftEncounter(encounter,doctor,clinician,0,"Integration test","Updated assessment","Updated treatment",DateTime.UtcNow.AddDays(7));
            Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.EncounterDraftHistory WHERE EncounterId=@E",Database.Parameter("@E",encounter)))==1,"Draft edits preserve previous clinical text and author");
            int extraTest=Convert.ToInt32(db.Scalar("INSERT INTO gh.LabCatalog(TestCode,TestName) VALUES(@C,@C);SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@C",code+"-2")));
            long extraOrder=wf.AddClinicalOrder(encounter,doctor,clinician,"Laboratory",extraTest,null,null,0);
            wf.CancelClinicalOrder(extraOrder,doctor,clinician,"Laboratory","Duplicate investigation not needed");
            Assert(Convert.ToString(db.Scalar("SELECT Status FROM gh.LabOrders WHERE LabOrderId=@I",Database.Parameter("@I",extraOrder)))=="Cancelled","Additional consultation orders support safe cancellation before collection");
            long order=Convert.ToInt64(db.Scalar("SELECT LabOrderId FROM gh.LabOrders WHERE EncounterId=@E AND Status='Ordered'",Database.Parameter("@E",encounter)));
            long item=Convert.ToInt64(db.Scalar("SELECT LabOrderItemId FROM gh.LabOrderItems WHERE LabOrderId=@O",Database.Parameter("@O",order)));
            wf.ProgressLab(order,"Collected",user);wf.ProgressLab(order,"Processing",user);
            wf.FinalizeLabResult(item,"5","unit","Critical",user);
            Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.Notifications WHERE UserId=@U AND PatientId=@P AND Subject LIKE 'Critical laboratory%'",Database.Parameter("@U",clinician),Database.Parameter("@P",patient)))==1,"Critical result alerts the ordering clinician");
            wf.ReviewLab(order,doctor,clinician,Convert.ToInt32(db.Scalar("SELECT ResultVersion FROM gh.LabOrders WHERE LabOrderId=@O",Database.Parameter("@O",order))));
            Assert(PatientRows(wf.GetOperationalWorklist("Laboratory",admin),patient)==0,"Reviewed results and cancelled tests leave the outstanding laboratory worklist");
            Assert(db.Scalar("SELECT ReviewedAt FROM gh.LabOrders WHERE LabOrderId=@O",Database.Parameter("@O",order))!=DBNull.Value,"Ordering doctor can acknowledge completed results");
            wf.FinalizeEncounter(encounter,doctor,clinician);
            wf.AppendClinicalAddendum(encounter,doctor,clinician,"Integration addendum");
            Assert(Convert.ToString(db.Scalar("SELECT Status FROM gh.Encounters WHERE EncounterId=@E",Database.Parameter("@E",encounter)))=="Amended","Signed consultation retains an append-only addendum");
            long triage=wf.RecordTriage(patient,3,"Integration triage",nurse);
            wf.ProgressTriage(triage,"UnderCare",nurse);
            Assert(Convert.ToString(db.Scalar("SELECT Status FROM gh.TriageAssessments WHERE TriageAssessmentId=@T",Database.Parameter("@T",triage)))=="UnderCare","Emergency assessment remains under care pending inpatient handoff");
            Assert(Convert.ToString(db.Scalar("SELECT Status FROM gh.LabOrders WHERE LabOrderId=@O",Database.Parameter("@O",order)))=="Completed","Lab order completes");
            int supplier=wf.SaveSupplier(null,code,"Contact","","supplier@example.test",pharmacist);
            wf.ReceiveStock(medication,code,DateTime.UtcNow.AddYears(1),10,2,1,supplier,pharmacist);
            long batch=Convert.ToInt64(db.Scalar("SELECT MedicationBatchId FROM gh.MedicationBatches WHERE MedicationId=@M",Database.Parameter("@M",medication)));
            long rx=Convert.ToInt64(db.Scalar("SELECT i.PrescriptionItemId FROM gh.PrescriptionItems i JOIN gh.PrescriptionOrders o ON o.PrescriptionOrderId=i.PrescriptionOrderId WHERE o.EncounterId=@E",Database.Parameter("@E",encounter)));
            wf.DispensePrescriptionItem(rx,batch,2,pharmacist);
            Assert(PatientRows(wf.GetOperationalWorklist("Pharmacy",admin),patient)==0,"Full dispensing clears the pending pharmacy handoff");
            Assert(Convert.ToDecimal(db.Scalar("SELECT SUM(Quantity) FROM gh.StockMovements WHERE MedicationBatchId=@B",Database.Parameter("@B",batch)))==8,"Stock movement ledger matches on-hand stock");
            var countSnapshot=wf.GetStockCountSnapshot(batch,pharmacist).Rows[0];
            wf.ReconcileStock(batch,7,"Integration test stock count",pharmacist,Convert.ToInt64(countSnapshot["MovementVersion"]),Convert.ToDecimal(countSnapshot["QuantityOnHand"]));
            Assert(Convert.ToDecimal(db.Scalar("SELECT SUM(Quantity) FROM gh.StockMovements WHERE MedicationBatchId=@B",Database.Parameter("@B",batch)))==7,"Reconciliation is reflected in stock movement ledger");
            long invoice=wf.CreateInvoice(patient,"Integration test",100,null,receptionist);
            wf.AddInvoiceLine(invoice,"Additional service",2,25,receptionist);
            Assert(Convert.ToDecimal(db.Scalar("SELECT TotalAmount FROM gh.Invoices WHERE InvoiceId=@I",Database.Parameter("@I",invoice)))==150,"Invoice total follows invoice lines");
            int provider=wf.SaveInsuranceProvider(null,code,code,"","",true,receptionist);
            long policy=wf.AddPolicy(patient,provider,code,80,DateTime.UtcNow.Date,DateTime.UtcNow.AddYears(1).Date,receptionist);
            long claim=wf.SubmitClaim(invoice,policy,receptionist);
            Assert(PatientRows(wf.GetOperationalWorklist("Insurance",admin),patient)==1,"Submitted claim appears in administration's settlement worklist");
            wf.DecideClaim(claim,"UnderReview",0,admin,"Eligibility documents checked");
            wf.DecideClaim(claim,"Approved",120,admin,"Insurer approved");
            wf.TakePayment(invoice,10,"Card",code+"-card",receptionist);
            wf.TakePayment(invoice,20,"Transfer",code+"-transfer",receptionist);
            wf.DecideClaim(claim,"Paid",0,admin,"Remittance received",code);
            Assert(PatientRows(wf.GetOperationalWorklist("Insurance",admin),patient)==0,"Settled claims leave the outstanding claims queue");
            Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.Payments WHERE InsuranceClaimId=@C",Database.Parameter("@C",claim)))==1,"Insurance settlement has exactly one linked payment");
            Assert(Convert.ToString(db.Scalar("SELECT Status FROM gh.Invoices WHERE InvoiceId=@I",Database.Parameter("@I",invoice)))=="Paid","Patient payment clears remaining balance");
            long rejectedInvoice=wf.CreateInvoice(patient,"Rejected claim test",50,null,receptionist);
            long rejectedClaim=wf.SubmitClaim(rejectedInvoice,policy,receptionist);
            wf.DecideClaim(rejectedClaim,"Rejected",0,admin,"Insurer advised service excluded");
            wf.TakePayment(rejectedInvoice,50,"Cash",code+"-rejected",receptionist);
            Assert(Convert.ToString(db.Scalar("SELECT Status FROM gh.Invoices WHERE InvoiceId=@I",Database.Parameter("@I",rejectedInvoice)))=="Paid","Claim rejection releases the reserved amount for patient payment");
            Assert(wf.GetAuditHistory(admin,"Workflow","Claim",admin,DateTime.UtcNow.Date,DateTime.UtcNow.Date,"Remittance received").Rows.Count==1,"Accountability filters find the actual insurance settlement actor and decision");
            wf.SaveSystemSetting("ReceptionContact",code,admin);
            Assert(wf.GetAuditHistory(admin,"Access","SystemSetting",admin,DateTime.UtcNow.Date,DateTime.UtcNow.Date,code).Rows.Count==1,"System setting change and previous/new-value audit persist atomically");
            Assert(wf.GetAuditHistory(admin,"Access",null,null,null,null,"no-such-audit-"+code,2).Rows.Count==0,"Audit pagination and empty filters execute without fabricating events");
            int ward=wf.AddWard(code,"General",null,admin),room=wf.AddRoom(ward,"1","General",admin),bed1=wf.AddBed(room,"1",admin),bed2=wf.AddBed(room,"2",admin);
            long admission=wf.AdmitPatient(patient,bed1,doctor,"Integration test",admin,triage);
            Assert(Convert.ToInt64(db.Scalar("SELECT TriageAssessmentId FROM gh.Admissions WHERE AdmissionId=@A",Database.Parameter("@A",admission)))==triage,"Admission retains the explicitly selected same-patient emergency episode");
            Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.Notifications WHERE UserId=@U AND PatientId=@P AND Subject='New inpatient admission'",Database.Parameter("@U",nurse),Database.Parameter("@P",patient)))==1,"Inpatient assignment notifies nursing for cross-department handoff");
            Assert(PatientRows(wf.GetOperationalWorklist("Beds",admin),patient)==1,"Admission appears in current bed occupancy worklist");
            wf.RecordNursingNote(patient,admission,nurse,"Escalated","Integration test",37,70,120,80);
            wf.ProgressTriage(triage,"Resolved",nurse);
            Assert(PatientRows(wf.GetOperationalWorklist("Emergency",admin),patient)==0,"Nurse resolves emergency care after inpatient handoff and clears its worklist");
            wf.TransferAdmission(admission,bed2,admin);wf.ReadyBed(bed1,admin);
            Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.Notifications WHERE UserId=@U AND PatientId=@P AND Subject='Nursing assessment: Escalated'",Database.Parameter("@U",clinician),Database.Parameter("@P",patient)))==1,"Nursing escalation reaches the attending clinician");
            Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.VitalSigns WHERE PatientId=@P",Database.Parameter("@P",patient)))>0,"Nursing observations persist without attaching to an unrelated encounter");
            wf.DischargePatient(admission,"Integration test",admin);wf.ReadyBed(bed2,admin);
            Assert(Convert.ToString(db.Scalar("SELECT Status FROM gh.Beds WHERE BedId=@B",Database.Parameter("@B",bed2)))=="Available","Transferred bed is cleaned after discharge");
            long requestAppointment=wf.BookAppointment(patient,doctor,clinicDate.AddHours(9).AddMinutes(40),clinicDate.AddHours(10),"Request test",portalUser);
            wf.TransitionAppointment(requestAppointment,"Confirmed",receptionist);
            wf.TransitionAppointment(requestAppointment,"RescheduleRequested",portalUser,null,clinicDate.AddDays(1).AddHours(10),clinicDate.AddDays(1).AddHours(11),"Preferred new date");
            wf.DeclineRescheduleRequest(requestAppointment,"No published availability for that time",receptionist);
            Assert(Convert.ToString(db.Scalar("SELECT Status FROM gh.Appointments WHERE AppointmentId=@A",Database.Parameter("@A",requestAppointment)))=="Confirmed","Declined request restores the confirmed original appointment");
            wf.TransitionAppointment(requestAppointment,"Cancelled",portalUser);
            Assert(wf.GetPatientJourney(patient,portalUser).Rows.Count==7,"Patient can view authorized cross-department outstanding work");
            Assert(wf.GetOperationalAlerts(admin).Rows.Count==9,"Administrative operational rules use live database queries");
            foreach(var queue in new[]{"Appointments","Clinical","Laboratory","Pharmacy","Stock","Beds","Billing","Insurance","Emergency"})Assert(wf.GetOperationalWorklist(queue,admin).Columns.Contains("NextAction"),"Operational worklist query executes: "+queue);
            var events=wf.GetPatientEvents(patient,portalUser);
            Assert(events.Select("EventType='Triage'").Length==1&&events.Select("EventType='Nursing assessment'").Length==1&&events.Select("EventType='Clinical addendum'").Length==1,"Patient 360 preserves triage, nursing and addenda omitted by migration 013");
            Assert(events.Select("EventType='Appointment created'").Length==2,"Appointment creation is a recorded event, not the future appointment date");
            Assert(events.Select("EventType='Prescription' AND Detail='Prescription placed'").Length==1,"Historical prescription creation does not claim it was already dispensed");
            Assert(events.Select("EventType='Patient notification'").Length>0,"Patient notifications are part of the real care journey");
            Assert(wf.GetPatientEvents(patient,portalUser,"Nursing assessment",DateTime.UtcNow.Date,DateTime.UtcNow.Date).Rows.Count==1,"Timeline filters return the patient's selected real event type within inclusive UTC dates");
            Assert(wf.GetPatientEvents(patient,portalUser,null,new DateTime(2098,1,1),new DateTime(2098,1,1)).Rows.Count==0,"Future appointments are not fabricated as future recorded events");
            Assert(wf.GetPatientTasks(patient,portalUser).Rows.Count==0,"Completed cross-department work leaves no outstanding patient tasks");
            var movements=wf.GetOperationalReport("stock-movements",admin,DateTime.UtcNow.Date,DateTime.UtcNow.Date).Select("MedicationName='"+code+"' AND MovementType='Dispensed'");
            Assert(movements.Length==1&&Convert.ToDecimal(movements[0]["Units"])==-2,"Operational stock report uses actual dispensing ledger quantities");
            var decisions=wf.GetOperationalReport("claim-decisions",admin,DateTime.UtcNow.Date,DateTime.UtcNow.Date).Select("ProviderName='"+code+"' AND Status='Paid'");
            Assert(decisions.Length==1&&Convert.ToDecimal(decisions[0]["SettledAmount"])==120,"Insurance reporting reconciles the recorded settled claim amount");
            Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(DISTINCT EventType) FROM gh.vwPatientTimeline WHERE PatientId=@P",Database.Parameter("@P",patient)))>=10,"Patient timeline integrates clinical, pharmacy, admission and financial events");
            int otherPatient=Convert.ToInt32(db.Scalar("SELECT TOP 1 patientId FROM dbo.patientTbl WHERE IsActive=1 AND patientId<>@P",Database.Parameter("@P",patient)));
            var documentBytes=System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n% regression fixture\n%%EOF");
            long document=wf.StoreDocument(patient,"integration.pdf",documentBytes,receptionist);
            Assert(wf.GetDocument(document,portalUser).Rows.Count==1,"Patient can download own document");
            Assert(wf.GetDocument(document,user).Rows.Count==0,"Laboratory account cannot download documents");
            long otherDocument=wf.StoreDocument(otherPatient,"integration.pdf",documentBytes,receptionist);
            Assert(wf.GetDocument(otherDocument,portalUser).Rows.Count==0,"Patient cannot download another patient's document");
            Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.WorkflowHistory WHERE EntityType='Document' AND EntityId=@D",Database.Parameter("@D",document)))==1,"Document upload has transactional history");
            // Negative transaction test runs last: a local rollback also aborts the ambient fixture transaction.
            Reject(()=>wf.TransitionAppointment(ap,"Cancelled",receptionist,patient+100000),"Other patient appointment is inaccessible");
            Console.WriteLine("All integration fixtures roll back.");
        }
    }
}
