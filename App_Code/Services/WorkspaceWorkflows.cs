using System;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Services
{
    public sealed partial class HospitalWorkflowService
    {
        public System.Data.DataTable GetDocument(long id,int actor)
        {
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin","Doctor","Nurse","Receptionist","Pharmacist","Laboratorian","Patient");
                return ReadTable(c,t,@"SELECT d.FileName,d.ContentType,x.Content FROM gh.Documents d
                JOIN gh.DocumentContents x ON x.DocumentId=d.DocumentId
                JOIN gh.UserAccounts u ON u.UserId=@U AND u.IsActive=1
                WHERE d.DocumentId=@Id AND d.IsActive=1 AND
                (u.RoleName IN('Admin','Doctor','Nurse','Receptionist') OR (u.RoleName='Patient' AND u.PatientId=d.PatientId))",P("@U",actor),P("@Id",id));
            });
        }
        public long StoreDocument(int patient,string filename,byte[] bytes,int actor)
        {
            if(bytes==null||bytes.Length<1||bytes.Length>5*1024*1024)throw new ArgumentException("Choose a PDF, PNG or JPEG up to 5 MB.");
            string name=System.IO.Path.GetFileName(filename??""),extension=System.IO.Path.GetExtension(name).ToLowerInvariant(),mime;
            if(name.Length==0||name.Length>260)throw new ArgumentException("Choose a file name of up to 260 characters.");
            if(extension==".pdf"&&bytes.Length>5&&System.Text.Encoding.ASCII.GetString(bytes,0,5)=="%PDF-")mime="application/pdf";
            else if(extension==".png"&&bytes.Length>8&&bytes[0]==137&&bytes[1]==80&&bytes[2]==78&&bytes[3]==71&&bytes[4]==13&&bytes[5]==10&&bytes[6]==26&&bytes[7]==10)mime="image/png";
            else if((extension==".jpg"||extension==".jpeg")&&bytes.Length>3&&bytes[0]==255&&bytes[1]==216&&bytes[2]==255)mime="image/jpeg";
            else throw new ArgumentException("The document content does not match a supported file type.");
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin","Doctor","Receptionist");
                if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM dbo.patientTbl WHERE patientId=@P AND IsActive=1",P("@P",patient)))!=1)
                    throw new InvalidOperationException("Select an active patient.");
                long id=Convert.ToInt64(Scalar(c,t,@"INSERT INTO gh.Documents(PatientId,FileName,ContentType,StorageKey,UploadedByUserId)
                    VALUES(@P,@N,@T,@K,@U);SELECT CAST(SCOPE_IDENTITY() AS bigint);",P("@P",patient),P("@N",name),P("@T",mime),P("@K","database:"+Guid.NewGuid().ToString("N")),P("@U",actor)));
                var content=new System.Data.SqlClient.SqlParameter("@Bytes",System.Data.SqlDbType.VarBinary,-1){Value=bytes};
                Execute(c,t,"INSERT INTO gh.DocumentContents(DocumentId,Content) VALUES(@Id,@Bytes)",P("@Id",id),content);
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,NewStatus) VALUES('Document',@Id,@U,'Uploaded')",P("@Id",id),P("@U",actor));
                QueuePatientNotification(c,t,patient,"Documents","New document available","A document has been added to your patient record.");
                return id;
            });
        }
        public void UpdateDraftEncounter(long id,int doctor,int actor,int revision,string complaint,string assessment,string plan,DateTime? followUp)
        {
            if(string.IsNullOrWhiteSpace(complaint)||complaint.Length>1000||(assessment??"").Length>4000||(plan??"").Length>4000)throw new ArgumentException("Enter a complaint (up to 1,000 characters) and clinical text of up to 4,000 characters per field.");
            if(followUp.HasValue&&followUp.Value.Date<DateTime.UtcNow.Date)throw new ArgumentException("Follow-up cannot be in the past.");
            database.InTransaction((c,t)=>{
                RequireDoctor(c,t,actor,doctor);
                var current=QueryOne(c,t,"SELECT Status,NoteRevision FROM gh.Encounters WITH(UPDLOCK,HOLDLOCK) WHERE EncounterId=@I AND DoctorId=@D",P("@I",id),P("@D",doctor));
                if(current==null||Convert.ToString(current["Status"])!="Open")throw new InvalidOperationException("Only your open consultation can be edited. Signed notes require an addendum.");
                if(Convert.ToInt32(current["NoteRevision"])!=revision)throw new InvalidOperationException("This consultation changed after you opened it. Reload before editing.");
                Execute(c,t,@"INSERT INTO gh.EncounterDraftHistory(EncounterId,Revision,ChiefComplaint,Assessment,TreatmentPlan,FollowUpDate,ChangedByUserId) SELECT EncounterId,NoteRevision,ChiefComplaint,Assessment,TreatmentPlan,FollowUpDate,@U FROM gh.Encounters WHERE EncounterId=@I",P("@U",actor),P("@I",id));
                Execute(c,t,"UPDATE gh.Encounters SET ChiefComplaint=@C,Assessment=@A,TreatmentPlan=@P,FollowUpDate=@F,NoteRevision=NoteRevision+1 WHERE EncounterId=@I",P("@C",complaint),P("@A",assessment),P("@P",plan),P("@F",followUp),P("@I",id));
                History(c,t,"Encounter",id,actor,"DraftUpdated","Previous draft retained; revision "+(revision+1));
                return 0;
            });
        }
        public long AddClinicalOrder(long encounter,int doctor,int actor,string kind,int catalogId,string dose,string frequency,decimal quantity)
        {
            return database.InTransaction((c,t)=>CreateClinicalOrder(c,t,encounter,doctor,actor,kind,catalogId,dose,frequency,quantity));
        }
        private static long CreateClinicalOrder(System.Data.SqlClient.SqlConnection c,System.Data.SqlClient.SqlTransaction t,long encounter,int doctor,int actor,string kind,int catalogId,string dose,string frequency,decimal quantity)
        {
            if(kind!="Prescription"&&kind!="Laboratory")throw new ArgumentException("Choose a clinical order type.");
            if(kind=="Prescription"&&(quantity<=0||decimal.Round(quantity,2)!=quantity||string.IsNullOrWhiteSpace(dose)||string.IsNullOrWhiteSpace(frequency)||(dose??"").Length>100||(frequency??"").Length>100))throw new ArgumentException("Enter a dose, frequency and positive quantity with up to two decimal places.");
                RequireDoctor(c,t,actor,doctor);
                var patient=Scalar(c,t,"SELECT PatientId FROM gh.Encounters WITH(UPDLOCK,HOLDLOCK) WHERE EncounterId=@E AND DoctorId=@D AND Status='Open'",P("@E",encounter),P("@D",doctor));
                if(patient==null)throw new InvalidOperationException("Select one of your open consultations.");
                long id;
                if(kind=="Prescription"){
                    if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.MedicationCatalog WHERE MedicationId=@M AND IsActive=1",P("@M",catalogId)))!=1)throw new InvalidOperationException("Select an active medication.");
                    if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.PrescriptionOrders o JOIN gh.PrescriptionItems i ON i.PrescriptionOrderId=o.PrescriptionOrderId WHERE o.EncounterId=@E AND i.MedicationId=@M AND o.Status<>'Cancelled'",P("@E",encounter),P("@M",catalogId)))>0)throw new InvalidOperationException("This medication is already prescribed for this consultation.");
                    id=Convert.ToInt64(Scalar(c,t,"INSERT INTO gh.PrescriptionOrders(EncounterId,PatientId,PrescribedByDoctorId) VALUES(@E,@P,@D);SELECT CAST(SCOPE_IDENTITY() AS bigint)",P("@E",encounter),P("@P",patient),P("@D",doctor)));
                    Execute(c,t,"INSERT INTO gh.PrescriptionItems(PrescriptionOrderId,MedicationId,Dose,Frequency,Quantity) VALUES(@O,@M,@Dose,@F,@Q)",P("@O",id),P("@M",catalogId),P("@Dose",dose),P("@F",frequency),P("@Q",quantity));
                }else{
                    if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.LabCatalog WHERE LabTestId=@L AND IsActive=1",P("@L",catalogId)))!=1)throw new InvalidOperationException("Select an active laboratory test.");
                    if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.LabOrders o JOIN gh.LabOrderItems i ON i.LabOrderId=o.LabOrderId WHERE o.EncounterId=@E AND i.LabTestId=@L AND o.Status<>'Cancelled'",P("@E",encounter),P("@L",catalogId)))>0)throw new InvalidOperationException("This test is already ordered for this consultation.");
                    id=Convert.ToInt64(Scalar(c,t,"INSERT INTO gh.LabOrders(EncounterId,PatientId,OrderedByDoctorId) VALUES(@E,@P,@D);SELECT CAST(SCOPE_IDENTITY() AS bigint)",P("@E",encounter),P("@P",patient),P("@D",doctor)));
                    Execute(c,t,"INSERT INTO gh.LabOrderItems(LabOrderId,LabTestId,ReferenceRange,ResultStatus) SELECT @O,LabTestId,ReferenceRange,'Pending' FROM gh.LabCatalog WHERE LabTestId=@L",P("@O",id),P("@L",catalogId));
                }
                History(c,t,kind=="Prescription"?"PrescriptionOrder":"LabOrder",id,actor,"Ordered","Added to consultation "+encounter);
                QueuePatientNotification(c,t,Convert.ToInt32(patient),kind,"New "+kind.ToLowerInvariant()+" order","Your clinician has added an order to your care record.");
                Execute(c,t,"INSERT INTO gh.Notifications(UserId,PatientId,NotificationType,Subject,Message,SentAt) SELECT UserId,@P,@T,'New order awaiting processing','A clinical order is ready in your work queue.',SYSUTCDATETIME() FROM gh.UserAccounts WHERE RoleName=@R AND IsActive=1",P("@P",patient),P("@T",kind),P("@R",kind=="Prescription"?"Pharmacist":"Laboratorian"));
                return id;
        }
        public void FinalizeEncounter(long id,int doctor,int actor)
        {
            database.InTransaction((c,t)=>{
                RequireDoctor(c,t,actor,doctor);
                if(Execute(c,t,@"UPDATE gh.Encounters SET Status='Signed',ClosedAt=SYSUTCDATETIME() WHERE EncounterId=@Id AND DoctorId=@D AND Status='Open' AND LEN(LTRIM(RTRIM(ISNULL(Assessment,''))))>0 AND LEN(LTRIM(RTRIM(ISNULL(TreatmentPlan,''))))>0",P("@Id",id),P("@D",doctor))!=1)throw new InvalidOperationException("Select your open consultation with an assessment and treatment plan.");
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus) VALUES('Encounter',@Id,@U,'Open','Signed')",P("@Id",id),P("@U",actor));
                return 0;
            });
        }
        public void CancelClinicalOrder(long id,int doctor,int actor,string kind,string reason)
        {
            if(string.IsNullOrWhiteSpace(reason)||reason.Length>700)throw new ArgumentException("Enter the cancellation reason (up to 700 characters).");
            if(kind!="Prescription"&&kind!="Laboratory")throw new ArgumentException("Choose an order type.");
            database.InTransaction((c,t)=>{
                RequireDoctor(c,t,actor,doctor);
                string sql=kind=="Prescription"
                    ?"SELECT PatientId FROM gh.PrescriptionOrders WITH(UPDLOCK,HOLDLOCK) WHERE PrescriptionOrderId=@I AND PrescribedByDoctorId=@D AND Status='Pending' AND NOT EXISTS(SELECT 1 FROM gh.Dispensations x JOIN gh.PrescriptionItems i ON i.PrescriptionItemId=x.PrescriptionItemId WHERE i.PrescriptionOrderId=@I)"
                    :"SELECT PatientId FROM gh.LabOrders WITH(UPDLOCK,HOLDLOCK) WHERE LabOrderId=@I AND OrderedByDoctorId=@D AND Status='Ordered'";
                var patient=Scalar(c,t,sql,P("@I",id),P("@D",doctor));
                if(patient==null)throw new InvalidOperationException("Only your unprocessed order can be cancelled. Dispensed medicines and collected samples cannot be cancelled here.");
                Execute(c,t,kind=="Prescription"?"UPDATE gh.PrescriptionOrders SET Status='Cancelled' WHERE PrescriptionOrderId=@I":"UPDATE gh.LabOrders SET Status='Cancelled' WHERE LabOrderId=@I",P("@I",id));
                History(c,t,kind=="Prescription"?"PrescriptionOrder":"LabOrder",id,actor,"Cancelled",reason);
                QueuePatientNotification(c,t,Convert.ToInt32(patient),kind,"Care order cancelled","Your clinician has cancelled a "+kind.ToLowerInvariant()+" order. Contact the care team if you have questions.");
                return 0;
            });
        }
        public void AppendClinicalAddendum(long id,int doctor,int actor,string note)
        {
            if(string.IsNullOrWhiteSpace(note)||note.Length>4000)throw new ArgumentException("Enter an addendum of up to 4,000 characters.");
            database.InTransaction((c,t)=>{
                RequireDoctor(c,t,actor,doctor);
                if(Execute(c,t,"UPDATE gh.Encounters SET Status='Amended' WHERE EncounterId=@Id AND DoctorId=@D AND Status IN('Signed','Amended')",P("@Id",id),P("@D",doctor))!=1)throw new InvalidOperationException("Select one of your finalized consultations.");
                Execute(c,t,"INSERT INTO gh.EncounterAddenda(EncounterId,AuthorUserId,Note) VALUES(@Id,@U,@N)",P("@Id",id),P("@U",actor),P("@N",note));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,NewStatus) VALUES('Encounter',@Id,@U,'Amended')",P("@Id",id),P("@U",actor));
                return 0;
            });
        }
        public void ReviewLab(long id,int doctor,int actor,int expectedResultVersion)
        {
            database.InTransaction((c,t)=>{
                RequireDoctor(c,t,actor,doctor);
                if(Execute(c,t,"UPDATE gh.LabOrders SET ReviewedAt=SYSUTCDATETIME(),ReviewedByDoctorId=@D WHERE LabOrderId=@Id AND OrderedByDoctorId=@D AND Status='Completed' AND ReviewedAt IS NULL AND ResultVersion=@Version",P("@D",doctor),P("@Id",id),P("@Version",expectedResultVersion))!=1)throw new InvalidOperationException("Open the current unreviewed completed results assigned to you. Results may have changed since you opened this form.");
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,NewStatus,Detail) VALUES('LabOrder',@Id,@U,'Reviewed',@D)",P("@Id",id),P("@U",actor),P("@D","Result version "+expectedResultVersion));
                return 0;
            });
        }
        public void ProgressTriage(long id,string status,int actor)
        {
            if(status!="UnderCare"&&status!="Resolved")throw new ArgumentException("Select a valid triage state.");
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Nurse","Doctor");
                string before=Convert.ToString(Scalar(c,t,"SELECT Status FROM gh.TriageAssessments WITH(UPDLOCK,HOLDLOCK) WHERE TriageAssessmentId=@Id",P("@Id",id)));
                if((status=="UnderCare"&&before!="Waiting")||(status=="Resolved"&&before!="UnderCare"))throw new InvalidOperationException("This triage record is not ready for that transition.");
                Execute(c,t,"UPDATE gh.TriageAssessments SET Status=@S WHERE TriageAssessmentId=@Id",P("@S",status),P("@Id",id));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus) VALUES('Triage',@Id,@U,@Old,@New)",P("@Id",id),P("@U",actor),P("@Old",before),P("@New",status));
                return 0;
            });
        }
        public System.Data.DataTable GetStockCountSnapshot(long batch,int actor)
        {
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Pharmacist");
                return ReadTable(c,t,@"SELECT b.MedicationBatchId,m.MedicationName,b.BatchNumber,b.QuantityOnHand,
                    ISNULL((SELECT MAX(StockMovementId) FROM gh.StockMovements WHERE MedicationBatchId=b.MedicationBatchId),0) AS MovementVersion
                    FROM gh.MedicationBatches b JOIN gh.MedicationCatalog m ON m.MedicationId=b.MedicationId WHERE b.MedicationBatchId=@B",P("@B",batch));
            });
        }
        public void ReconcileStock(long batch,decimal counted,string reason,int actor,long expectedMovementId,decimal expectedQuantity)
        {
            Money(counted);Money(expectedQuantity);
            if(counted<0||expectedQuantity<0||expectedMovementId<0||string.IsNullOrWhiteSpace(reason)||reason.Length>700)throw new ArgumentException("Open a current batch count, enter a nonnegative quantity and a reconciliation reason of up to 700 characters.");
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Pharmacist");
                var value=Scalar(c,t,"SELECT QuantityOnHand FROM gh.MedicationBatches WITH(UPDLOCK,HOLDLOCK) WHERE MedicationBatchId=@B",P("@B",batch));
                if(value==null)throw new InvalidOperationException("Batch not found.");
                long version=Convert.ToInt64(Scalar(c,t,"SELECT ISNULL(MAX(StockMovementId),0) FROM gh.StockMovements WHERE MedicationBatchId=@B",P("@B",batch)));
                if(version!=expectedMovementId||Convert.ToDecimal(value)!=expectedQuantity)throw new InvalidOperationException("Stock changed after this count was opened. Reopen the batch and recount before reconciling; no adjustment was applied.");
                decimal delta=counted-Convert.ToDecimal(value);
                if(delta==0)throw new InvalidOperationException("The count already matches stock on hand.");
                Execute(c,t,"UPDATE gh.MedicationBatches SET QuantityOnHand=@Q WHERE MedicationBatchId=@B",P("@Q",counted),P("@B",batch));
                Execute(c,t,"INSERT INTO gh.StockMovements(MedicationBatchId,MovementType,Quantity,ReferenceType,RecordedByUserId) VALUES(@B,'Adjustment',@Q,'StockCount',@U)",P("@B",batch),P("@Q",delta),P("@U",actor));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,Detail) VALUES('StockCount',@B,@U,@D)",P("@B",batch),P("@U",actor),P("@D","Previous: "+expectedQuantity.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+"; Counted: "+counted.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+"; Observed movement: "+expectedMovementId+"; "+reason));
                return 0;
            });
        }
        public void SetStaffActive(int id,bool active,int actor)
        {
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin");
                var user=QueryOne(c,t,"SELECT RoleName,StaffMemberId,IsActive FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK) WHERE UserId=@Id",P("@Id",id));
                if(user==null || !new[]{"Nurse","Pharmacist"}.ContainsValue(Convert.ToString(user["RoleName"])))throw new InvalidOperationException("Use the dedicated staff directory for this role.");
                if(Convert.ToBoolean(user["IsActive"])==active)throw new InvalidOperationException("This account already has the selected access status.");
                if(Execute(c,t,"UPDATE gh.StaffMembers SET IsActive=@A,UpdatedAt=SYSUTCDATETIME() WHERE StaffMemberId=@Id AND RoleName=@R",P("@A",active),P("@Id",user["StaffMemberId"]),P("@R",user["RoleName"]))!=1)throw new InvalidOperationException("The linked staff profile is inconsistent. No access change was saved.");
                Execute(c,t,"UPDATE gh.UserAccounts SET IsActive=@A,AuthVersion=AuthVersion+1 WHERE UserId=@Id",P("@A",active),P("@Id",id));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus) VALUES('UserAccount',@Id,@U,@Old,@S)",P("@Id",id),P("@U",actor),P("@Old",Convert.ToBoolean(user["IsActive"])?"Active":"Inactive"),P("@S",active?"Active":"Inactive"));
                return 0;
            });
        }
        public void DecideClaim(long id,string status,decimal approved,int actor,string note=null,string settlementReference=null)
        {
            if(!new[]{"UnderReview","Approved","Rejected","Paid"}.ContainsValue(status))throw new ArgumentException("Choose a claim decision.");
            Money(approved);
            if((note??"").Length>700||(settlementReference??"").Length>80)throw new ArgumentException("Keep the decision note within 700 characters and settlement reference within 80.");
            if(status=="Rejected"&&string.IsNullOrWhiteSpace(note))throw new ArgumentException("Record the insurer's reason for rejection.");
            if(status=="Paid"&&string.IsNullOrWhiteSpace(settlementReference))throw new ArgumentException("Enter the received insurance payment reference.");
            var invoiceId=database.Scalar("SELECT InvoiceId FROM gh.InsuranceClaims WHERE InsuranceClaimId=@Id",P("@Id",id));
            if(invoiceId==null)throw new InvalidOperationException("Claim not found.");
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin");
                // All claim/payment workflows lock the invoice before its claims to avoid lock inversion.
                var invoice=QueryOne(c,t,"SELECT TotalAmount,PaidAmount,Status FROM gh.Invoices WITH(UPDLOCK,HOLDLOCK) WHERE InvoiceId=@I",P("@I",invoiceId));
                var claim=QueryOne(c,t,"SELECT InvoiceId,Status,ClaimedAmount,ApprovedAmount FROM gh.InsuranceClaims WITH(UPDLOCK,HOLDLOCK) WHERE InsuranceClaimId=@Id",P("@Id",id));
                if(claim==null)throw new InvalidOperationException("Claim not found.");
                var before=Convert.ToString(claim["Status"]);
                if(status=="Paid"){
                    if(before!="Approved")throw new InvalidOperationException("Only an approved claim can be settled.");
                    approved=Convert.ToDecimal(claim["ApprovedAmount"]);
                    if(invoice==null||!new[]{"Open","PartiallyPaid"}.ContainsValue(Convert.ToString(invoice["Status"]))||approved<=0||approved>Convert.ToDecimal(invoice["TotalAmount"])-Convert.ToDecimal(invoice["PaidAmount"]))throw new InvalidOperationException("Settlement exceeds the current outstanding balance.");
                    Execute(c,t,"INSERT INTO gh.Payments(InvoiceId,Amount,Method,ReferenceNumber,ReceivedByUserId,InsuranceClaimId) VALUES(@I,@A,'Insurance',@R,@U,@C)",P("@I",claim["InvoiceId"]),P("@A",approved),P("@R",settlementReference.Trim()),P("@U",actor),P("@C",id));
                    Execute(c,t,"UPDATE gh.Invoices SET PaidAmount=PaidAmount+@A,Status=CASE WHEN PaidAmount+@A=TotalAmount THEN 'Paid' ELSE 'PartiallyPaid' END WHERE InvoiceId=@I",P("@A",approved),P("@I",claim["InvoiceId"]));
                }else if(status=="UnderReview"){
                    if(before!="Submitted")throw new InvalidOperationException("Only a submitted claim can enter review.");
                    approved=0;
                }else{
                    if(before!="Submitted"&&before!="UnderReview")throw new InvalidOperationException("This claim has already been decided.");
                    if(status=="Approved"&&(approved<=0||approved>Convert.ToDecimal(claim["ClaimedAmount"])))throw new ArgumentException("Approval must be positive and no more than the claimed amount.");
                    if(status=="Rejected")approved=0;
                }
                Execute(c,t,"UPDATE gh.InsuranceClaims SET Status=@S,ApprovedAmount=@A,DecisionNote=COALESCE(NULLIF(@N,''),DecisionNote),SettlementReference=CASE WHEN @S='Paid' THEN @R ELSE SettlementReference END,DecidedAt=CASE WHEN @S IN('Approved','Rejected') THEN SYSUTCDATETIME() ELSE DecidedAt END,SettledAt=CASE WHEN @S='Paid' THEN SYSUTCDATETIME() ELSE SettledAt END WHERE InsuranceClaimId=@Id",P("@S",status),P("@A",approved),P("@Id",id),P("@N",note),P("@R",settlementReference));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus,Detail) VALUES('Claim',@Id,@U,@Old,@New,@D)",P("@Id",id),P("@U",actor),P("@Old",before),P("@New",status),P("@D",(note??"")+(status=="Paid"?"; Receipt: "+settlementReference:"")));
                int patient=Convert.ToInt32(Scalar(c,t,"SELECT PatientId FROM gh.Invoices WHERE InvoiceId=@I",P("@I",claim["InvoiceId"])));
                QueuePatientNotification(c,t,patient,"Insurance","Insurance claim update","Your claim status is now "+status+".");
                return 0;
            });
        }
        public long AddInvoiceLine(long invoiceId,string description,decimal quantity,decimal price,int actor)
        {
            if(string.IsNullOrWhiteSpace(description)||quantity<=0||price<0)throw new ArgumentException("Enter a description, positive quantity, and nonnegative price.");
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Receptionist","Admin");
                Money(price);Money(quantity);
                var status=Convert.ToString(Scalar(c,t,"SELECT Status FROM gh.Invoices WITH(UPDLOCK,HOLDLOCK) WHERE InvoiceId=@Id",P("@Id",invoiceId)));
                if(status!="Open"&&status!="Draft")throw new InvalidOperationException("Only open or draft invoices can receive new line items.");
                if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.InsuranceClaims WHERE InvoiceId=@I AND Status<>'Rejected'",P("@I",invoiceId)))!=0)throw new InvalidOperationException("An invoice with a submitted claim cannot be changed.");
                long id=Convert.ToInt64(Scalar(c,t,"INSERT INTO gh.InvoiceItems(InvoiceId,ItemType,Description,Quantity,UnitPrice) VALUES(@Id,'Service',@D,@Q,@P); SELECT CAST(SCOPE_IDENTITY() AS bigint);",P("@Id",invoiceId),P("@D",description),P("@Q",quantity),P("@P",price)));
                Execute(c,t,"UPDATE gh.Invoices SET TotalAmount=(SELECT SUM(LineTotal) FROM gh.InvoiceItems WHERE InvoiceId=@Id) WHERE InvoiceId=@Id",P("@Id",invoiceId));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,Detail) VALUES('Invoice',@Id,@U,@D)",P("@Id",invoiceId),P("@U",actor),P("@D","Added invoice line "+id));
                return id;
            });
        }
        public int SaveInsuranceProvider(int? id,string name,string code,string phone,string email,bool active,int actor)
        {
            if(string.IsNullOrWhiteSpace(name)||name.Length>160||string.IsNullOrWhiteSpace(code)||code.Length>40||(phone??"").Length>30||(email??"").Length>254)throw new ArgumentException("Enter a provider name, code and valid contact lengths.");
            if(!string.IsNullOrWhiteSpace(email)){try{if(new System.Net.Mail.MailAddress(email).Address!=email)throw new FormatException();}catch(FormatException){throw new ArgumentException("Enter a valid provider email.");}}
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Receptionist","Admin");
                int result;
                if(id.HasValue){
                    if(Execute(c,t,"UPDATE gh.InsuranceProviders SET ProviderName=@N,ProviderCode=@C,Phone=@P,Email=@E,IsActive=@A WHERE InsuranceProviderId=@I",P("@N",name),P("@C",code),P("@P",phone),P("@E",email),P("@A",active),P("@I",id))!=1)throw new InvalidOperationException("Provider not found.");result=id.Value;
                }else result=Convert.ToInt32(Scalar(c,t,"INSERT INTO gh.InsuranceProviders(ProviderName,ProviderCode,Phone,Email,IsActive) VALUES(@N,@C,@P,@E,@A);SELECT CAST(SCOPE_IDENTITY() AS int)",P("@N",name),P("@C",code),P("@P",phone),P("@E",email),P("@A",active)));
                History(c,t,"InsuranceProvider",result,actor,active?"Active":"Inactive",id.HasValue?"Provider details updated":"Provider created");return result;
            });
        }
        public void SetPolicyActive(long id,bool active,int actor)
        {
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Receptionist","Admin");
                if(Execute(c,t,"UPDATE gh.InsurancePolicies SET IsActive=@A WHERE InsurancePolicyId=@I AND IsActive<>@A",P("@A",active),P("@I",id))!=1)throw new InvalidOperationException("Policy not found or already in that state.");
                History(c,t,"InsurancePolicy",id,actor,active?"Active":"Inactive","Applies to future claim eligibility; existing claim decisions are retained");return 0;
            });
        }
        public long AddPolicy(int patient,int provider,string number,decimal coverage,DateTime from,DateTime to,int actor = 0)
        {
            if(coverage<0||coverage>100||to<from||string.IsNullOrWhiteSpace(number))throw new ArgumentException("Check policy number, coverage percentage and validity dates.");
            return database.InTransaction((c,t)=>{
                actor=RequireActor(c,t,actor,"Receptionist","Admin");
                if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM dbo.patientTbl p CROSS JOIN gh.InsuranceProviders v WHERE p.patientId=@P AND v.InsuranceProviderId=@V AND p.IsActive=1 AND v.IsActive=1",P("@P",patient),P("@V",provider)))!=1)throw new InvalidOperationException("Select an active patient and insurance provider.");
                long id=Convert.ToInt64(Scalar(c,t,"INSERT INTO gh.InsurancePolicies(PatientId,InsuranceProviderId,PolicyNumber,CoveragePercent,ValidFrom,ValidTo) VALUES(@P,@I,@N,@C,@F,@T);SELECT CAST(SCOPE_IDENTITY() AS bigint);",P("@P",patient),P("@I",provider),P("@N",number),P("@C",coverage),P("@F",from),P("@T",to)));
                History(c,t,"InsurancePolicy",id,actor,"Created","Recorded coverage "+coverage.ToString(System.Globalization.CultureInfo.InvariantCulture)+"%");return id;
            });
        }
        public long SubmitClaim(long invoice,long policy,int actor)
        {
            return database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Receptionist","Admin");
                var i=QueryOne(c,t,"SELECT PatientId,TotalAmount,PaidAmount,Status FROM gh.Invoices WITH(UPDLOCK,HOLDLOCK) WHERE InvoiceId=@Id",P("@Id",invoice));
                var p=QueryOne(c,t,@"SELECT p.PatientId,p.CoveragePercent FROM gh.InsurancePolicies p JOIN gh.InsuranceProviders v ON v.InsuranceProviderId=p.InsuranceProviderId WHERE p.InsurancePolicyId=@Id AND p.IsActive=1 AND v.IsActive=1 AND (p.ValidFrom IS NULL OR p.ValidFrom<=CAST(SYSUTCDATETIME() AS date)) AND (p.ValidTo IS NULL OR p.ValidTo>=CAST(SYSUTCDATETIME() AS date))",P("@Id",policy));
                if(i==null||p==null||Convert.ToInt32(i["PatientId"])!=Convert.ToInt32(p["PatientId"])||!(new[]{"Open","PartiallyPaid"}).ContainsValue(Convert.ToString(i["Status"])))throw new InvalidOperationException("The policy must be valid and belong to this outstanding invoice's patient.");
                if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.InsuranceClaims WITH(UPDLOCK,HOLDLOCK) WHERE InvoiceId=@Id AND Status<>'Rejected'",P("@Id",invoice)))>0)throw new InvalidOperationException("This invoice already has an active claim.");
                decimal amount=Math.Min(Convert.ToDecimal(i["TotalAmount"])-Convert.ToDecimal(i["PaidAmount"]),Math.Round(Convert.ToDecimal(i["TotalAmount"])*Convert.ToDecimal(p["CoveragePercent"])/100,2));
                if(amount<=0)throw new InvalidOperationException("No covered balance remains.");
                long id=Convert.ToInt64(Scalar(c,t,"INSERT INTO gh.InsuranceClaims(InvoiceId,InsurancePolicyId,ClaimNumber,ClaimedAmount) VALUES(@I,@P,@N,@A);SELECT CAST(SCOPE_IDENTITY() AS bigint);",P("@I",invoice),P("@P",policy),P("@N","CLM-"+Guid.NewGuid().ToString("N")),P("@A",amount)));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,NewStatus) VALUES('Claim',@Id,@U,'Submitted')",P("@Id",id),P("@U",actor));
                QueuePatientNotification(c,t,Convert.ToInt32(i["PatientId"]),"Insurance","Insurance claim recorded","A claim has been recorded for your invoice.");
                return id;
            });
        }
        public void TransferAdmission(long id, int bed, int actor)
        {
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin");
                var a=QueryOne(c,t,"SELECT PatientId,BedId FROM gh.Admissions WITH(UPDLOCK,HOLDLOCK) WHERE AdmissionId=@Id AND DischargedAt IS NULL AND Status IN('Admitted','Transferred')",P("@Id",id));
                if(a==null)throw new InvalidOperationException("Active admission not found.");
                if(Convert.ToString(Scalar(c,t,@"SELECT b.Status FROM gh.Beds b WITH(UPDLOCK,HOLDLOCK) JOIN gh.Rooms r ON r.RoomId=b.RoomId JOIN gh.Wards w ON w.WardId=r.WardId WHERE b.BedId=@B AND r.IsActive=1 AND w.IsActive=1",P("@B",bed)))!="Available")throw new InvalidOperationException("Destination bed is not available.");
                Execute(c,t,"UPDATE gh.Beds SET Status='Cleaning' WHERE BedId=@B",P("@B",a["BedId"]));
                Execute(c,t,"UPDATE gh.Beds SET Status='Occupied' WHERE BedId=@B",P("@B",bed));
                Execute(c,t,"UPDATE gh.Admissions SET BedId=@B,Status='Transferred' WHERE AdmissionId=@Id",P("@B",bed),P("@Id",id));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,NewStatus,Detail) VALUES('Admission',@Id,@U,'Transferred',@Detail)",P("@Id",id),P("@U",actor),P("@Detail","Bed "+Convert.ToString(a["BedId"])+" → "+bed));
                QueuePatientNotification(c,t,Convert.ToInt32(a["PatientId"]),"Admission","Bed transfer","Your care team has updated your bed assignment.");
                return 0;
            });
        }
        public void ReadyBed(int bed,int actor)
        {
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Admin");
                var affected=Execute(c,t,"UPDATE gh.Beds SET Status='Available' WHERE BedId=@B AND Status='Cleaning' AND NOT EXISTS(SELECT 1 FROM gh.Admissions WHERE BedId=@B AND DischargedAt IS NULL)",P("@B",bed));
                if(affected!=1)throw new InvalidOperationException("Only an unoccupied bed awaiting cleaning can be marked ready.");
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus) VALUES('Bed',@B,@U,'Cleaning','Available')",P("@B",bed),P("@U",actor));
                return 0;
            });
        }
        public int SaveSupplier(int? supplierId,string name,string contact,string phone,string email,int actor = 0)
        {
            if(string.IsNullOrWhiteSpace(name)||name.Length>160)throw new ArgumentException("Enter a supplier name of up to 160 characters.");
            return database.InTransaction((c,t)=>{
            actor=RequireActor(c,t,actor,"Pharmacist");
            if(supplierId.HasValue){
                if(Execute(c,t,"UPDATE gh.Suppliers SET SupplierName=@N,ContactName=@C,Phone=@P,Email=@E WHERE SupplierId=@Id",P("@N",name),P("@C",contact),P("@P",phone),P("@E",email),P("@Id",supplierId))!=1)throw new InvalidOperationException("Supplier not found.");
                History(c,t,"Supplier",supplierId.Value,actor,"Updated",name);
                return supplierId.Value;
            }
            int id=Convert.ToInt32(Scalar(c,t,"INSERT INTO gh.Suppliers(SupplierName,ContactName,Phone,Email) VALUES(@N,@C,@P,@E);SELECT CAST(SCOPE_IDENTITY() AS int);",P("@N",name),P("@C",contact),P("@P",phone),P("@E",email)));
            History(c,t,"Supplier",id,actor,"Created",name);return id;
            });
        }
        public void SetSupplierActive(int id,bool active,int actor)
        {
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Pharmacist");
                if(Execute(c,t,"UPDATE gh.Suppliers SET IsActive=@A WHERE SupplierId=@I AND IsActive<>@A",P("@A",active),P("@I",id))!=1)throw new InvalidOperationException("Supplier not found or already in that state.");
                History(c,t,"Supplier",id,actor,active?"Active":"Inactive","Existing batches and receipts retained");return 0;
            });
        }
        public void TransitionAppointment(long id, string target, int actor, int? patientScope = null,DateTime? preferredStart=null,DateTime? preferredEnd=null,string requestNote=null,DateTime? expectedStart=null,string expectedStatus=null,DateTime? expectedEnd=null)
        {
            string detail=null;
            if(target=="RescheduleRequested"){
                if(!preferredStart.HasValue||!preferredEnd.HasValue||preferredStart<=DateTime.UtcNow||preferredEnd<=preferredStart||preferredStart.Value.Date!=preferredEnd.Value.Date)
                    throw new ArgumentException("Choose a future preferred appointment window within one day.");
                if((requestNote??"").Length>700)throw new ArgumentException("Keep the request note within 700 characters.");
                detail="Requested UTC window: "+preferredStart.Value.ToString("yyyy-MM-dd HH:mm")+" to "+preferredEnd.Value.ToString("yyyy-MM-dd HH:mm")+". "+requestNote;
            }
            database.InTransaction((c,t) => {
                RequireActor(c,t,actor,"Receptionist","Patient");
                var caller=QueryOne(c,t,"SELECT RoleName,PatientId FROM gh.UserAccounts WHERE UserId=@U",P("@U",actor));
                if(Convert.ToString(caller["RoleName"])=="Patient")patientScope=Convert.ToInt32(caller["PatientId"]);
                var r=QueryOne(c,t,"SELECT PatientId,DoctorId,Status,ScheduledStart,ScheduledEnd FROM gh.Appointments WITH(UPDLOCK,HOLDLOCK) WHERE AppointmentId=@Id",P("@Id",id));
                if(r==null || (patientScope.HasValue && Convert.ToInt32(r["PatientId"])!=patientScope.Value)) throw new InvalidOperationException("Appointment not found.");
                string before=Convert.ToString(r["Status"]);
                var scheduled=Convert.ToDateTime(r["ScheduledStart"]);
                if((expectedStart.HasValue&&scheduled!=expectedStart.Value)||(expectedStatus!=null&&before!=expectedStatus)||(expectedEnd.HasValue&&Convert.ToDateTime(r["ScheduledEnd"])!=expectedEnd.Value))throw new InvalidOperationException("This appointment changed since you opened it. Review the current details before trying again.");
                if(target=="CheckedIn"&&scheduled.Date!=DateTime.UtcNow.Date)throw new InvalidOperationException("Only appointments scheduled for today (UTC) can be checked in. Reschedule first if the appointment date changed.");
                if(target=="CheckedIn"&&Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM dbo.patientTbl p CROSS JOIN dbo.doctorTbl d WHERE p.patientId=@P AND p.IsActive=1 AND d.doctorID=@D AND d.doctorStatus='Active'",P("@P",r["PatientId"]),P("@D",r["DoctorId"])))!=1)throw new InvalidOperationException("Check-in requires an active patient and clinician.");
                if(patientScope.HasValue&&scheduled<=DateTime.UtcNow)throw new InvalidOperationException("This appointment has already started. Contact reception for changes.");
                bool booked=before=="Booked"||before=="Confirmed"||before=="RescheduleRequested";
                bool allowed=(target=="Confirmed"&&before=="Booked")||(target=="CheckedIn"&&booked)||(target=="InProgress"&&before=="CheckedIn")||(target=="Cancelled"&&booked)||(target=="NoShow"&&booked&&Convert.ToDateTime(r["ScheduledStart"])<DateTime.UtcNow)||(target=="RescheduleRequested"&&(before=="Booked"||before=="Confirmed"));
                if(patientScope.HasValue && target!="Cancelled" && target!="RescheduleRequested") allowed=false;
                if(!allowed)throw new InvalidOperationException("This appointment cannot move from "+before+" to "+target+".");
                Execute(c,t,"UPDATE gh.Appointments SET Status=@S,UpdatedAt=SYSUTCDATETIME(),CheckInAt=CASE WHEN @S='CheckedIn' THEN SYSUTCDATETIME() ELSE CheckInAt END WHERE AppointmentId=@Id",P("@S",target),P("@Id",id));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus,Detail) VALUES('Appointment',@Id,@Actor,@Old,@New,@Detail)",P("@Id",id),P("@Actor",actor),P("@Old",before),P("@New",target),P("@Detail",detail));
                if(target=="CheckedIn")Execute(c,t,"INSERT INTO gh.Notifications(UserId,PatientId,NotificationType,Subject,Message,SentAt) SELECT UserId,@P,'Appointment','Patient checked in','A patient is waiting in your consultation queue.',SYSUTCDATETIME() FROM gh.UserAccounts WHERE RoleName='Doctor' AND LegacyUserId=@D AND IsActive=1",P("@P",r["PatientId"]),P("@D",r["DoctorId"]));
                if(patientScope.HasValue)Execute(c,t,"INSERT INTO gh.Notifications(UserId,PatientId,NotificationType,Subject,Message,SentAt) SELECT UserId,@P,'Appointment','Patient appointment update',@M,SYSUTCDATETIME() FROM gh.UserAccounts WHERE RoleName='Receptionist' AND IsActive=1",P("@P",patientScope),P("@M","Appointment "+id+": "+target+". "+detail));
                QueuePatientNotification(c,t,Convert.ToInt32(r["PatientId"]),"Appointment","Appointment update","Your appointment status is now "+target+".");
                return 0;
            });
        }
        public void RescheduleAppointment(long id, DateTime start, DateTime end, int actor)
        {
            if(start<=DateTime.UtcNow || end<=start || start.Date!=end.Date || start.Ticks%TimeSpan.TicksPerMinute!=0 || end.Ticks%TimeSpan.TicksPerMinute!=0)throw new ArgumentException("Choose a future appointment within one day, using whole minutes.");
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Receptionist");
                var r=QueryOne(c,t,"SELECT PatientId,DoctorId,Status FROM gh.Appointments WITH(UPDLOCK,HOLDLOCK) WHERE AppointmentId=@Id",P("@Id",id));
                if(r==null || !(new[]{"Booked","Confirmed","RescheduleRequested"}).ContainsValue(Convert.ToString(r["Status"])))throw new InvalidOperationException("Appointment cannot be rescheduled.");
                if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM dbo.patientTbl p CROSS JOIN dbo.doctorTbl d WHERE p.patientId=@P AND p.IsActive=1 AND d.doctorID=@D AND d.doctorStatus='Active'",P("@P",r["PatientId"]),P("@D",r["DoctorId"])))!=1)throw new InvalidOperationException("Rescheduling requires an active patient and clinician.");
                if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.Appointments WITH(UPDLOCK,HOLDLOCK) WHERE PatientId=@P AND AppointmentId<>@Id AND Status NOT IN('Cancelled','NoShow') AND @Start<ScheduledEnd AND @End>ScheduledStart",P("@P",r["PatientId"]),P("@Id",id),P("@Start",start),P("@End",end)))>0)throw new InvalidOperationException("This patient already has an overlapping appointment.");
                if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.Appointments WITH(UPDLOCK,HOLDLOCK) WHERE DoctorId=@D AND AppointmentId<>@Id AND Status NOT IN('Cancelled','NoShow') AND @Start<ScheduledEnd AND @End>ScheduledStart",P("@D",r["DoctorId"]),P("@Id",id),P("@Start",start),P("@End",end)))>0)throw new InvalidOperationException("That clinician is already booked.");
                if(Convert.ToInt32(Scalar(c,t,@"SELECT COUNT(*) FROM gh.DoctorSchedules WITH(UPDLOCK,HOLDLOCK) WHERE DoctorId=@D AND ScheduleDate=CAST(@Start AS date) AND IsAvailable=1 AND CAST(@Start AS time)>=StartTime AND CAST(@End AS time)<=EndTime AND DATEDIFF(minute,StartTime,CAST(@Start AS time))%SlotMinutes=0 AND DATEDIFF(minute,@Start,@End)%SlotMinutes=0",P("@D",r["DoctorId"]),P("@Start",start),P("@End",end)))==0)throw new InvalidOperationException("Choose a published availability window aligned to its slot length.");
                Execute(c,t,"UPDATE gh.Appointments SET ScheduledStart=@Start,ScheduledEnd=@End,Status='Booked',UpdatedAt=SYSUTCDATETIME() WHERE AppointmentId=@Id",P("@Start",start),P("@End",end),P("@Id",id));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus,Detail) VALUES('Appointment',@Id,@Actor,@Old,'Booked',@Detail)",P("@Id",id),P("@Actor",actor),P("@Old",r["Status"]),P("@Detail","Rescheduled to "+start.ToString("s")));
                QueuePatientNotification(c,t,Convert.ToInt32(r["PatientId"]),"Appointment","Appointment rescheduled","Your new appointment starts "+HospitalTime.Format(start)+" ("+HospitalTime.Zone.Id+").");
                return 0;
            });
        }
        public void DeclineRescheduleRequest(long id,string reason,int actor)
        {
            if(string.IsNullOrWhiteSpace(reason)||reason.Length>700)throw new ArgumentException("Enter a reason of up to 700 characters.");
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Receptionist");
                var patient=Scalar(c,t,"SELECT PatientId FROM gh.Appointments WITH(UPDLOCK,HOLDLOCK) WHERE AppointmentId=@I AND Status='RescheduleRequested'",P("@I",id));
                if(patient==null)throw new InvalidOperationException("Only a pending rescheduling request can be declined.");
                var before=Convert.ToString(Scalar(c,t,"SELECT TOP 1 PreviousStatus FROM gh.WorkflowHistory WHERE EntityType='Appointment' AND EntityId=@I AND NewStatus='RescheduleRequested' ORDER BY HistoryId DESC",P("@I",id)));
                string restored=before=="Confirmed"?"Confirmed":"Booked";
                Execute(c,t,"UPDATE gh.Appointments SET Status=@S,UpdatedAt=SYSUTCDATETIME() WHERE AppointmentId=@I",P("@S",restored),P("@I",id));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus,Detail) VALUES('Appointment',@I,@U,'RescheduleRequested',@S,@D)",P("@I",id),P("@U",actor),P("@S",restored),P("@D","Reschedule declined: "+reason));
                QueuePatientNotification(c,t,Convert.ToInt32(patient),"Appointment","Rescheduling request declined","Your original appointment remains reserved. Reception's note: "+reason);
                return 0;
            });
        }
        public void ProgressLab(long id, string target, int actor)
        {
            if(target!="Collected"&&target!="Processing")throw new ArgumentException("Select a valid laboratory stage.");
            database.InTransaction((c,t)=>{
                RequireActor(c,t,actor,"Laboratorian");
                var before=Convert.ToString(Scalar(c,t,"SELECT Status FROM gh.LabOrders WITH(UPDLOCK,HOLDLOCK) WHERE LabOrderId=@Id",P("@Id",id)));
                if((target=="Collected"&&before!="Ordered")||(target=="Processing"&&before!="Collected"))throw new InvalidOperationException("The order is not ready for that stage.");
                Execute(c,t,"UPDATE gh.LabOrders SET Status=@S,CollectedAt=CASE WHEN @S='Collected' THEN SYSUTCDATETIME() ELSE CollectedAt END WHERE LabOrderId=@Id",P("@S",target),P("@Id",id));
                if(target=="Processing")Execute(c,t,"UPDATE gh.LabOrderItems SET ResultStatus='Processing' WHERE LabOrderId=@Id AND ResultStatus='Pending'",P("@Id",id));
                Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus) VALUES('LabOrder',@Id,@Actor,@Old,@New)",P("@Id",id),P("@Actor",actor),P("@Old",before),P("@New",target));
                return 0;
            });
        }
    }
    internal static class WorkflowValues
    {
        internal static bool ContainsValue(this string[] values,string value){return Array.IndexOf(values,value)>=0;}
    }
}
