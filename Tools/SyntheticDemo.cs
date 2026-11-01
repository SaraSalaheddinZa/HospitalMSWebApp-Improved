using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
using HospitalMSWebApp.Models;

// Explicit console tool only. Not compiled into the web application or called at startup.
public static class SyntheticDemo
{
    public const string RunKey="guardian-synthetic-v1";
    static readonly Database Db=new Database();
    static readonly HospitalWorkflowService Work=new HospitalWorkflowService();
    sealed class Row { public string Table,Key,Hash;public long Id;public string Identity {get{return Table+":"+Id;}} }
    static SqlParameter P(string n,object v){return Database.Parameter(n,v);}
    static int I(string sql,params SqlParameter[] p){return Convert.ToInt32(Db.Scalar(sql,p));}
    static long L(string sql,params SqlParameter[] p){return Convert.ToInt64(Db.Scalar(sql,p));}
    public static void Guard()
    {
        if(System.Web.HttpContext.Current!=null || Environment.GetEnvironmentVariable("HOSPITALMS_DEMO_OPT_IN")!="SYNTHETIC_DEVELOPMENT_ONLY" || Environment.GetEnvironmentVariable("HOSPITALMS_ENVIRONMENT")!="Development")
            throw new InvalidOperationException("Synthetic setup is console-only and requires explicit Development and demo opt-in flags.");
        // Fail closed even if someone accidentally copies the opt-in flags to a release host.
        var b=new SqlConnectionStringBuilder(Functions.ConnectionString);
        if(!string.Equals(b.DataSource,@".\SQLEXP2017",StringComparison.OrdinalIgnoreCase)||b.InitialCatalog!="HospitalMS-Sara7593-Wasan7639"||!b.IntegratedSecurity)
            throw new InvalidOperationException("Demo setup is restricted to the existing local development database.");
    }
    static string Hash(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","");}
    static Dictionary<string,Row> Snapshot()
    {
        var result=new Dictionary<string,Row>();
        var tables=Db.Query(@"SELECT QUOTENAME(s.name)+'.'+QUOTENAME(t.name) T,MAX(c.name) K FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
            JOIN sys.indexes i ON i.object_id=t.object_id AND i.is_primary_key=1 JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE s.name IN('dbo','gh') AND t.name NOT IN('SyntheticDemoRuns','SyntheticDemoRecords','sysdiagrams')
            GROUP BY s.name,t.name HAVING COUNT(*)=1 AND MAX(TYPE_NAME(c.user_type_id)) IN('int','bigint') ORDER BY T");
        foreach(DataRow table in tables.Rows){
            string name=Convert.ToString(table["T"]),key=Convert.ToString(table["K"]);
            foreach(DataRow row in Db.Query("SELECT * FROM "+name+" WITH(HOLDLOCK)").Rows){
                var b=new StringBuilder();
                foreach(DataColumn col in row.Table.Columns){object value=row[col];string serialized=value==DBNull.Value?"NULL":value is byte[]?Convert.ToBase64String((byte[])value):value is DateTime?((DateTime)value).ToString("O",CultureInfo.InvariantCulture):Convert.ToString(value,CultureInfo.InvariantCulture);b.Append(col.ColumnName).Append(':').Append(serialized.Length).Append(':').Append(serialized).Append(';');}
                var entry=new Row{Table=name,Key=key,Id=Convert.ToInt64(row[key]),Hash=Hash(b.ToString())};result.Add(entry.Identity,entry);
            }
        }return result;
    }
    static string Aggregate(IEnumerable<Row> rows){return Hash(string.Join("|",rows.OrderBy(x=>x.Identity,StringComparer.Ordinal).Select(x=>x.Identity+":"+x.Hash)));}
    static void Lock(){Db.Execute("DECLARE @R int; EXEC @R=sys.sp_getapplock @Resource='GuardianSyntheticDemo',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @R<0 THROW 51240,'Another demo command is running.',1;");}
    public static string Preview()
    {
        Guard();var status=Db.Scalar("SELECT Status FROM gh.SyntheticDemoRuns WHERE RunKey=@R",P("@R",RunKey));
        return status==null?"Preview only: create 3 synthetic doctors, 1 nurse, 1 pharmacist, 1 receptionist, 1 laboratorian, 4 patients with portal accounts; 3 departments, ward/room/3 beds, published schedules, clinical/lab/pharmacy/admission/finance journeys. Existing records and credentials must remain byte-for-byte unchanged.":"Dataset already registered: "+status+". Seed will not recreate or reset any identity.";
    }
    public static bool Seed(string password)
    {
        Guard();
        using(var scope=new TransactionScope(TransactionScopeOption.Required,new TransactionOptions{IsolationLevel=System.Transactions.IsolationLevel.Serializable,Timeout=TimeSpan.FromMinutes(10)})){
            Lock();
            if(Db.Scalar("SELECT Status FROM gh.SyntheticDemoRuns WITH(UPDLOCK,HOLDLOCK) WHERE RunKey=@R",P("@R",RunKey))!=null){scope.Complete();return false;}
            string error;if(!PasswordSecurity.IsAcceptable(password,out error))throw new ArgumentException("Provide a strong synthetic account password through HOSPITALMS_DEMO_PASSWORD; it is never printed or stored in documentation.");
            if(I("SELECT COUNT(*) FROM gh.UserAccounts WHERE Email LIKE 'synthetic.%@guardianhealth.test'")>0||I("SELECT COUNT(*) FROM gh.Departments WHERE DepartmentCode LIKE 'SYN-%'")>0)
                throw new InvalidOperationException("Synthetic identifiers already exist without this manifest. Refusing to adopt or overwrite them.");
            var before=Snapshot();
            int admin=I("SELECT UserId FROM gh.UserAccounts WHERE RoleName='Admin' AND IsActive=1 AND Email='dev.admin@guardianhealth.test'");
            if(admin<1)throw new InvalidOperationException("An existing active development administrator is required.");
            DateTime anchor=Convert.ToDateTime(Db.Scalar("SELECT CAST(SYSUTCDATETIME() AS date)"));
            int medicine=Work.AddDepartment("SYN-MED","Synthetic General Medicine","Synthetic development only","",admin);
            int cardiology=Work.AddDepartment("SYN-CARD","Synthetic Cardiology","Synthetic development only","",admin);
            int pediatrics=Work.AddDepartment("SYN-PED","Synthetic Pediatrics","Synthetic development only","",admin);
            var doctors=new int[3];var doctorUsers=new int[3];
            string[] names={"Synthetic Dr Maya Reed","Synthetic Dr Omar Lane","Synthetic Dr Lina Park"},specs={"General Medicine","Cardiology","Pediatrics"};
            int[] depts={medicine,cardiology,pediatrics};
            for(int n=0;n<3;n++){
                doctors[n]=Work.SaveStaffDirectoryEntry(new StaffDirectoryEntry{Role="Doctor",Name=names[n],Email="synthetic.doctor"+(n+1)+"@guardianhealth.test",Phone="",Address="SYNTHETIC DEVELOPMENT DATA",Gender="Other",Status="Active",Password=password,Specialty=specs[n],Experience=0,BirthDate=new DateTime(1985,1,1)},admin);
                doctorUsers[n]=I("SELECT UserId FROM gh.UserAccounts WHERE RoleName='Doctor' AND LegacyUserId=@D",P("@D",doctors[n]));
                Work.SetDoctorDepartment(doctors[n],depts[n],admin);
                for(int day=1;day<=7;day++)Work.AddDoctorSchedule(doctors[n],depts[n],anchor.AddDays(day),TimeSpan.FromHours(9),TimeSpan.FromHours(12),20,admin);
            }
            int receptionLegacy=Work.SaveStaffDirectoryEntry(new StaffDirectoryEntry{Role="Receptionist",Name="Synthetic Reception Team",Email="synthetic.reception@guardianhealth.test",Phone="",Address="SYNTHETIC DEVELOPMENT DATA",Status="Active",Password=password},admin);
            int labLegacy=Work.SaveStaffDirectoryEntry(new StaffDirectoryEntry{Role="Laboratorian",Name="Synthetic Laboratory Team",Email="synthetic.lab@guardianhealth.test",Phone="",Address="SYNTHETIC DEVELOPMENT DATA",Gender="Other",Status="Active",Password=password},admin);
            int reception=I("SELECT UserId FROM gh.UserAccounts WHERE RoleName='Receptionist' AND LegacyUserId=@I",P("@I",receptionLegacy));
            int lab=I("SELECT UserId FROM gh.UserAccounts WHERE RoleName='Laboratorian' AND LegacyUserId=@I",P("@I",labLegacy));
            int nurse=Work.CreateStaffAccount("SYN-NURSE-01","Nurse","Synthetic Nursing Team","synthetic.nurse@guardianhealth.test","",medicine,password,admin);
            int pharmacist=Work.CreateStaffAccount("SYN-PHARM-01","Pharmacist","Synthetic Pharmacy Team","synthetic.pharmacist@guardianhealth.test","",medicine,password,admin);
            int[] patients=new int[4],portals=new int[4];string[] patientNames={"Synthetic Alex Brook","Synthetic Sam River","Synthetic Jamie Vale","Synthetic Robin Lake"};
            for(int n=0;n<4;n++){
                string email="synthetic.patient"+(n+1)+"@guardianhealth.test";
                patients[n]=Work.RegisterPatient(patientNames[n],"","Other",new DateTime(1990+n,1,1),"SYNTHETIC DEVELOPMENT DATA — not a real patient","Synthetic scenario: no known allergies",email,"","","","Synthetic demonstration only",receptionLegacy,reception);
                portals[n]=Work.CreatePatientAccount(patients[n],email,password,reception);
            }
            int med=I("INSERT INTO gh.MedicationCatalog(MedicationCode,MedicationName,Strength,Form) VALUES('SYN-TRAINING','Synthetic training medication','Not for clinical use','Training');SELECT CAST(SCOPE_IDENTITY() AS int)");
            int test=I("INSERT INTO gh.LabCatalog(TestCode,TestName,SpecimenType,ReferenceRange,Unit) VALUES('SYN-TRAINING','Synthetic demonstration assay','Training sample','Training range: 1–5','unit');SELECT CAST(SCOPE_IDENTITY() AS int)");
            int supplier=Work.SaveSupplier(null,"Synthetic Training Supplies","Synthetic development only","","synthetic.supplier@example.test",pharmacist);
            Work.ReceiveStock(med,"SYN-BATCH-01",anchor.AddMonths(6),40,10,2,supplier,pharmacist,"SYN-DELIVERY-01/1");
            Work.ReceiveStock(med,"SYN-BATCH-01",anchor.AddMonths(6),10,10,2,supplier,pharmacist,"SYN-DELIVERY-02/1");
            Work.ReceiveStock(med,"SYN-NEAR-EXPIRY",anchor.AddDays(20),3,5,2,supplier,pharmacist,"SYN-DELIVERY-03/1");
            long batch=L("SELECT MedicationBatchId FROM gh.MedicationBatches WHERE MedicationId=@M AND BatchNumber='SYN-BATCH-01'",P("@M",med));
            // Real recorded walk-in encounter; no fabricated historical timestamps or attendance.
            long encounter=Work.RecordEncounter(patients[0],doctors[0],null,"Synthetic workflow training","Synthetic assessment — not medical advice","Training-only follow-up plan",anchor.AddDays(7),37,72,120,80,"Synthetic demonstration diagnosis",med,"Training dose","Training frequency",2,test,doctorUsers[0]);
            long order=L("SELECT LabOrderId FROM gh.LabOrders WHERE EncounterId=@E",P("@E",encounter)),item=L("SELECT LabOrderItemId FROM gh.LabOrderItems WHERE LabOrderId=@O",P("@O",order));
            Work.ProgressLab(order,"Collected",lab);Work.ProgressLab(order,"Processing",lab);Work.FinalizeLabResult(item,"3","unit","Normal",lab);
            Work.ReviewLab(order,doctors[0],doctorUsers[0],I("SELECT ResultVersion FROM gh.LabOrders WHERE LabOrderId=@O",P("@O",order)));
            Work.FinalizeEncounter(encounter,doctors[0],doctorUsers[0]);
            long rx=L("SELECT i.PrescriptionItemId FROM gh.PrescriptionItems i JOIN gh.PrescriptionOrders o ON o.PrescriptionOrderId=i.PrescriptionOrderId WHERE o.EncounterId=@E",P("@E",encounter));
            Work.DispensePrescriptionItem(rx,batch,2,pharmacist);
            // A second real open encounter supplies the pending lab/dispensing worklists.
            Work.RecordEncounter(patients[1],doctors[1],null,"Synthetic pending-work scenario","Synthetic draft assessment","Synthetic draft plan",null,null,null,null,null,null,med,"Training dose","Training frequency",1,test,doctorUsers[1]);
            for(int n=0;n<3;n++)Work.BookAppointment(patients[n],doctors[n],anchor.AddDays(1).AddHours(9),anchor.AddDays(1).AddHours(9).AddMinutes(20),"SYNTHETIC DEVELOPMENT appointment",portals[n]);
            // Same-day attendance is seeded only when a valid future session still fits today.
            DateTime now=DateTime.UtcNow,start=new DateTime(now.Year,now.Month,now.Day,now.Hour,now.Minute,0).AddMinutes(5);
            if(start.Date==anchor&&start.AddMinutes(60).Date==anchor){
                Work.AddDoctorSchedule(doctors[0],medicine,anchor,start.TimeOfDay,start.AddMinutes(60).TimeOfDay,20,admin);
                long completed=Work.BookAppointment(patients[2],doctors[0],start,start.AddMinutes(20),"SYNTHETIC same-day consultation",reception);
                Work.TransitionAppointment(completed,"Confirmed",reception);Work.TransitionAppointment(completed,"CheckedIn",reception);
                long note=Work.RecordEncounter(patients[2],doctors[0],completed,"Synthetic attended consultation","Synthetic assessment","Synthetic plan",null,null,null,null,null,null,null,null,null,null,null,doctorUsers[0]);Work.FinalizeEncounter(note,doctors[0],doctorUsers[0]);
                long waiting=Work.BookAppointment(patients[3],doctors[0],start.AddMinutes(20),start.AddMinutes(40),"SYNTHETIC waiting for consultation",reception);Work.TransitionAppointment(waiting,"Confirmed",reception);Work.TransitionAppointment(waiting,"CheckedIn",reception);
            }
            int ward=Work.AddWard("Synthetic Observation Ward","General",medicine,admin),room=Work.AddRoom(ward,"SYN-101","General",admin);
            int bed1=Work.AddBed(room,"SYN-A",admin),bed2=Work.AddBed(room,"SYN-B",admin),bed3=Work.AddBed(room,"SYN-C",admin);
            long triage=Work.RecordTriage(patients[2],3,"Synthetic emergency handoff exercise",nurse);Work.ProgressTriage(triage,"UnderCare",nurse);
            long admission=Work.AdmitPatient(patients[2],bed1,doctors[0],"Synthetic observation",admin,triage);
            Work.RecordNursingNote(patients[2],admission,nurse,"Stable","Synthetic nursing assessment",37,72,120,80);
            Work.TransferAdmission(admission,bed2,admin);Work.ReadyBed(bed1,admin);Work.ProgressTriage(triage,"Resolved",nurse);
            Work.DischargePatient(admission,"Synthetic discharge summary",admin);Work.ReadyBed(bed2,admin);
            long current=Work.AdmitPatient(patients[3],bed3,doctors[0],"Synthetic inpatient workflow",admin);
            Work.RecordNursingNote(patients[3],current,nurse,"NeedsReview","Synthetic handoff for clinical review",37,72,120,80);
            int provider=Work.SaveInsuranceProvider(null,"SYN-INS","Synthetic Training Insurer","","",true,reception);
            for(int n=0;n<3;n++){
                long invoice=Work.CreateInvoice(patients[n],"Synthetic training consultation",100,anchor.AddDays(30),reception);Work.AddInvoiceLine(invoice,"Synthetic training service",1,25,reception);
                long policy=Work.AddPolicy(patients[n],provider,"SYN-POLICY-"+(n+1),80,anchor,anchor.AddYears(1),reception),claim=Work.SubmitClaim(invoice,policy,reception);
                if(n==0){Work.DecideClaim(claim,"UnderReview",0,admin,"Synthetic eligibility review");Work.DecideClaim(claim,"Approved",100,admin,"Synthetic approval");Work.DecideClaim(claim,"Paid",0,admin,"Synthetic settlement","SYN-SETTLE-01");Work.TakePayment(invoice,25,"Cash","SYN-PAYMENT-01",reception);}
                if(n==1)Work.DecideClaim(claim,"UnderReview",0,admin,"Synthetic pending review");
            }
            var after=Snapshot();
            foreach(var old in before)if(!after.ContainsKey(old.Key)||old.Value.Hash!=after[old.Key].Hash)throw new InvalidOperationException("Existing data changed during setup. Entire synthetic setup rolled back.");
            Db.Execute("INSERT INTO gh.SyntheticDemoRuns(RunKey,AnchorDate,Status,OutsideFingerprint) VALUES(@R,@D,'Ready',@H)",P("@R",RunKey),P("@D",anchor),P("@H",Aggregate(before.Values)));
            foreach(var row in after.Values.Where(x=>!before.ContainsKey(x.Identity)))Db.Execute("INSERT INTO gh.SyntheticDemoRecords(RunKey,TableName,KeyColumn,RecordId,Fingerprint) VALUES(@R,@T,@K,@I,@H)",P("@R",RunKey),P("@T",row.Table),P("@K",row.Key),P("@I",row.Id),P("@H",row.Hash));
            scope.Complete();return true;
        }
    }
    public static int Cleanup(bool preview)
    {
        Guard();
        using(var scope=new TransactionScope(TransactionScopeOption.Required,new TransactionOptions{IsolationLevel=System.Transactions.IsolationLevel.Serializable,Timeout=TimeSpan.FromMinutes(10)})){
            Lock();var run=Db.Query("SELECT Status,OutsideFingerprint FROM gh.SyntheticDemoRuns WHERE RunKey=@R",P("@R",RunKey));
            if(run.Rows.Count==0||Convert.ToString(run.Rows[0]["Status"])=="Cleaned"){scope.Complete();return 0;}
            var manifest=Db.Query("SELECT TableName,KeyColumn,RecordId,Fingerprint FROM gh.SyntheticDemoRecords WHERE RunKey=@R",P("@R",RunKey));
            var all=Snapshot();var owned=new List<Row>();
            foreach(DataRow record in manifest.Rows){
                string identity=record["TableName"]+":"+record["RecordId"];Row row;
                if(!all.TryGetValue(identity,out row)||row.Key!=Convert.ToString(record["KeyColumn"])||row.Hash!=Convert.ToString(record["Fingerprint"]))throw new InvalidOperationException("Demo records changed after setup. Cleanup refused; retain the history and request a dependency-reviewed cleanup. No rows removed.");
                owned.Add(row);all.Remove(identity);
            }
            if(Aggregate(all.Values)!=Convert.ToString(run.Rows[0]["OutsideFingerprint"]))throw new InvalidOperationException("Other database activity occurred after setup. Cleanup refused to protect untracked relationships. No rows removed.");
            if(!preview){
                var tables=new HashSet<string>(owned.Select(x=>x.Table));
                var dependencies=Db.Query("SELECT QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))+'.'+QUOTENAME(OBJECT_NAME(parent_object_id)) Child,QUOTENAME(OBJECT_SCHEMA_NAME(referenced_object_id))+'.'+QUOTENAME(OBJECT_NAME(referenced_object_id)) Parent FROM sys.foreign_keys");
                while(tables.Count>0){
                    string leaf=tables.FirstOrDefault(parent=>!dependencies.Rows.Cast<DataRow>().Any(d=>Convert.ToString(d["Parent"])==parent&&Convert.ToString(d["Child"])!=parent&&tables.Contains(Convert.ToString(d["Child"]))));
                    if(leaf==null)throw new InvalidOperationException("Cyclic dependency requires manual review; cleanup rolled back.");
                    foreach(var row in owned.Where(x=>x.Table==leaf))Db.Execute("DELETE FROM "+row.Table+" WHERE ["+row.Key+"]=@I",P("@I",row.Id));
                    tables.Remove(leaf);
                }
                Db.Execute("UPDATE gh.SyntheticDemoRuns SET Status='Cleaned' WHERE RunKey=@R",P("@R",RunKey));
                // Preserve tombstone and provenance: repeated seeding never resurrects removed/renamed users.
            }
            scope.Complete();return owned.Count;
        }
    }
    public static int Main(string[] args)
    {
        try{
            string action=args.Length==1?args[0]:"preview";
            if(action=="preview")Console.WriteLine(Preview());
            else if(action=="seed")Console.WriteLine(Seed(Environment.GetEnvironmentVariable("HOSPITALMS_DEMO_PASSWORD"))?"Synthetic dataset created; original records preserved.":"No changes: dataset already registered; identities and credentials preserved.");
            else if(action=="cleanup-preview"||action=="cleanup")Console.WriteLine((action=="cleanup"?"Removed only unchanged manifest-owned rows: ":"Verified eligible manifest-owned rows: ")+Cleanup(action!="cleanup"));
            else throw new ArgumentException("Use preview, seed, cleanup-preview or cleanup.");
            return 0;
        }catch(Exception ex){Console.Error.WriteLine("Demo command failed ("+ex.GetType().Name+"). No partial dataset is committed. "+(ex is ArgumentException||ex is InvalidOperationException?ex.Message:"Review local diagnostics; no credentials are printed."));return 1;}
    }
}
