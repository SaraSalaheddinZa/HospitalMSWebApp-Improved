using System;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
class SyntheticData
{
    static Database db=new Database();
    // Compare exact in-memory values without emitting credential material into test logs.
    static string Accounts()
    {
        var table=db.Query("SELECT UserId,LegacyUserId,StaffMemberId,PatientId,DisplayName,Email,NormalizedEmail,PasswordHash,RoleName,IsActive,AuthVersion,FailedLoginCount,LockedUntil FROM gh.UserAccounts ORDER BY UserId");
        table.TableName="Accounts";
        using(var writer=new System.IO.StringWriter()){table.WriteXml(writer);return writer.ToString();}
    }
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    static void Reject(Action action,string message){bool failed=false;try{action();}catch(InvalidOperationException){failed=true;}Assert(failed,message);}
    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        string opt=Environment.GetEnvironmentVariable("HOSPITALMS_DEMO_OPT_IN"),env=Environment.GetEnvironmentVariable("HOSPITALMS_ENVIRONMENT");
        try{
            Environment.SetEnvironmentVariable("HOSPITALMS_DEMO_OPT_IN",null);Reject(()=>SyntheticDemo.Preview(),"Demo opt-in is required even for preview");
            Environment.SetEnvironmentVariable("HOSPITALMS_DEMO_OPT_IN","SYNTHETIC_DEVELOPMENT_ONLY");Environment.SetEnvironmentVariable("HOSPITALMS_ENVIRONMENT","Production");Reject(()=>SyntheticDemo.Seed("UnusedTest123!"),"Production demo setup is disabled before any database write");
            Environment.SetEnvironmentVariable("HOSPITALMS_ENVIRONMENT","Development");
            int before=Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts"));
            string accountsBefore=Accounts();
            using(var tx=new TransactionScope(TransactionScopeOption.Required,TimeSpan.FromMinutes(10))){
                // Never remove a live manifest or recreate the persistent dataset. Any account
                // security-state fixture below remains inside this rollback-only transaction.
                if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.SyntheticDemoRuns"))>0){
                    Assert(!SyntheticDemo.Seed("IgnoredDifferent123!"),"Registered demo identities are never recreated or reset");
                    Assert(Accounts()==accountsBefore,"Idempotence preserves every account identity, credential and security state exactly");
                    if(Convert.ToString(db.Scalar("SELECT Status FROM gh.SyntheticDemoRuns WHERE RunKey=@R",Database.Parameter("@R",SyntheticDemo.RunKey)))=="Ready"){
                        int user=Convert.ToInt32(db.Scalar("SELECT TOP 1 r.RecordId FROM gh.SyntheticDemoRecords r JOIN gh.UserAccounts u ON u.UserId=r.RecordId WHERE r.RunKey=@R AND r.TableName='[gh].[UserAccounts]' ORDER BY r.RecordId",Database.Parameter("@R",SyntheticDemo.RunKey)));
                        Assert(user>0,"Ready demonstration has manifest-owned account provenance");
                        string fixtureEmail="QA-DEMO-"+Guid.NewGuid().ToString("N")+"@example.test";
                        db.Execute("UPDATE gh.UserAccounts SET DisplayName='QA demo preservation',Email=@E,NormalizedEmail=UPPER(@E),IsActive=0,FailedLoginCount=5,LockedUntil=DATEADD(hour,1,SYSUTCDATETIME()),AuthVersion=AuthVersion+1 WHERE UserId=@U",Database.Parameter("@E",fixtureEmail),Database.Parameter("@U",user));
                        string changedAccounts=Accounts();
                        Assert(!SyntheticDemo.Seed("IgnoredDifferent123!"),"Registered demo is not recreated after a fixture rename, disable and lockout");
                        Assert(Accounts()==changedAccounts,"No-op seeding preserves the renamed, disabled, locked fixture and password");
                        Reject(()=>SyntheticDemo.Cleanup(true),"Cleanup preview refuses a modified manifest-owned record without deleting data");
                    }
                    Console.WriteLine("NOTE: full seed/cleanup fixture is covered in the pre-population run; existing dataset is preserved.");
                }else{
                    Assert(SyntheticDemo.Preview().StartsWith("Preview only"),"Preview reports intent without adding accounts");
                    Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts"))==before,"Preview is read-only");
                    Assert(SyntheticDemo.Seed("SyntheticFixture123!"),"Full cross-department synthetic setup succeeds through normal workflows");
                    Assert(!SyntheticDemo.Seed("IgnoredDifferent123!"),"Second seed is an idempotent no-op and does not replace passwords");
                    Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts"))==before+11,"Exactly eleven new synthetic identities; no duplicate accounts");
                    int rows=SyntheticDemo.Cleanup(true);Assert(rows>100,"Cleanup preview verifies provenance and original-record preservation");
                    Assert(SyntheticDemo.Cleanup(false)==rows,"Selective cleanup removes exactly the unchanged manifest rows");
                    Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts"))==before,"Selective cleanup preserves every original account");
                    Assert(!SyntheticDemo.Seed("IgnoredDifferent123!"),"Cleanup tombstone prevents accidental account resurrection");
                }
            }
            Assert(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts"))==before,"Synthetic test fixture leaves original account count unchanged");
            Assert(Accounts()==accountsBefore,"Rollback restores all account identities, credentials and security states exactly");
            if(Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM gh.SyntheticDemoRuns"))==0){
                using(var tx=new TransactionScope(TransactionScopeOption.Required,TimeSpan.FromMinutes(10))){
                    SyntheticDemo.Seed("SyntheticFixture123!");
                    db.Execute("UPDATE gh.UserAccounts SET DisplayName=DisplayName+' updated',IsActive=0,LockedUntil=DATEADD(hour,1,SYSUTCDATETIME()) WHERE Email='synthetic.patient1@guardianhealth.test'");
                    Assert(!SyntheticDemo.Seed("IgnoredDifferent123!"),"Renamed, disabled and locked demo accounts are not reset by seeding");
                    Reject(()=>SyntheticDemo.Cleanup(false),"Cleanup refuses changed demo records rather than discarding workflow history");
                }
                using(var tx=new TransactionScope(TransactionScopeOption.Required,TimeSpan.FromMinutes(10))){
                    SyntheticDemo.Seed("SyntheticFixture123!");
                    db.Execute("INSERT INTO gh.Departments(DepartmentCode,DepartmentName) VALUES('QA-DEMO-OUTSIDE','Unrelated fixture')");
                    Reject(()=>SyntheticDemo.Cleanup(false),"Cleanup refuses untracked activity and cannot remove unrelated data");
                }
            }
        }finally{Environment.SetEnvironmentVariable("HOSPITALMS_DEMO_OPT_IN",opt);Environment.SetEnvironmentVariable("HOSPITALMS_ENVIRONMENT",env);}
    }
}
