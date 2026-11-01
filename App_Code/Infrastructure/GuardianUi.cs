using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
namespace HospitalMSWebApp.Infrastructure
{
    // Presentation only: authorization remains in pages and workflow services.
    public static class GuardianUi
    {
        public static string H(string value){return HttpUtility.HtmlEncode(value??"");}
        private static string Url(string value){return VirtualPathUtility.ToAbsolute("~/"+value);}
        public static string Home(string role){return "Views/"+(role=="Doctor"?"Doctors/ClinicalWorkspace.aspx":role+"/Workspace.aspx");}
        public static string MasterBody(string title)
        {
            var context=HttpContext.Current;var session=context.Session;
            var user=new AuthenticatedUser{Role=Convert.ToString(session["role"]),Name=Convert.ToString(session["userName"])};
            var markup=Begin(user,title,context.Request.Path);
            var message=Convert.ToString(session["DirectoryError"]);session.Remove("DirectoryError");
            return markup.Substring(markup.IndexOf("<body",StringComparison.Ordinal))+(message.Length>0?"<div class='alert alert-danger' role='alert'>"+H(message)+"</div>":"");
        }
        public static string Begin(AuthenticatedUser user,string title,string path)
        {
            string role=user.Role, displayRole=role=="Laboratorian"?"Laboratory":role=="Receptionist"?"Reception":role=="Admin"?"Administration":role;
            var b=new StringBuilder("<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>");
            b.Append(H(title)).Append(" · Guardian Health</title><link rel='stylesheet' href='").Append(Url("Lib/css/bootstrap.min.css")).Append("'><link rel='stylesheet' href='").Append(Url("Content/guardian.css?v=20261008.r2")).Append("'></head><body data-role='").Append(H(role)).Append("'><a class='gh-skip' href='#main-content'>Skip to main content</a><div class='gh-app'><aside class='gh-sidebar' id='app-navigation'><a class='gh-brand' href='").Append(Url(Home(role))).Append("'><span class='gh-brand-mark' aria-hidden='true'>+</span><span>Guardian<span class='gh-brand-sub'>Health.</span></span></a><div class='gh-role-label'><span class='gh-secure-dot'></span>").Append(H(displayRole)).Append(role=="Patient"?" portal":" workspace").Append("</div><nav aria-label='Primary navigation'>");
            NavGroup(b,role=="Patient"?"MY HEALTH":"WORKSPACE");
            Link(b,path,Home(role),role=="Patient"?"My health":"Dashboard","⌂");
            if(role=="Doctor"){Link(b,path,"Views/Doctors/ClinicalWorkspace.aspx#record-consultation","New consultation","✚");Link(b,path,"Views/Doctors/Notes.aspx","Consultations & notes","▤");Link(b,path,"Views/Doctors/Results.aspx","Laboratory review","◷");}
            if(role=="Nurse"){Link(b,path,"Views/Nurse/Workspace.aspx#nursing-assessment","Nursing assessments","▤");Link(b,path,"Views/Nurse/Emergency.aspx","Emergency & triage","✚");}
            if(role=="Pharmacist"){Link(b,path,"Views/Pharmacist/Workspace.aspx#dispense-prescription","Dispensing","✚");Link(b,path,"Views/Pharmacist/Workspace.aspx#inventory-and-alerts","Inventory","▦");Link(b,path,"Views/Pharmacist/Suppliers.aspx","Stock & suppliers","▤");}
            if(role=="Laboratorian"){Link(b,path,"Views/Laboratorian/Workspace.aspx#sample-collection-and-processing","Sample processing","◷");Link(b,path,"Views/Laboratorian/Workspace.aspx#finalize-result","Result entry & history","▥");}
            if(role=="Receptionist"){Link(b,path,"Views/Receptionist/Workspace.aspx#book-appointment","Appointment booking","◷");Link(b,path,"Views/Receptionist/Patients.aspx","Patient registration","♧");Link(b,path,"Views/Receptionist/Finance.aspx","Billing & insurance","▤");}
            if(role=="Admin"){
                Link(b,path,"Views/Admin/CommandCenter.aspx","Command center","▦");
                Link(b,path,"Views/Admin/Operations.aspx","Hospital operations","▦");Link(b,path,"Views/Admin/Reports.aspx","Reports & analytics","▥");
                NavGroup(b,"PEOPLE & ADMINISTRATION");
                Link(b,path,"Views/Admin/Doctors.aspx","Doctors","♧");Link(b,path,"Views/Admin/Receptionist.aspx","Reception staff","♧");Link(b,path,"Views/Admin/Laboratorian.aspx","Laboratory staff","♧");
                Link(b,path,"Views/Admin/Workspace.aspx#staff-directory","Staff & departments","▤");Link(b,path,"Views/Admin/Workspace.aspx#ward-room-and-bed-directory","Wards & beds","▦");Link(b,path,"Views/Admin/AuditReview.aspx","Audit review","≡");Link(b,path,"Views/Admin/Workspace.aspx#system-configuration","System settings","⚙");
            }
            if(role=="Patient"){
                Link(b,path,"Views/Patient/MedicalTeam.aspx","Find a doctor","♧");
                Link(b,path,"Views/Patient/Appointments.aspx","Appointments","◷");Link(b,path,"Views/Patient/Workspace.aspx#prescriptions","My medicines","✚");Link(b,path,"Views/Patient/Workspace.aspx#laboratory-results","Test results","▥");Link(b,path,"Views/Patient/Workspace.aspx#invoices","Invoices & payments","▤");
            }
            NavGroup(b,role=="Patient"?"RECORDS & MESSAGES":"SHARED TOOLS");
            if(role=="Doctor"||role=="Nurse"||role=="Patient")Link(b,path,"Views/Records/Index.aspx",role=="Patient"?"My medical record":"Medical records","▤");
            if(role=="Admin"||role=="Doctor"||role=="Nurse"||role=="Receptionist"||role=="Patient")Link(b,path,"Views/Documents/Index.aspx","Documents","▱");
            if(role!="Patient")Link(b,path,"Views/Search/Index.aspx","Search","⌕");
            Link(b,path,"Views/Notifications/Index.aspx",role=="Patient"?"Messages":"Notifications","○");
            b.Append("</nav><div class='gh-sidebar-footer'><a href='").Append(Url("Views/Guest/Home.aspx")).Append("'>Hospital home <span aria-hidden='true'>↗</span></a><small>Human care. Connected.</small></div></aside><div class='gh-main'><header class='gh-topbar'><button class='gh-menu' type='button' aria-controls='app-navigation' aria-expanded='true' aria-label='Toggle navigation' title='Toggle navigation'>").Append(Icon("≡")).Append("</button><div class='gh-breadcrumb'>").Append(H(displayRole)).Append("<span>/</span><strong>").Append(H(title)).Append("</strong></div>");
            if(role!="Patient")b.Append("<form class='gh-global-search' method='get' action='").Append(Url("Views/Search/Index.aspx")).Append("'><label><span class='visually-hidden'>Search hospital records</span><input type='search' name='q' placeholder='Search records…' minlength='2' maxlength='100' aria-label='Search hospital records'></label><button type='submit' aria-label='Run hospital search'>").Append(Icon("⌕")).Append("</button></form>");
            b.Append("<div class='gh-top-actions'><a class='gh-inbox-link' aria-label='Inbox' title='Notifications and messages' href='").Append(Url("Views/Notifications/Index.aspx")).Append("'>").Append(Icon("○")).Append("</a><span class='gh-user'><span class='gh-avatar' aria-hidden='true'>").Append(H(string.IsNullOrEmpty(user.Name)?"G":user.Name.Substring(0,1).ToUpperInvariant())).Append("</span><span>").Append(H(user.Name)).Append("<small>").Append(H(displayRole)).Append("</small></span></span><a class='gh-signout' href='").Append(Url("Views/SignOut.aspx")).Append("'>Sign out</a></div></header><main id='main-content' class='gh-content' tabindex='-1'><header class='gh-page-header'><div><p class='gh-eyebrow'>").Append(H(role=="Patient"?"YOUR PERSONAL HEALTH PORTAL":displayRole.ToUpperInvariant()+" / WORKSPACE")).Append("</p><h1>").Append(H(title)).Append("</h1><p class='gh-page-description'>").Append(H(Description(role))).Append("</p></div><div class='gh-header-side'><span class='gh-date'>").Append(DateTime.UtcNow.ToString("ddd, dd MMM yyyy",CultureInfo.InvariantCulture)).Append("<small>Clinical times · UTC</small></span>").Append(QuickActions(role)).Append("</div></header>");
            return b.ToString();
        }
        private static void NavGroup(StringBuilder b,string text){b.Append("<p class='gh-nav-label'>").Append(H(text)).Append("</p>");}
        private static string QuickActions(string role){
            string target=Home(role),text="Open workspace";
            switch(role){case "Admin":target="Views/Admin/Reports.aspx";text="View reports";break;case "Doctor":target+="#record-consultation";text="New consultation";break;case "Nurse":target+="#nursing-assessment";text="Record assessment";break;case "Pharmacist":target+="#dispense-prescription";text="Dispense prescription";break;case "Receptionist":target+="#book-appointment";text="Book appointment";break;case "Patient":target="Views/Patient/MedicalTeam.aspx";text="Book appointment";break;case "Laboratorian":target+="#finalize-result";text="Enter results";break;}
            var request=HttpContext.Current.Request;int patient;
            if((role=="Doctor"||role=="Nurse")&&int.TryParse(request.QueryString["patientId"],out patient)&&patient>0)
                target=target.Replace("#","?patientId="+patient.ToString(CultureInfo.InvariantCulture)+"#");
            if(role=="Nurse"&&request.Path.EndsWith("/Emergency.aspx",StringComparison.OrdinalIgnoreCase))target="Views/Nurse/Emergency.aspx#record-assessment";
            long task;
            if(role=="Pharmacist"&&long.TryParse(request.QueryString["itemId"],out task)&&task>0)target="Views/Pharmacist/Workspace.aspx?itemId="+task+"#dispense-prescription";
            if(role=="Laboratorian"&&long.TryParse(request.QueryString["item"],out task)&&task>0){target="Views/Laboratorian/Workspace.aspx?item="+task+"#selected-laboratory-task";text="Selected laboratory task";}
            return "<a class='btn gh-primary-action' href='"+Url(target)+"'>"+Icon("✚")+H(text)+"</a>";
        }
        private static string Icon(string key){
            string shape;
            switch(key){
                case "⌂":shape="<path d='m3 10 9-7 9 7v10a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1Z'/>";break;
                case "♧":shape="<circle cx='12' cy='7' r='4'/><path d='M4 21v-2a8 8 0 0 1 16 0v2'/>";break;
                case "◷":shape="<rect x='3' y='5' width='18' height='16' rx='2'/><path d='M7 3v4m10-4v4M3 11h18m-13 4h3m2 3h3'/>";break;
                case "✚":shape="<path d='M12 4v16M4 12h16'/>";break;
                case "▥":shape="<path d='M4 20V9m8 11V4m8 16v-8M2 22h20'/>";break;
                case "▦":shape="<rect x='3' y='3' width='7' height='7' rx='1'/><rect x='14' y='3' width='7' height='7' rx='1'/><rect x='3' y='14' width='7' height='7' rx='1'/><rect x='14' y='14' width='7' height='7' rx='1'/>";break;
                case "⌕":shape="<circle cx='10' cy='10' r='7'/><path d='m15 15 6 6'/>";break;
                case "○":shape="<path d='M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M9 21h6'/>";break;
                case "≡":shape="<path d='M4 6h16M4 12h16M4 18h16'/>";break;
                case "⚙":shape="<circle cx='12' cy='12' r='3'/><path d='M9 3h6l1 3 3 1 2 5-2 5-3 1-1 3H9l-1-3-3-1-2-5 2-5 3-1Z'/>";break;
                default:shape="<path d='M5 3h10l4 4v14H5Zm9 0v5h5M8 12h8m-8 4h6'/>";break;
            }
            return "<svg viewBox='0 0 24 24' class='gh-icon' fill='none' stroke='currentColor' stroke-width='1.6' stroke-linecap='round' stroke-linejoin='round' aria-hidden='true'>"+shape+"</svg>";
        }
        public static string End(){return "<footer class='gh-footer'>Guardian Health <span>Confidential health information · Authorized access only</span></footer></main></div></div><script src='"+Url("Scripts/guardian.js?v=20261019.task-feedback")+"' defer></script></body></html>";}
        private static void Link(StringBuilder b,string current,string target,string text,string icon){bool active=target.IndexOf('#')<0&&current.EndsWith("/"+target,StringComparison.OrdinalIgnoreCase);b.Append("<a class='gh-nav-link").Append(active?" is-active":"").Append("' ").Append(active?"aria-current='page' ":"").Append("href='").Append(Url(target)).Append("'><span class='gh-nav-icon' aria-hidden='true'>").Append(Icon(icon)).Append("</span>").Append(H(text)).Append("</a>");}
        private static string Description(string role){switch(role){case "Admin":return "A clear view of your people, operations and hospital performance.";case "Doctor":return "Patient context, clinical decisions and continuity of care.";case "Nurse":return "Stay close to patient needs, observations and the next care action.";case "Pharmacist":return "Safe dispensing, reliable stock and complete medication traceability.";case "Receptionist":return "Coordinate the patient journey, from registration to the next appointment.";case "Laboratorian":return "Follow every order from collection to a clear, verified result.";default:return "Your appointments, care records and messages — together in one secure place.";}}
        public static string Slug(string title){return Regex.Replace(title.ToLowerInvariant(),"[^a-z0-9]+","-").Trim('-');}
        public static string PatientSummary(DataTable table)
        {
            if(table.Rows.Count==0)return Panel("Patient not found","Choose an existing patient record.");
            var row=table.Rows[0];
            var b=new StringBuilder("<section class='gh-patient-context' aria-label='Patient safety summary'><div><p class='gh-eyebrow'>PATIENT CONTEXT &amp; SAFETY</p><h2>");
            b.Append(H(Convert.ToString(row["Patient"]))).Append("</h2><p>MRN ").Append(H(Convert.ToString(row["MRN"])));
            if(table.Columns.Contains("BirthDate")&&row["BirthDate"]!=DBNull.Value)b.Append(" · Born ").Append(H(Convert.ToDateTime(row["BirthDate"]).ToString("dd MMM yyyy",CultureInfo.InvariantCulture)));
            b.Append("</p></div><dl>");
            foreach(var field in new[]{"Allergies","BloodType","ChronicConditions"}){
                string value=Convert.ToString(row[field]);
                b.Append("<div").Append(field=="Allergies"?" class='gh-allergy'":"").Append("><dt>").Append(field=="BloodType"?"Blood type":field=="ChronicConditions"?"Chronic conditions":"Allergies").Append("</dt><dd>").Append(H(string.IsNullOrWhiteSpace(value)?"Not recorded — confirm with care team":value)).Append("</dd></div>");
            }
            return b.Append("</dl></section>").ToString();
        }
        public static string Panel(string title,string body){
            if(title.StartsWith("Create ")||title.StartsWith("Add ")||title.StartsWith("Publish "))return "<details class='gh-panel gh-disclosure' id='"+Slug(title)+"'><summary><h2>"+H(title)+"</h2><span>Open form</span></summary><div class='card-body'>"+body+"</div></details>";
            return "<section class='gh-panel card' id='"+Slug(title)+"'><div class='card-body'><h2>"+H(title)+"</h2>"+body+"</div></section>";
        }
        public static string Stat(string label,object value){return "<div class='col-6 col-lg-3'><div class='gh-stat'><div class='gh-stat-label'>"+H(label)+"</div><div class='gh-stat-value'>"+H(value is decimal?((decimal)value).ToString("N2",CultureInfo.InvariantCulture):Convert.ToString(value))+"</div><div class='gh-stat-note'>Live hospital data</div></div></div>";}
        public static string Badge(string value){string key=(value??"").Replace(" ","").ToLowerInvariant(),tone="neutral";if(Array.IndexOf(new[]{"completed","final","paid","approved","available","ready","stable","signed","instock","eligible"},key)>=0)tone="success";if(Array.IndexOf(new[]{"pending","ordered","booked","scheduled","submitted","confirmed","processing","inprogress","checkedin","undercare","unread"},key)>=0)tone="info";if(Array.IndexOf(new[]{"lowstock","nearexpiry","needsreview","cleaning","partiallypaid","reschedulerequested","waiting"},key)>=0)tone="warning";if(Array.IndexOf(new[]{"expired","critical","high","low","abnormal","escalated","rejected"},key)>=0)tone="danger";return "<span class='gh-badge gh-badge-"+tone+"'>"+H(Regex.Replace(value??"","([a-z])([A-Z])","$1 $2"))+"</span>";}
        public static string Table(DataTable table,string actionKey=null,string actionPrefix=null,string actionLabel=null,string actionFragment=null)
        {
            if(table.Rows.Count==0)return "<div class='gh-empty'><span class='gh-empty-symbol' aria-hidden='true'>≡</span><strong>Nothing to show yet</strong><p>Records will appear here when they are available.</p></div>";
            var b=new StringBuilder("<div class='gh-table-wrap table-responsive' tabindex='0' role='region' aria-label='Scrollable records'><table class='table gh-table'><thead><tr>");
            foreach(DataColumn c in table.Columns){if(c.ColumnName==actionKey)continue;b.Append("<th scope='col'>").Append(H(Regex.Replace(c.ColumnName,"([a-z])([A-Z])","$1 $2"))).Append("</th>");}if(actionKey!=null)b.Append("<th scope='col'>Next action</th>");b.Append("</tr></thead><tbody>");
            foreach(DataRow row in table.Rows){b.Append("<tr>");foreach(DataColumn c in table.Columns){if(c.ColumnName==actionKey)continue;var value=row[c];string text=value==DBNull.Value?"—":Convert.ToString(value,CultureInfo.InvariantCulture);if(value is DateTime){var date=(DateTime)value;text=date.ToString(date.TimeOfDay==TimeSpan.Zero?"dd MMM yyyy":"dd MMM yyyy · HH:mm",CultureInfo.InvariantCulture);}if(value is decimal)text=((decimal)value).ToString("N2",CultureInfo.InvariantCulture);bool status=c.ColumnName.IndexOf("status",StringComparison.OrdinalIgnoreCase)>=0||c.ColumnName=="Flag"||c.ColumnName=="Alert"||c.ColumnName=="ResultFlag";bool safety=c.ColumnName.IndexOf("allerg",StringComparison.OrdinalIgnoreCase)>=0;b.Append("<td").Append(value is decimal?" class='gh-number'":safety?" class='gh-safety-cell'":"").Append(">");if(c.ColumnName=="Acuity"||c.ColumnName=="AcuityLevel")b.Append(Badge(text=="1"||text=="2"?"Critical":text)).Append("<small>Level ").Append(H(text)).Append("</small>");else b.Append(status&&value!=DBNull.Value?Badge(text):H(text));b.Append("</td>");}if(actionKey!=null)b.Append("<td><a class='d-inline-block' href='").Append(H(actionPrefix+Convert.ToInt64(row[actionKey]).ToString(CultureInfo.InvariantCulture)+actionFragment)).Append("'>").Append(H(actionLabel)).Append("</a></td>");b.Append("</tr>");}
            return b.Append("</tbody></table></div>").ToString();
        }
    }
}
