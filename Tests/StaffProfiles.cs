using System;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;

class StaffProfiles
{
    static readonly Database Db=new Database();
    static readonly HospitalWorkflowService Service=new HospitalWorkflowService();
    static int Admin;
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    static int Create(string role,out string email){string code="QA-PROFILE-"+Guid.NewGuid().ToString("N").Substring(0,12);email=code.ToLowerInvariant()+"@example.test";return Service.CreateStaffAccount(code,role,code,email,"",null,"FixtureProfile123!",Admin);}
    static string Version(int id){return Convert.ToString(Service.GetStaffProfile(id,Admin).Rows[0]["ProfileVersion"]);}
    static string Snapshot()
    {
        var values=new System.Text.StringBuilder();
        foreach(string table in new[]{"UserAccounts","StaffMembers","WorkflowHistory","Notifications","PasswordResetTokens"})
            values.Append(Convert.ToString(Db.Scalar("SELECT CONVERT(varchar(30),COUNT_BIG(*))+':'+ISNULL(CONVERT(varchar(30),CHECKSUM_AGG(BINARY_CHECKSUM(*))),'empty') FROM gh."+table))).Append('|');
        return values.ToString();
    }
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        Admin=Convert.ToInt32(Db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.admin@guardianhealth.test'"));
        string before=Snapshot();
        foreach(string role in new[]{"Nurse","Pharmacist"})
        using(var fixture=new TransactionScope()){
            string email;int id=Create(role,out email);
            var original=Db.Query("SELECT * FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",id)).Rows[0];
            string staffNumber=Convert.ToString(Db.Scalar("SELECT StaffNumber FROM gh.StaffMembers WHERE StaffMemberId=@S",Database.Parameter("@S",original["StaffMemberId"])));
            int accountCount=Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts")),profileCount=Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.StaffMembers"));
            Db.Execute("UPDATE gh.UserAccounts SET FailedLoginCount=5,LockedUntil=DATEADD(hour,1,SYSUTCDATETIME()) WHERE UserId=@U",Database.Parameter("@U",id));
            Service.SetStaffActive(id,false,Admin);
            var locked=Db.Scalar("SELECT LockedUntil FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",id));
            int department=Convert.ToInt32(Db.Scalar("SELECT TOP(1) DepartmentId FROM gh.Departments WHERE IsActive=1 ORDER BY DepartmentId"));
            Service.UpdateStaffProfile(id,"Updated staff",email,"555-0190",department,false,Version(id),Admin);
            var updated=Db.Query("SELECT u.*,s.FullName,s.DepartmentId,s.Phone,s.IsActive AS StaffActive FROM gh.UserAccounts u JOIN gh.StaffMembers s ON s.StaffMemberId=u.StaffMemberId WHERE u.UserId=@U",Database.Parameter("@U",id)).Rows[0];
            Assert(Convert.ToString(updated["DisplayName"])=="Updated staff"&&Convert.ToString(updated["FullName"])=="Updated staff"&&Convert.ToString(updated["Phone"])=="555-0190"&&Convert.ToInt32(updated["DepartmentId"])==department,role+" profile and department update synchronizes both records");
            Assert(Equals(updated["PasswordHash"],original["PasswordHash"])&&Equals(updated["StaffMemberId"],original["StaffMemberId"])&&Equals(updated["RoleName"],original["RoleName"]),role+" profile update preserves password, staff identity and role");
            Assert(!Convert.ToBoolean(updated["IsActive"])&&!Convert.ToBoolean(updated["StaffActive"])&&Equals(updated["LockedUntil"],locked)&&Convert.ToInt32(updated["FailedLoginCount"])==5,role+" disabled/locked account remains disabled and locked after editing");
            int version=Convert.ToInt32(updated["AuthVersion"]);
            Service.UpdateStaffProfile(id,"Updated staff","u-"+email,"555-0190",null,true,Version(id),Admin);
            updated=Db.Query("SELECT * FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",id)).Rows[0];
            Assert(Convert.ToString(updated["Email"])=="u-"+email&&Convert.ToInt32(updated["AuthVersion"])==version+1,role+" explicit email change updates the same account and invalidates prior credentials version");
            Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.WorkflowHistory WHERE EntityType='UserAccount' AND EntityId=@U AND NewStatus='ProfileUpdated' AND ActorUserId=@A",Database.Parameter("@U",id),Database.Parameter("@A",Admin)))==5,role+" profile history captures each changed field and responsible administrator");
            Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.Notifications WHERE UserId=@U AND Subject='Staff profile updated'",Database.Parameter("@U",id)))==2,role+" profile updates notify only the affected staff member");
            Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts"))==accountCount&&Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.StaffMembers"))==profileCount&&Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts WHERE StaffMemberId=@S",Database.Parameter("@S",original["StaffMemberId"])))==1,role+" repeated profile updates create no duplicate accounts or profiles");
            Assert(Convert.ToString(Db.Scalar("SELECT StaffNumber FROM gh.StaffMembers WHERE StaffMemberId=@S",Database.Parameter("@S",original["StaffMemberId"])))==staffNumber&&Equals(updated["PasswordHash"],original["PasswordHash"])&&!Convert.ToBoolean(updated["IsActive"])&&Equals(updated["LockedUntil"],locked),role+" confirmed email change also preserves staff number, password, disabled state and lockout");
        }
        foreach(string role in new[]{"Doctor","Receptionist","Laboratorian","Nurse","Pharmacist","Patient"}){
            int actor=Convert.ToInt32(Db.Scalar("SELECT TOP(1) UserId FROM gh.UserAccounts WHERE RoleName=@R AND IsActive=1 ORDER BY UserId",Database.Parameter("@R",role)));
            bool readDenied=false,writeDenied=false;
            try{Service.GetStaffProfile(actor,actor);}catch(InvalidOperationException ex){readDenied=ex.Message.Contains("not authorized");}
            try{Service.UpdateStaffProfile(actor,"Never saved","never-saved@example.test","",null,false,"stale",actor);}catch(InvalidOperationException ex){writeDenied=ex.Message.Contains("not authorized");}
            Assert(readDenied&&writeDenied,role+" cannot read or update staff profiles through direct service calls");
        }
        foreach(string scenario in new[]{"read-role","write-role","unsupported-role","missing-account","unconfirmed-email","duplicate-account-email","duplicate-directory-email","invalid-email","long-phone","empty-name","missing-department","inactive-department","stale-profile","stale-access","stale-recovery","unchanged"})
        using(var fixture=new TransactionScope()){
            string email;int id=Create("Nurse",out email),target=id,actor=Admin;string version=Version(id),name="Changed name",phone="";int? department=null;bool confirm=false;
            if(scenario=="write-role"||scenario=="read-role")actor=id;
            if(scenario=="unsupported-role")target=Admin;
            if(scenario=="missing-account")target=int.MaxValue;
            if(scenario=="unconfirmed-email")email="u-"+email;
            if(scenario=="duplicate-account-email"){email="dev.admin@guardianhealth.test";confirm=true;}
            if(scenario=="duplicate-directory-email"){email=Convert.ToString(Db.Scalar("SELECT TOP(1) doctorEmail FROM dbo.doctorTbl WHERE doctorEmail IS NOT NULL"));confirm=true;}
            if(scenario=="invalid-email")email="not-an-email";
            if(scenario=="long-phone")phone=new string('5',31);
            if(scenario=="empty-name")name=" ";
            if(scenario=="missing-department")department=int.MaxValue;
            if(scenario=="inactive-department")department=Convert.ToInt32(Db.Scalar("INSERT INTO gh.Departments(DepartmentCode,DepartmentName,IsActive) VALUES(@C,'QA inactive',0);SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@C","QA-"+Guid.NewGuid().ToString("N").Substring(0,12))));
            if(scenario=="stale-profile")Service.UpdateStaffProfile(id,"First edit",email,"",null,false,version,Admin);
            if(scenario=="stale-access")Service.SetStaffActive(id,false,Admin);
            if(scenario=="stale-recovery"){var recovery=new PasswordRecoveryService();recovery.CompleteReset(recovery.RequestReset(email,"Nurse",null).Token,"FixtureRecovered123!",null);}
            if(scenario=="unchanged")name=Convert.ToString(Service.GetStaffProfile(id,Admin).Rows[0]["FullName"]);
            bool rejected=false;
            try{if(scenario=="read-role")Service.GetStaffProfile(id,actor);else Service.UpdateStaffProfile(target,name,email,phone,department,confirm,version,actor);}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}
            Assert(rejected,"Staff profile guard: "+scenario);
        }
        foreach(string scenario in new[]{"empty-name","invalid-email","long-number","long-phone","missing-department","duplicate-email","directory-email"})
        using(var fixture=new TransactionScope()){
            string code="QA-"+Guid.NewGuid().ToString("N").Substring(0,12);bool rejected=false;
            string email=scenario=="duplicate-email"?"dev.admin@guardianhealth.test":scenario=="directory-email"?Convert.ToString(Db.Scalar("SELECT TOP(1) labEmail FROM dbo.laboratorianTbl")):scenario=="invalid-email"?"invalid":code+"@example.test";
            try{Service.CreateStaffAccount(scenario=="long-number"?new string('x',31):code,"Nurse",scenario=="empty-name"?"":code,email,scenario=="long-phone"?new string('x',31):"",scenario=="missing-department"?(int?)int.MaxValue:null,"FixtureProfile123!",Admin);}catch(ArgumentException){rejected=true;}
            Assert(rejected,"Staff creation validation: "+scenario);
        }
        Assert(before==Snapshot(),"Successful edits and rejected updates roll back without staff, credential, history, notification or recovery-token residue");
        Console.WriteLine("All staff profile fixtures roll back; existing staff and account records are preserved.");
    }
}
