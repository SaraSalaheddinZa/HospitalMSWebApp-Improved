using System;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
class AuthenticationGuards
{
    const string LegacyPassword="FixtureLegacy123!",CurrentPassword="FixtureCurrent123!";
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        var db=new Database();
        foreach(string scenario in new[]{"locked","disabled","old-password","renamed-account","new-legacy","valid-current","lockout"})
        using(var fixture=new TransactionScope()){
            string email="qa-auth-"+Guid.NewGuid().ToString("N").Substring(0,12)+"@example.test";
            int legacy=Convert.ToInt32(db.Scalar("INSERT INTO dbo.laboratorianTbl(labName,labEmail,labPassword,labPhone,labAddress,labGender,labStatus) VALUES('QA authentication',@E,@P,'','','Other','Active');SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@E",email),Database.Parameter("@P",PasswordSecurity.Hash(LegacyPassword))));
            string hash=PasswordSecurity.Hash(CurrentPassword);int account=0;
            if(scenario!="new-legacy")account=Convert.ToInt32(db.Scalar("INSERT INTO gh.UserAccounts(LegacyUserId,RoleName,DisplayName,Email,NormalizedEmail,PasswordHash,IsActive,LockedUntil) VALUES(@L,'Laboratorian','QA authentication',@E,UPPER(@E),@H,@A,@K);SELECT CAST(SCOPE_IDENTITY() AS int)",Database.Parameter("@L",legacy),Database.Parameter("@E",scenario=="renamed-account"?"renamed-"+email:email),Database.Parameter("@H",hash),Database.Parameter("@A",scenario!="disabled"),Database.Parameter("@K",scenario=="locked"?(object)DateTime.UtcNow.AddMinutes(15):null)));
            if(scenario=="lockout"){
                for(int n=0;n<5;n++)Assert(AuthenticationService.Authenticate(email,"Incorrect", "Laboratorian")==null,"Incorrect password is rejected");
                Assert(AuthenticationService.Authenticate(email,CurrentPassword,"Laboratorian")==null,"Locked account rejects its current password");
            }
            string password=scenario=="valid-current"?CurrentPassword:LegacyPassword;
            var user=AuthenticationService.Authenticate(email,password,"Laboratorian");
            Assert((user!=null)==(scenario=="new-legacy"||scenario=="valid-current"),"Authentication boundary: "+scenario);
            if(account>0){
                Assert(Convert.ToString(db.Scalar("SELECT PasswordHash FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",account)))==hash,"Legacy fallback does not overwrite the unified account credential");
                Assert(Convert.ToBoolean(db.Scalar("SELECT IsActive FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",account)))==(scenario!="disabled"),"Legacy fallback preserves account activation state");
            }else Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts WHERE LegacyUserId=@L AND RoleName='Laboratorian'",Database.Parameter("@L",legacy)))==1,"Unmigrated legacy account is provisioned once");
        }
        foreach(string malformed in new[]{"PBKDF2$0$AAAAAAAAAAA=$AAAAAAAAAAA=","PBKDF2$1$AA==$AA==","PBKDF2$-1$AAAAAAAAAAA=$AAAAAAAAAAA=","PBKDF2$100000$$","PBKDF2$100000$invalid$invalid"})Assert(!PasswordSecurity.Verify("test",malformed),"Malformed PBKDF2 values fail closed without throwing");
        string stored=PasswordSecurity.Hash(LegacyPassword);
        Assert(!PasswordSecurity.VerifyLegacy(stored,stored),"A PBKDF2 hash string is never accepted as a plaintext legacy password");
        VerifySeederPreservation(db);
        Console.WriteLine("All authentication fixtures roll back; development credentials were not modified.");
    }
    static void VerifySeederPreservation(Database db)
    {
        var settings=System.Configuration.ConfigurationManager.AppSettings;
        var readOnly=typeof(System.Collections.Specialized.NameObjectCollectionBase).GetProperty("IsReadOnly",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        bool wasReadOnly=(bool)readOnly.GetValue(settings,null);
        string environment=settings["EnvironmentName"],enabled=settings["SeedDevelopmentData"];
        int settingCount=settings.Count;
        string connection=Environment.GetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING");
        try {
            // No writable AppSettings/ConfigurationElementCollection hacks: the actual startup
            // gate accepts explicit settings internally, while the public entry reads config.
            // An invalid connection guarantees disabled gates cannot accidentally touch SQL.
            Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING","UnsupportedGuardianTestKey=1");
            foreach(var gate in new[]{new[]{"Production","true"},new[]{"Development","false"},new[]{"Development",(string)null},new[]{(string)null,"true"}}){
                SeedWithSettings(gate[0],gate[1]);
                Assert(true,"Seeder stays disabled without BOTH explicit development and enabled settings: "+(gate[0]??"missing environment")+" / "+(gate[1]??"missing flag"));
            }
            bool failed=false;
            try{SeedWithSettings("Development","true");}catch(InvalidOperationException){failed=true;}
            Assert(failed,"Explicitly enabled development bootstrap reaches SQL and fails safely on invalid configuration");
        } finally {
            Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",connection);
        }
        Assert(Environment.GetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING")==connection&&settings["EnvironmentName"]==environment&&settings["SeedDevelopmentData"]==enabled&&settings.Count==settingCount&&(bool)readOnly.GetValue(settings,null)==wasReadOnly,"Seeder verification restores connection configuration after failure and never changes application settings or read-only state");
            using(var fixture=new TransactionScope()){
                string email="qa-auth-renamed-"+Guid.NewGuid().ToString("N")+"@example.test";
                db.Execute("INSERT INTO gh.UserAccounts(RoleName,DisplayName,Email,NormalizedEmail,PasswordHash,IsActive,FailedLoginCount,LockedUntil) VALUES('Laboratorian','QA renamed identity',@E,UPPER(@E),@H,0,5,DATEADD(hour,1,SYSUTCDATETIME()))",Database.Parameter("@E",email),Database.Parameter("@H",PasswordSecurity.Hash(CurrentPassword)));
                db.Execute("UPDATE gh.UserAccounts SET Email='renamed-'+Email,NormalizedEmail=UPPER('renamed-'+Email),AuthVersion=AuthVersion+1 WHERE Email=@E",Database.Parameter("@E",email));
                email="renamed-"+email;
                string before=SeederSnapshot(db);
                SeedWithSettings("Development","true");
                Assert(before==SeederSnapshot(db),"Enabled development seeding leaves a populated account store and linked profiles completely unchanged");
                Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts WHERE Email=@E AND IsActive=0 AND FailedLoginCount=5 AND LockedUntil>SYSUTCDATETIME()",Database.Parameter("@E",email)))==1,"Startup preserves a renamed disabled/locked fixture identity without restoring seed access");
            }
    }
    static void SeedWithSettings(string environment,string enabled)
    {
        var method=typeof(DevelopmentDataSeeder).GetMethod("EnsureDevelopmentAccounts",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic,null,new[]{typeof(string),typeof(string)},null);
        try{method.Invoke(null,new object[]{environment,enabled});}
        catch(System.Reflection.TargetInvocationException ex){throw ex.InnerException;}
    }
    static string SeederSnapshot(Database db)
    {
        var snapshot=new System.Text.StringBuilder();
        foreach(string table in new[]{"gh.UserAccounts","gh.StaffMembers","dbo.doctorTbl","dbo.laboratorianTbl","dbo.receptionistTbl","dbo.patientTbl"})
            snapshot.Append(Convert.ToString(db.Scalar("SELECT CONVERT(varchar(30),COUNT_BIG(*))+':'+ISNULL(CONVERT(varchar(30),CHECKSUM_AGG(BINARY_CHECKSUM(*))),'empty') FROM "+table))).Append('|');
        return snapshot.ToString();
    }
}
