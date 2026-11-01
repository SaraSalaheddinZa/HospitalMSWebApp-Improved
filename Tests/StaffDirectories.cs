using System;
using System.Transactions;
using System.Reflection;
using System.Web.UI;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
class StaffDirectories
{
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    static StaffDirectoryEntry Entry(string role){string code="QA-"+Guid.NewGuid().ToString("N").Substring(0,12);return new StaffDirectoryEntry{Role=role,Name=code,Email=code+"@example.test",Phone="",Address="",Gender="Other",Status="Active",Password="FixtureOnly123!",Specialty="QA",Experience=3,BirthDate=new DateTime(1990,1,1)};}
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        var db=new Database();var service=new HospitalWorkflowService();
        int admin=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.admin@guardianhealth.test'")),nurse=Convert.ToInt32(db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.nurse@guardianhealth.test'"));
        foreach(string role in new[]{"Doctor","Laboratorian","Receptionist"})
        using(var fixture=new TransactionScope()){
            var input=Entry(role);int id=service.SaveStaffDirectoryEntry(input,admin);input.Id=id;
            var user=AuthenticationService.Authenticate(input.Email,input.Password,role);
            Assert(user!=null&&user.RoleEntityId==id,role+" creation provisions matching secure sign-in immediately");
            string hash=Convert.ToString(db.Scalar("SELECT PasswordHash FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",user.UserId)));
            int version=Convert.ToInt32(db.Scalar("SELECT AuthVersion FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",user.UserId)));
            input.Password="";input.Name+=" Updated";input.Email="u-"+input.Email;input.ConfirmEmailChange=true;
            service.SaveStaffDirectoryEntry(input,admin);
            var updated=db.Query("SELECT * FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",user.UserId)).Rows[0];
            Assert(Convert.ToString(updated["DisplayName"])==input.Name&&string.Equals(Convert.ToString(updated["Email"]),input.Email,StringComparison.OrdinalIgnoreCase),role+" profile changes update unified identity atomically");
            Assert(Convert.ToString(updated["PasswordHash"])==hash,role+" blank password preserves existing credential");
            Assert(Convert.ToInt32(updated["UserId"])==user.UserId&&Convert.ToString(updated["RoleName"])==role&&Convert.ToInt32(updated["LegacyUserId"])==id&&Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts WHERE RoleName=@R AND LegacyUserId=@I",Database.Parameter("@R",role),Database.Parameter("@I",id)))==1,role+" updates preserve one linked account and its identity/role");
            Assert(Convert.ToInt32(updated["AuthVersion"])>version,role+" login-name change invalidates prior tickets");
            db.Execute("UPDATE gh.UserAccounts SET FailedLoginCount=5,LockedUntil=DATEADD(hour,1,SYSUTCDATETIME()) WHERE UserId=@U",Database.Parameter("@U",user.UserId));
            service.SaveStaffDirectoryEntry(input,admin);
            Assert(Convert.ToInt32(db.Scalar("SELECT FailedLoginCount FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",user.UserId)))==5&&AuthenticationService.Authenticate(input.Email,"FixtureOnly123!",role)==null,role+" ordinary profile update does not bypass lockout");
            input.Password="FixtureReplaced123!";input.ConfirmPasswordChange=true;service.SaveStaffDirectoryEntry(input,admin);
            Assert(Convert.ToInt32(db.Scalar("SELECT FailedLoginCount FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",user.UserId)))==5&&AuthenticationService.Authenticate(input.Email,input.Password,role)==null,role+" password change does not bypass an existing lockout");
            db.Execute("UPDATE gh.UserAccounts SET LockedUntil=DATEADD(minute,-1,SYSUTCDATETIME()) WHERE UserId=@U",Database.Parameter("@U",user.UserId));
            Assert(AuthenticationService.Authenticate(input.Email,input.Password,role)!=null,role+" explicit password replacement updates sign-in coherently");
            service.SetDirectoryStaffActive(role,id,false,admin);
            Assert(AuthenticationService.Authenticate(input.Email,input.Password,role)==null,role+" deactivation blocks all authentication paths");
            input.Status="Inactive";input.Password="";service.SaveStaffDirectoryEntry(input,admin);
            Assert(!Convert.ToBoolean(db.Scalar("SELECT IsActive FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",user.UserId))),role+" inactive profile maintenance preserves disabled sign-in state");
            service.SetDirectoryStaffActive(role,id,true,admin);
            Assert(AuthenticationService.Authenticate(input.Email,"FixtureReplaced123!",role)!=null,role+" explicit reactivation restores the preserved account");
            Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.WorkflowHistory WHERE EntityType=@R AND EntityId=@I AND ActorUserId=@A",Database.Parameter("@R",role+"Directory"),Database.Parameter("@I",id),Database.Parameter("@A",admin)))==7,role+" directory changes have transactional actor/history records");
        }
        foreach(string scenario in new[]{"role","status-role","missing-record","unknown-role","duplicate-email","cross-directory-email","bad-email","long-name","weak-password","doctor-dob","doctor-experience","compatibility-mismatch","deactivate-replay","unconfirmed-email","unconfirmed-password","disabled-account-profile","disabled-compatibility-sync"})
        using(var fixture=new TransactionScope()){
            var input=Entry("Doctor");int id=service.SaveStaffDirectoryEntry(input,admin);input.Id=id;input.Password="";
            if(scenario=="duplicate-email"){input.Email="dev.admin@guardianhealth.test";input.ConfirmEmailChange=true;}
            if(scenario=="cross-directory-email"){
                var other=Entry("Laboratorian");service.SaveStaffDirectoryEntry(other,admin);input.Email=other.Email;input.ConfirmEmailChange=true;
            }
            if(scenario=="bad-email")input.Email="invalid";
            if(scenario=="long-name")input.Name=new string('x',51);
            if(scenario=="weak-password"){input.Password="short";input.ConfirmPasswordChange=true;}
            if(scenario=="unconfirmed-password")input.Password="Unconfirmed123!";
            if(scenario=="doctor-dob")input.BirthDate=DateTime.UtcNow.AddDays(1);
            if(scenario=="doctor-experience")input.Experience=-1;
            if(scenario=="unknown-role")input.Role="Admin";
            if(scenario=="missing-record")input.Id=int.MaxValue;
            if(scenario=="unconfirmed-email")input.Email="u-"+input.Email;
            if(scenario=="disabled-account-profile"||scenario=="disabled-compatibility-sync")db.Execute("UPDATE gh.UserAccounts SET IsActive=0 WHERE RoleName='Doctor' AND LegacyUserId=@I",Database.Parameter("@I",id));
            if(scenario=="deactivate-replay")service.SetDirectoryStaffActive("Doctor",id,false,admin);
            bool rejected=false;
            try{
                if(scenario=="compatibility-mismatch")service.SynchronizeLegacyAccount("Doctor",id,"Forged name",input.Email,null,true,admin);
                else if(scenario=="disabled-compatibility-sync")service.SynchronizeLegacyAccount("Doctor",id,input.Name,input.Email,null,true,admin);
                else if(scenario=="status-role"||scenario=="deactivate-replay")service.SetDirectoryStaffActive("Doctor",id,false,scenario=="status-role"?nurse:admin);
                else service.SaveStaffDirectoryEntry(input,scenario=="role"?nurse:admin);
            }catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}
            Assert(rejected,"Staff directory guard: "+scenario);
        }
        var failing=Entry("Laboratorian");string rollbackEmail=failing.Email;
        using(var fixture=new TransactionScope()){
            failing.Id=Convert.ToInt32(db.Scalar("INSERT INTO dbo.laboratorianTbl(labName,labEmail,labPassword,labPhone,labAddress,labGender,labStatus) VALUES(@N,@E,'legacy-fixture','','','Other','Active');SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@N",failing.Name),Database.Parameter("@E",failing.Email)));
            failing.Password="";failing.Name+=" Changed";
            bool rejected=false;
            try{service.SaveStaffDirectoryEntry(failing,admin);}catch(InvalidOperationException){rejected=true;}
            Assert(rejected,"Failure to provision secure access after directory write aborts the staff transaction");
        }
        Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM dbo.laboratorianTbl WHERE labEmail=@E",Database.Parameter("@E",rollbackEmail)))==0&&Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts WHERE Email=@E",Database.Parameter("@E",rollbackEmail)))==0,"Failed staff synchronization and fixture roll back without an orphan directory or account");
        foreach(var row in new[]{new[]{"HospitalMSWebApp.Views.Admin.Doctors","selectedDoctorId"},new[]{"HospitalMSWebApp.Views.Admin.Laboratorian","selectedLabId"},new[]{"HospitalMSWebApp.Views.Admin.Receptionist","selectedRecId"}}){
            var type=typeof(AppPage).Assembly.GetType(row[0]);var page=Activator.CreateInstance(type);
            var selected=type.GetProperty(row[1],BindingFlags.Instance|BindingFlags.NonPublic);selected.SetValue(page,123,null);
            var state=(StateBag)typeof(Control).GetProperty("ViewState",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(page,null);
            Assert(Convert.ToInt32(state[row[1]])==123,"Selected staff identity is retained in protected ViewState: "+type.Name);
        }
        Console.WriteLine("All staff directory fixtures roll back; existing accounts are not edited.");
    }
}
