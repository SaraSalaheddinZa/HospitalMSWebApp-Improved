using System;
using System.Net;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
class HttpRegression
{
    static readonly CookieContainer Cookies=new CookieContainer();
    static string LastContentType;
    static string LastCacheControl,LastDisposition,LastNoSniff;
    static string Request(string path,string body=null)
    {
        var r=(HttpWebRequest)WebRequest.Create("http://localhost:53677"+path);r.CookieContainer=Cookies;r.Timeout=20000;r.AllowAutoRedirect=true;
        if(body!=null){r.Method="POST";r.ContentType="application/x-www-form-urlencoded";var bytes=Encoding.UTF8.GetBytes(body);using(var s=r.GetRequestStream())s.Write(bytes,0,bytes.Length);}
        using(var response=r.GetResponse())using(var reader=new StreamReader(response.GetResponseStream())){LastContentType=response.ContentType;LastCacheControl=response.Headers["Cache-Control"]??"";LastDisposition=response.Headers["Content-Disposition"]??"";LastNoSniff=response.Headers["X-Content-Type-Options"]??"";return reader.ReadToEnd();}
    }
    static string Encode(Dictionary<string,string> values)
    {
        var b=new StringBuilder();foreach(var p in values){if(b.Length>0)b.Append('&');b.Append(Uri.EscapeDataString(p.Key)).Append('=').Append(Uri.EscapeDataString(p.Value));}return b.ToString();
    }
    static Dictionary<string,string> Hidden(string html)
    {
        var result=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(html,"<input[^>]+>",RegexOptions.IgnoreCase)){
            if(!Regex.IsMatch(m.Value,"type=['\"]hidden['\"]",RegexOptions.IgnoreCase))continue;
            var name=Regex.Match(m.Value,"name=['\"]([^'\"]+)");var value=Regex.Match(m.Value,"value=['\"]([^'\"]*)");
            if(name.Success&&value.Success)result[name.Groups[1].Value]=WebUtility.HtmlDecode(value.Groups[1].Value);
        }return result;
    }
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message);}
    static void BadRequest(string path,string message){
        bool rejected=false;
        try{Request(path);}catch(WebException ex){using(var response=ex.Response as HttpWebResponse){rejected=response!=null&&response.StatusCode==HttpStatusCode.BadRequest;}}
        Assert(rejected,message);
    }
    static string Login(string role,string email)
    {
        Request("/Views/SignOut.aspx");
        var fields=Hidden(Request("/Views/login.aspx"));
        fields["emailTb"]=email;fields["roleCb"]=role;fields["loginBtn"]="Sign In";
        string key="HOSPITALMS_TEST_"+role.ToUpperInvariant()+"_PASSWORD";
        fields["passwordTb"]=Environment.GetEnvironmentVariable(key);
        if(string.IsNullOrEmpty(fields["passwordTb"]))throw new InvalidOperationException("Private credential required: "+key+". No sign-in was attempted.");
        string page=Request("/Views/login.aspx",Encode(fields));
        Assert(page.Contains("data-role='"+role+"'"),role+" authenticates and redirects to its dedicated workspace");
        return page;
    }
    static void DirectorySelection(string pageName,string email,string nameKey,string expectedValidation)
    {
        string path="/Views/Admin/"+pageName+".aspx",html=Request(path),row="";
        foreach(Match item in Regex.Matches(html,"<tr[^>]*>.*?</tr>",RegexOptions.Singleline))if(item.Value.Contains(email)){row=item.Value;break;}
        var action=Regex.Match(WebUtility.HtmlDecode(row),"__doPostBack\\('([^']+)'\\s*,\\s*'([^']+)'\\)");
        Assert(action.Success,pageName+" directory exposes selection for the preserved development profile");
        var fields=Hidden(html);fields["__EVENTTARGET"]=action.Groups[1].Value;fields["__EVENTARGUMENT"]=action.Groups[2].Value;
        html=Request(path,Encode(fields));
        Assert(html.Contains("value=\""+email+"\""),pageName+" selection postback loads the intended existing profile");
        fields=Hidden(html);fields["__EVENTTARGET"]="";fields["__EVENTARGUMENT"]="";fields["ctl00$Mybody$editBtn"]="Update";fields[nameKey]="";
        html=Request(path,Encode(fields));
        Assert(html.Contains(expectedValidation)&&!html.Contains("Select a staff member to update"),pageName+" selected identity survives the next postback; invalid edit changes no records");
        Assert(Request(path,Encode(fields)).Contains("already submitted or has expired"),pageName+" directory rejects a replayed Web Forms submission");
    }
    static string ProfileForm(string html)
    {
        foreach(Match form in Regex.Matches(html,"<form[^>]*>.*?</form>",RegexOptions.Singleline))
            if(form.Value.Contains("name='action' value='staffProfile'"))return form.Value;
        throw new Exception("Selected staff profile form is missing.");
    }
    static void ProfileSelection(string role)
    {
        const string path="/Views/Admin/Workspace.aspx";
        string html=Request(path);
        var option=Regex.Match(WebUtility.HtmlDecode(html),"<option value='([0-9]+)'>[^<]+ · "+role+"</option>");
        Assert(option.Success,"Admin profile workflow offers a named "+role+" account selector");
        string id=option.Groups[1].Value;
        html=Request(path+"?staffUser="+id);
        var fields=Hidden(ProfileForm(html));fields["name"]="Rejected profile fixture";fields["email"]="not-saved@example.test";fields["profileVersion"]="stale";
        html=Request(path,Encode(fields));
        Assert(html.Contains("after it was opened"),role+" profile HTTP endpoint rejects stale submissions without modifying the account");
        Assert(Hidden(ProfileForm(html))["userId"]==id&&!ProfileForm(html).Contains("Rejected profile fixture"),role+" profile remains selected with persisted values after failed submission");
        Assert(Request(path,Encode(fields)).Contains("already submitted or has expired"),role+" profile HTTP endpoint rejects replay");
        fields=Hidden(ProfileForm(Request(path+"?staffUser="+id)));fields["name"]=" ";fields["email"]="not-saved@example.test";
        html=Request(path,Encode(fields));
        Assert(html.Contains("Enter name.")&&Hidden(ProfileForm(html))["userId"]==id,role+" profile selection also survives required-field validation");
    }
    static void Main()
    {
        if(HasCredential("Receptionist")) {
        var result=Login("Receptionist","dev.reception@guardianhealth.test");Assert(result.Contains("Reception &amp; appointments")||result.Contains("Reception & appointments"),"HTTP sign-in routes to receptionist workspace");
        var page=Request("/Views/Receptionist/Finance.aspx");
        var form=Regex.Match(page,"<form[^>]*>.*?name='action' value='invoice'.*?</form>",RegexOptions.Singleline).Value;
        var fields=Hidden(form);fields["patientId"]="10";fields["description"]="HTTP validation fixture";fields["amount"]="-1";
        result=Request("/Views/Receptionist/Finance.aspx",Encode(fields));Assert(result.Contains("positive invoice amount"),"Negative invoice is rejected");
        result=Request("/Views/Receptionist/Finance.aspx",Encode(fields));Assert(result.Contains("already submitted or has expired"),"Identical POST replay is rejected");
        fields["csrf"]="invalid";result=Request("/Views/Receptionist/Finance.aspx",Encode(fields));Assert(result.Contains("Security validation failed"),"Invalid CSRF is rejected");
        result=Request("/Views/Admin/Workspace.aspx");Assert(result.Contains("id=\"loginBtn\""),"Receptionist cannot open administrator workspace");
        }
        var roles=new[]{"Admin","Doctor","Receptionist","Laboratorian","Nurse","Pharmacist","Patient"};
        var emails=new[]{"admin","doctor","reception","lab","nurse","pharmacist","patient"};
        var routes=new[]{
            new[]{"Admin/Workspace.aspx","Admin/Operations.aspx","Admin/Reports.aspx","Admin/CommandCenter.aspx","Admin/CommandCenter.aspx?queue=Laboratory","Admin/CommandCenter.aspx?queue=Insurance","Admin/AuditReview.aspx","Admin/AuditReview.aspx?source=Access&page=2","Admin/Doctors.aspx","Admin/Receptionist.aspx","Admin/Laboratorian.aspx","Documents/Index.aspx","Search/Index.aspx?q=Taylor"},
            new[]{"Doctors/ClinicalWorkspace.aspx","Doctors/ClinicalWorkspace.aspx?patientId=10","Doctors/Results.aspx","Doctors/Notes.aspx","Records/Index.aspx?patientId=10","Doctors/Prescriptions.aspx"},
            new[]{"Receptionist/Workspace.aspx","Receptionist/Patients.aspx","Receptionist/Finance.aspx"},
            new[]{"Laboratorian/Workspace.aspx","Laboratorian/LabTest.aspx"},
            new[]{"Nurse/Workspace.aspx","Nurse/Emergency.aspx","Records/Index.aspx?patientId=10"},
            new[]{"Pharmacist/Workspace.aspx","Pharmacist/Suppliers.aspx"},
            new[]{"Patient/Workspace.aspx","Patient/Appointments.aspx","Patient/Appointments.aspx?view=past","Patient/Appointments.aspx?appointment=9223372036854775807","Patient/MedicalTeam.aspx","Patient/MedicalTeam.aspx?doctor=2147483647","Patient/MedicalTeam.aspx?date=2000-01-01","Patient/MedicalTeam.aspx?date=9999-12-31","Records/Index.aspx?patientId=5","Documents/Index.aspx","Notifications/Index.aspx"}
        };
        for(int i=0;i<roles.Length;i++){
            if(!HasCredential(roles[i]))continue;
            Login(roles[i],"dev."+emails[i]+"@guardianhealth.test");
            if(roles[i]!="Patient")Assert(Request("/Views/Patient/MedicalTeam.aspx").Contains("id=\"loginBtn\""),roles[i]+" cannot enter patient booking UI");
            if(roles[i]!="Patient")Login(roles[i],"dev."+emails[i]+"@guardianhealth.test");
            if(roles[i]!="Patient"){
                Assert(Request("/Views/Patient/Appointments.aspx").Contains("id=\"loginBtn\""),roles[i]+" cannot enter patient appointment management");
                Login(roles[i],"dev."+emails[i]+"@guardianhealth.test");
            }
            foreach(var route in routes[i]){
                string html=Request("/Views/"+route);
                Assert(html.Contains("gh-main")&&!html.Contains("Server Error in"),roles[i]+" renders "+route);
                if(roles[i]=="Patient"&&route.StartsWith("Records/"))Assert(html.Contains("GH-DEV-000001")&&!html.Contains("GH-00000005"),"Patient record ignores another patient's requested ID");
            }
            var inbox=Request("/Views/Notifications/Index.aspx");
            Assert(inbox.Contains("My notifications"),roles[i]+" has its own inbox");
            if(roles[i]=="Admin"){
                DirectorySelection("Doctors","dev.doctor@guardianhealth.test","ctl00$Mybody$DocNameTb","Please enter doctor name");
                DirectorySelection("Laboratorian","dev.lab@guardianhealth.test","ctl00$Mybody$LabNameTb","Please enter full name");
                DirectorySelection("Receptionist","dev.reception@guardianhealth.test","ctl00$Mybody$RecNameTb","Please enter a full name");
                ProfileSelection("Nurse");
                ProfileSelection("Pharmacist");
                string csv=Request("/Views/Admin/ReportDownload.aspx?report=collections&from=2026-01-01&to=2026-12-31");
                Assert(LastContentType.StartsWith("text/csv")&&csv.Contains("\"Day\",\"Method\",\"Payments\",\"Collected\"")&&!csv.Contains("<html"),"Authorized report download returns CSV without app markup");
                Assert(LastCacheControl.Contains("no-store")&&LastNoSniff=="nosniff"&&LastDisposition.StartsWith("attachment; filename=guardian-collections-"),"Report download uses private no-store attachment and nosniff headers");
                BadRequest("/Views/Admin/ReportDownload.aspx?report=unknown&from=2026-01-01&to=2026-12-31","Report download rejects unknown selections");
                BadRequest("/Views/Admin/ReportDownload.aspx?report=collections&from=0001-01-01&to=2026-12-31","Report download rejects out-of-range SQL dates without server error");
                BadRequest("/Views/Admin/ReportDownload.aspx?report=collections&from=2026-12-31&to=2026-01-01","Report download rejects reversed ranges");
            }
            string forbidden=roles[i]=="Admin"?"/Views/Doctors/ClinicalWorkspace.aspx":"/Views/Admin/Workspace.aspx";
            Assert(Request(forbidden).Contains("id=\"loginBtn\""),roles[i]+" cannot cross the tested role boundary");
        }
        if(HasCredential("Patient")) {
        Login("Patient","dev.patient@guardianhealth.test");
        Assert(Request("/Views/Admin/CommandCenter.aspx?queue=Billing").Contains("id=\"loginBtn\""),"Patient cannot access operational worklists");
        Assert(Request("/Views/Admin/AuditReview.aspx").Contains("id=\"loginBtn\""),"Patient cannot access accountability history");
        Assert(Request("/Views/Admin/ReportDownload.aspx?report=collections&from=2026-01-01&to=2026-12-31").Contains("id=\"loginBtn\""),"Patient cannot download operational reports");
        }
        foreach(var path in new[]{"/Views/Guest/Home.aspx","/Views/HelpCenter.aspx","/Views/ContactAdmin.aspx","/Views/Support.aspx","/Views/ForgotPassword.aspx","/Views/ResetPassword.aspx","/Views/SignOut.aspx"}){
            string html=Request(path);Assert(html.IndexOf("guardian",StringComparison.OrdinalIgnoreCase)>=0&&!html.Contains("Server Error in"),"Public page renders "+path);
        }
        string home=Request("/Views/Guest/Home.aspx");
        Assert(home.Contains("universe.css")&&home.Contains("public-preference.js")&&home.Contains("data-theme-toggle"),"Public homepage has isolated theme and accessible toggle infrastructure");
        Assert(home.IndexOf("public-preference.js",StringComparison.Ordinal)<home.IndexOf("universe.css",StringComparison.Ordinal),"Public theme initialization precedes the stylesheet");
        Assert(home.Contains("../Patient/MedicalTeam.aspx")&&home.Contains("role=Patient")&&home.Contains("access=staff"),"Homepage preserves protected discovery and both audience access paths");
        Assert(home.Contains("public-department-name")&&!home.Contains("Synthetic Cardiology")&&!home.Contains("risk score"),"Homepage binds real departments without publishing synthetic services");
        Assert(home.Contains("data-motion-toggle")&&home.IndexOf("illustrative",StringComparison.OrdinalIgnoreCase)>=0,"Homepage provides motion control and artwork disclosure");
        var publicLogin=Request("/Views/login.aspx?role=Patient");
        Assert(publicLogin.Contains("living-sculpture-render.css")&&publicLogin.Contains("password-toggle")&&publicLogin.Contains("aria-controls=\"passwordTb\""),"Sign-in has scoped presentation and accessible password visibility control");
        Assert(publicLogin.Contains("autocomplete=\"username\"")&&publicLogin.Contains("autocomplete=\"current-password\"")&&publicLogin.Contains("role=\"alert\""),"Sign-in preserves autofill and accessible server feedback");
        Assert(!publicLogin.Contains("guardian-auth.css")&&!publicLogin.Contains("guardian.css?"),"New public sign-in does not combine conflicting legacy themes");
        Assert(Request("/Assets/Public/LivingThread/living-sculpture.css").Contains("prefers-reduced-motion"),"Public stylesheet serves reduced-motion fallback");
        Assert(Request("/Assets/Public/LivingThread/public-preference.js").Contains("guardian-public-theme"),"Public theme script is served locally");
        Assert(Request("/Views/Patient/MedicalTeam.aspx").Contains("id=\"loginBtn\""),"Anonymous homepage discovery cannot bypass authentication");
        Assert(home.IndexOf("hospital experience",StringComparison.OrdinalIgnoreCase)>=0&&!home.Contains("Trusted by 500")&&!home.Contains("counterPatients")&&!home.Contains("testimonials"),"Hospital homepage contains no legacy fabricated statistics/testimonials");
        Assert(Request("/Default.aspx").Contains("heart-artistic-hybrid.png"),"Default route opens the hospital homepage");
        Assert(Request("/About.aspx").Contains("heart-artistic-hybrid.png"),"Legacy About opens the maintained hospital introduction");
        var contact=Request("/Contact.aspx");
        Assert(contact.Contains("How can we help?")&&!contact.Contains("One Microsoft Way")&&!contact.Contains("example.com"),"Legacy Contact no longer exposes fictitious scaffold contact information");
        Assert(Request("/WebForm1.aspx").Contains("heart-artistic-hybrid.png"),"Empty scaffold page opens the hospital homepage");
        Assert(Request("/Views/Logout.aspx").Contains("Signed out securely"),"Legacy logout uses the maintained secure sign-out experience");
        foreach(var path in new[]{"/Views/ContactAdmin.aspx","/Views/Support.aspx","/Views/ForgotPassword.aspx"}){
            var submission=Hidden(Request(path));submission["submitBtn"]="Submit";
            Request(path,Encode(submission)); // Invalid empty input; no message or ticket is persisted.
            Assert(Request(path,Encode(submission)).Contains("already submitted"),"Public assistance form rejects replay: "+path);
        }
        if(blocked.Count>0)Console.WriteLine("PARTIAL: HttpRegression public checks completed; authenticated checks blocked for "+string.Join(", ",blocked));
    }
    static readonly HashSet<string> blocked=new HashSet<string>();
    static bool HasCredential(string role) {
        string key="HOSPITALMS_TEST_"+role.ToUpperInvariant()+"_PASSWORD";
        if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))return true;
        if(Environment.GetEnvironmentVariable("HOSPITALMS_ALLOW_PARTIAL_CREDENTIAL_TESTS")!="1")throw new InvalidOperationException("Private credential required: "+key);
        if(blocked.Add(role))Console.WriteLine("BLOCKED: HttpRegression authenticated "+role+" checks; "+key+" unavailable. No sign-in attempted.");
        return false;
    }
}
