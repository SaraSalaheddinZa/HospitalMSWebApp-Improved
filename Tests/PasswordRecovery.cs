using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;

class PasswordRecovery
{
    const string Original = "RecoveryFixture123!", Replacement = "RecoveredFixture456!";
    static readonly Database Db = new Database();
    static readonly PasswordRecoveryService Recovery = new PasswordRecoveryService();
    static readonly HospitalWorkflowService Workflows = new HospitalWorkflowService();
    static int Admin;
    sealed class Account { public int Id; public string Email; public string Role; }
    static void Assert(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS: " + message); }
    static void Reject(Action action, string message) { bool denied=false;try { action(); } catch(ArgumentException) { denied=true; }catch(InvalidOperationException){denied=true;} Assert(denied,message); }
    static object Value(Account account, string column) { return Db.Scalar("SELECT " + column + " FROM gh.UserAccounts WHERE UserId=@U", Database.Parameter("@U",account.Id)); }
    static Account Create(string role)
    {
        string code="QA-RECOVERY-"+Guid.NewGuid().ToString("N").Substring(0,12);
        var account=new Account { Email=code+"@example.test",Role=role };
        account.Id=Workflows.CreateStaffAccount(code,role,code,account.Email,"",null,Original,Admin);
        return account;
    }
    static PasswordRecoveryRequest Issue(Account account) { return Recovery.RequestReset(account.Email,account.Role,null); }

    static void Main()
    {
        Environment.SetEnvironmentVariable("HOSPITALMS_CONNECTIONSTRING",@"Data Source=.\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True;MultipleActiveResultSets=True");
        Admin=Convert.ToInt32(Db.Scalar("SELECT UserId FROM gh.UserAccounts WHERE Email='dev.admin@guardianhealth.test'"));
        using(var fixture=new TransactionScope())
        {
            var account=Create("Nurse");var other=Create("Pharmacist");var otherHash=Value(other,"PasswordHash");
            var request=Issue(account);int version=Convert.ToInt32(Value(account,"AuthVersion"));
            Assert(request!=null&&request.Token.Length==43,"Recovery issues a cryptographically random, URL-safe 256-bit token");
            Assert(Convert.ToInt32(Db.Scalar("SELECT IssuedAuthVersion FROM gh.PasswordResetTokens WHERE UserId=@U AND UsedAt IS NULL",Database.Parameter("@U",account.Id)))==version,"Recovery token is bound to the account credential version");
            Assert(Convert.ToInt32(Db.Scalar("SELECT DATALENGTH(TokenHash) FROM gh.PasswordResetTokens WHERE UserId=@U AND UsedAt IS NULL",Database.Parameter("@U",account.Id)))==32,"Recovery stores a digest rather than the bearer token");
            Assert(Recovery.CompleteReset(request.Token,Replacement,null)==account.Id,"Valid reset changes only the token-bound account");
            Assert(AuthenticationService.Authenticate(account.Email,Replacement,account.Role)!=null&&AuthenticationService.Authenticate(account.Email,Original,account.Role)==null,"Recovered password authenticates and old password fails");
            Assert(Equals(Value(other,"PasswordHash"),otherHash),"A recovery token cannot target another account");
            Assert(Convert.ToInt32(Value(account,"AuthVersion"))==version+1,"Successful reset invalidates pre-existing authentication tickets");
            Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.PasswordResetTokens WHERE UserId=@U AND UsedAt IS NULL",Database.Parameter("@U",account.Id)))==0,"Successful reset consumes all outstanding account links");
            Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.Notifications WHERE UserId=@U AND NotificationType='AccountSecurity'",Database.Parameter("@U",account.Id)))==1,"Password recovery creates an account-scoped security notification");
            Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.AuditEvents WHERE EntityType='UserAccount' AND EntityId=@U AND ActionName IN('PasswordResetRequested','PasswordReset')",Database.Parameter("@U",account.Id.ToString())))==2,"Recovery request and completion have transactional audit records");
            Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.AuditEvents WHERE EntityId=@U AND (Detail LIKE '%'+@Token+'%' OR Detail LIKE '%'+@Password+'%')",Database.Parameter("@U",account.Id.ToString()),Database.Parameter("@Token",request.Token),Database.Parameter("@Password",Replacement)))==0,"Audit history contains neither bearer token nor password");
            Reject(()=>Recovery.CompleteReset(request.Token,Replacement,null),"A consumed reset token cannot be reused");
        }
        foreach(string scenario in new[]{"expired","superseded","unversioned","credential-version","disabled","weak-password","invalid-token"})
        using(var fixture=new TransactionScope())
        {
            var account=Create("Nurse");var request=Issue(account);
            if(scenario=="expired")Db.Execute("UPDATE gh.PasswordResetTokens SET ExpiresAt=DATEADD(second,-1,SYSUTCDATETIME()) WHERE UserId=@U",Database.Parameter("@U",account.Id));
            if(scenario=="superseded") {var latest=Issue(account);Assert(latest.Token!=request.Token&&Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.PasswordResetTokens WHERE UserId=@U AND UsedAt IS NULL",Database.Parameter("@U",account.Id)))==1,"Repeated requests retain only one usable outstanding link");}
            if(scenario=="unversioned")Db.Execute("UPDATE gh.PasswordResetTokens SET IssuedAuthVersion=NULL WHERE UserId=@U",Database.Parameter("@U",account.Id));
            if(scenario=="credential-version")Db.Execute("UPDATE gh.UserAccounts SET AuthVersion=AuthVersion+1 WHERE UserId=@U",Database.Parameter("@U",account.Id));
            if(scenario=="disabled") {Workflows.SetStaffActive(account.Id,false,Admin);Assert(Issue(account)==null&&!Convert.ToBoolean(Value(account,"IsActive")),"Disabled accounts receive no recovery token and remain disabled");}
            Reject(()=>Recovery.CompleteReset(scenario=="invalid-token"?new string('x',43):request.Token,scenario=="weak-password"?"weak":Replacement,null),"Recovery rejects "+scenario+" token or input");
        }
        using(var fixture=new TransactionScope())
        {
            var account=Create("Nurse");Issue(account);var latest=Issue(account);
            // Simulate two legacy/outstanding links at the same version without changing real data.
            Db.Execute("UPDATE gh.PasswordResetTokens SET UsedAt=NULL WHERE UserId=@U",Database.Parameter("@U",account.Id));
            Assert(Recovery.CompleteReset(latest.Token,Replacement,null)==account.Id,"The newest recovery link remains usable after a repeat request");
            Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.PasswordResetTokens WHERE UserId=@U AND UsedAt IS NULL",Database.Parameter("@U",account.Id)))==0,"Redemption consumes every outstanding link even if multiple same-version tokens exist");
        }
        foreach(string change in new[]{"password","email","deactivate"})
        using(var fixture=new TransactionScope())
        {
            string code="QA-RESET-"+Guid.NewGuid().ToString("N").Substring(0,12);
            var staff=new StaffDirectoryEntry {Role="Laboratorian",Name=code,Email=code+"@example.test",Gender="Other",Status="Active",Password=Original};
            staff.Id=Workflows.SaveStaffDirectoryEntry(staff,Admin);
            var request=Recovery.RequestReset(staff.Email,staff.Role,null);staff.Password="";
            if(change=="password"){staff.Password=Replacement;staff.ConfirmPasswordChange=true;}
            if(change=="email"){staff.Email="u-"+staff.Email;staff.ConfirmEmailChange=true;}
            if(change=="deactivate")Workflows.SetDirectoryStaffActive(staff.Role,staff.Id.Value,false,Admin);
            else Workflows.SaveStaffDirectoryEntry(staff,Admin);
            Reject(()=>Recovery.CompleteReset(request.Token,Replacement,null),"Directory "+change+" invalidates previously issued recovery links");
        }
        foreach(string role in new[]{"Nurse","Pharmacist"})
        {
            using(var fixture=new TransactionScope())
            {
                var account=Create(role);var hash=Value(account,"PasswordHash");
                Db.Execute("UPDATE gh.UserAccounts SET FailedLoginCount=5,LockedUntil=DATEADD(hour,1,SYSUTCDATETIME()) WHERE UserId=@U",Database.Parameter("@U",account.Id));
                object locked=Value(account,"LockedUntil");
                Workflows.SetStaffActive(account.Id,false,Admin);Workflows.SetStaffActive(account.Id,true,Admin);
                Assert(Equals(Value(account,"LockedUntil"),locked)&&Convert.ToInt32(Value(account,"FailedLoginCount"))==5,role+" access changes preserve lockout and failure count");
                Assert(Equals(Value(account,"PasswordHash"),hash)&&AuthenticationService.Authenticate(account.Email,Original,role)==null,role+" access changes preserve credential and cannot bypass a lockout");
                var request=Issue(account);Recovery.CompleteReset(request.Token,Replacement,null);
                Assert(Equals(Value(account,"LockedUntil"),locked)&&Convert.ToInt32(Value(account,"FailedLoginCount"))==5&&AuthenticationService.Authenticate(account.Email,Replacement,role)==null,role+" password recovery does not implicitly unlock the account");
                Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.WorkflowHistory WHERE EntityType='UserAccount' AND EntityId=@U AND PreviousStatus IS NOT NULL",Database.Parameter("@U",account.Id)))==2,role+" access history records actual prior and new states");
                Reject(()=>Workflows.SetStaffActive(account.Id,true,Admin),role+" rejects duplicate access status submissions");
            }
            using(var fixture=new TransactionScope())
            {
                var account=Create(role);Reject(()=>Workflows.SetStaffActive(account.Id,false,account.Id),role+" cannot change its own access without administrator authority");
            }
        }
        Assert(Recovery.RequestReset("not-a-real-account@example.test","Nurse",null)==null&&Recovery.RequestReset("not-a-real-account@example.test","Unknown",null)==null,"Unknown account or role creates no recovery request");
        foreach(string token in new[]{"",new string('x',42),new string('x',44),new string('!',43)})Reject(()=>Recovery.CompleteReset(token,Replacement,null),"Malformed reset token is rejected");
        using(var diagnostic=new StringWriter())
        using(var listener=new System.Diagnostics.TextWriterTraceListener(diagnostic))
        {
            System.Diagnostics.Trace.Listeners.Add(listener);
            try {
                var error=new InvalidOperationException("QA_SECRET_PASSWORD",new Exception("QA_SECRET_TOKEN"));
                error.Data["token"]="QA_SECRET_TOKEN";
                foreach(string stage in new[]{"request","delivery","redemption","QA_SECRET_STAGE"})PasswordRecoveryService.ReportFailure(stage,error);
                listener.Flush();
                string log=diagnostic.ToString();
                Assert(log.Contains("Password recovery delivery failed")&&log.Contains("InvalidOperationException")&&!log.Contains("QA_SECRET"),"Recovery error diagnostics exclude exception messages, nested secrets, data and untrusted stage text");
            } finally {System.Diagnostics.Trace.Listeners.Remove(listener);}
        }
        ConcurrentAndHttpChecks();
        Console.WriteLine("Recovery rollback fixtures and isolated concurrent/HTTP fixtures are cleaned; real accounts were never edited.");
    }

    static void ConcurrentAndHttpChecks()
    {
        // Independent SQL transactions/HTTP requests cannot see rollback-only setup rows.
        // Only this uniquely named disposable account is temporarily committed, and removed in finally.
        Account account=null;
        try
        {
            account=Create("Nurse");var token=Issue(account).Token;
            using(var start=new ManualResetEventSlim(false))
            {
                Func<bool> attempt=()=> {start.Wait();try { Recovery.CompleteReset(token,Replacement,null);return true; }catch(ArgumentException){return false;}};
                var first=Task.Run(attempt);var second=Task.Run(attempt);start.Set();
                Assert(Task.WaitAll(new Task[]{first,second},30000),"Concurrent recovery attempts finish without deadlock");
                Assert(first.Result!=second.Result,"Exactly one independent concurrent redemption succeeds");
                Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.AuditEvents WHERE EntityId=@U AND ActionName='PasswordReset'",Database.Parameter("@U",account.Id.ToString())))==1,"Concurrent recovery persists one completion audit and notification");
            }
            string known=ForgotHttp(account.Email,"Nurse"),unknown=ForgotHttp("not-a-real-account@example.test","Nurse"),wrongRole=ForgotHttp(account.Email,"Pharmacist");
            const string generic="If an active account matches those details, password-reset instructions will be sent shortly.";
            if(!known.Contains(generic)||!unknown.Contains(generic)||!wrongRole.Contains(generic)) {
                // Only fixed response classifications are diagnostic output; never dump form HTML/tokens.
                foreach(var feedback in new[]{known,unknown,wrongRole})Console.WriteLine("Recovery feedback: generic="+feedback.Contains(generic)+", expired="+feedback.Contains("already submitted")+", internalFailure="+feedback.Contains("could not process")+", missingInput="+feedback.Contains("select your role")+", legacyPreview="+feedback.Contains("one-time link below"));
            }
            Assert(known.Contains(generic)&&unknown.Contains(generic)&&wrongRole.Contains(generic),"Known, unknown and wrong-role recovery requests return the same public feedback");
            Assert(!known.Contains("?token=")&&!known.Contains("one-time link below"),"Public recovery responses do not disclose reset links even in development");
            token=Issue(account).Token;
            var response=Http("/Views/ResetPassword.aspx",new Dictionary<string,string>{{"token",token},{"password",Original},{"confirmation",Original},{"userId",Admin.ToString()}});
            Assert(response.Contains("Your password has been updated")&&AuthenticationService.Authenticate(account.Email,Original,"Nurse")!=null,"Supported reset POST consumes the token-bound fixture account, ignoring a forged target user ID");
            Assert(Http("/Views/ResetPassword.aspx",new Dictionary<string,string>{{"token",token},{"password",Replacement},{"confirmation",Replacement}}).Contains("invalid or expired"),"Reset HTTP endpoint rejects replay");
            Workflows.SetStaffActive(account.Id,false,Admin);
            Assert(ForgotHttp(account.Email,"Nurse").Contains(generic),"Disabled account recovery response does not expose activation state");
        }
        finally
        {
            if(account!=null)Cleanup(account);
        }
        Assert(Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM gh.UserAccounts WHERE UserId=@U",Database.Parameter("@U",account.Id)))==0,"Concurrent/HTTP recovery fixture leaves no retained account");
    }
    static readonly CookieContainer Cookies=new CookieContainer();
    static string Http(string path,Dictionary<string,string> fields=null)
    {
        var request=(HttpWebRequest)WebRequest.Create("http://localhost:53677"+path);request.CookieContainer=Cookies;request.Timeout=20000;
        if(fields!=null){var body=new StringBuilder();foreach(var field in fields){if(body.Length>0)body.Append('&');body.Append(Uri.EscapeDataString(field.Key)).Append('=').Append(Uri.EscapeDataString(field.Value));}request.Method="POST";request.ContentType="application/x-www-form-urlencoded";var bytes=Encoding.UTF8.GetBytes(body.ToString());using(var stream=request.GetRequestStream())stream.Write(bytes,0,bytes.Length);}
        using(var response=(HttpWebResponse)request.GetResponse())using(var reader=new StreamReader(response.GetResponseStream())){
            Assert((response.Headers["Cache-Control"]??"").Contains("no-store")&&response.Headers["Referrer-Policy"]=="no-referrer","Recovery HTTP response prevents token caching and referrer disclosure");return reader.ReadToEnd();
        }
    }
    static string ForgotHttp(string email,string role)
    {
        string html=Http("/Views/ForgotPassword.aspx");var fields=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(html,"<input[^>]+>",RegexOptions.IgnoreCase)){
            if(!Regex.IsMatch(m.Value,"type=['\"]hidden['\"]",RegexOptions.IgnoreCase))continue;
            var name=Regex.Match(m.Value,"name=['\"]([^'\"]+)");var value=Regex.Match(m.Value,"value=['\"]([^'\"]*)");
            if(name.Success&&value.Success)fields[name.Groups[1].Value]=WebUtility.HtmlDecode(value.Groups[1].Value);
        }
        fields["emailTb"]=email;fields["roleCb"]=role;fields["submitBtn"]="Send Reset Link";
        return Http("/Views/ForgotPassword.aspx",fields);
    }
    static void Cleanup(Account account)
    {
        Db.InTransaction((c,t)=>{
            using(var command=new System.Data.SqlClient.SqlCommand(@"DECLARE @Staff int;
                SELECT @Staff=StaffMemberId FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK)
                WHERE UserId=@U AND Email=@E AND Email LIKE 'QA-RECOVERY-%@example.test' AND RoleName='Nurse';
                IF @Staff IS NULL THROW 51190,'Recovery fixture cleanup identity mismatch.',1;
                DELETE FROM gh.Notifications WHERE UserId=@U;
                DELETE FROM gh.PasswordResetTokens WHERE UserId=@U;
                DELETE FROM gh.AuditEvents WHERE (EntityType='UserAccount' AND EntityId=CONVERT(nvarchar(80),@U)) OR ActorUserId=@U;
                DELETE FROM gh.WorkflowHistory WHERE EntityType='UserAccount' AND EntityId=@U;
                DELETE FROM gh.UserAccounts WHERE UserId=@U;
                DELETE FROM gh.StaffMembers WHERE StaffMemberId=@Staff;",c,t)){
                command.Parameters.AddRange(new[]{Database.Parameter("@U",account.Id),Database.Parameter("@E",account.Email)});command.ExecuteNonQuery();
            }return 0;
        });
    }
}
