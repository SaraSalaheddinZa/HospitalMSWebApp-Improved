using System;
using System.Text;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Admin
{
    public class CommandCenter:WorkspacePage
    {
        protected override string Role{get{return "Admin";}}
        protected override string Heading{get{return "Operational command center";}}
        protected override void Submit(){throw new ArgumentException("Use the authorized department workflow to make changes.");}
        protected override string Content()
        {
            var queues=new[]{"Appointments","Clinical","Laboratory","Pharmacy","Stock","Beds","Billing","Insurance","Emergency"};
            string selected=Request.QueryString["queue"]??"Appointments";
            if(Array.IndexOf(queues,selected)<0)return Panel("Unknown worklist","Choose a worklist from the command center navigation.");
            Audit("CommandCenterViewed","OperationalWorklist",selected);
            var b=new StringBuilder("<p>Live database snapshot at "+DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")+" UTC. Refresh to reload. Work remains assigned to the responsible department; administrator access does not grant clinical sign-off or dispensing authority.</p><nav aria-label='Operational worklists' class='d-flex flex-wrap gap-2 mb-3'>");
            foreach(var queue in queues)b.Append("<a class='btn btn-outline-primary' ").Append(queue==selected?"aria-current='page' ":"").Append("href='CommandCenter.aspx?queue=").Append(queue).Append("'>").Append(H(queue)).Append("</a>");
            b.Append("</nav><div class='row'>")
                .Append(Metric("Appointments today","SELECT COUNT(*) FROM gh.Appointments WHERE ScheduledStart>=CAST(SYSUTCDATETIME() AS date) AND ScheduledStart<DATEADD(day,1,CAST(SYSUTCDATETIME() AS date))"))
                .Append(Metric("Waiting patients","SELECT COUNT(*) FROM gh.Appointments WHERE Status='CheckedIn'"))
                .Append(Metric("Open consultations","SELECT COUNT(*) FROM gh.Encounters WHERE Status='Open'"))
                .Append(Metric("Pending lab orders","SELECT COUNT(*) FROM gh.LabOrders WHERE Status IN('Ordered','Collected','Processing')"))
                .Append(Metric("Prescriptions waiting","SELECT COUNT(*) FROM gh.PrescriptionOrders WHERE Status IN('Pending','PartiallyDispensed')"))
                .Append(Metric("Occupied beds","SELECT COUNT(*) FROM gh.Beds WHERE Status='Occupied'"))
                .Append(Metric("Unsettled insurance claims","SELECT COUNT(*) FROM gh.InsuranceClaims WHERE Status IN('Submitted','UnderReview','Approved')"))
                .Append(Metric("Outstanding balance","SELECT ISNULL(SUM(TotalAmount-PaidAmount),0) FROM gh.Invoices WHERE Status IN('Open','PartiallyPaid')")).Append("</div>");
            string action=selected=="Beds"?"<a href='Operations.aspx#active-admissions'>Admissions and transfers</a> · <a href='Operations.aspx#confirm-bed-cleaned-and-ready'>Bed cleaning/readiness</a>":selected=="Insurance"?"<a href='Operations.aspx#insurance-decisions-and-settlement'>Review or settle claims</a>":selected=="Appointments"?"<a href='Operations.aspx#publish-clinician-availability'>Manage clinician availability</a>":"<a href='Reports.aspx'>Review department reports</a>";
            var worklist=Workflows.GetOperationalWorklist(selected,CurrentUser.UserId);
            var statuses=new System.Collections.Generic.SortedSet<string>();foreach(System.Data.DataRow row in worklist.Rows)statuses.Add(Convert.ToString(row["Status"]));
            string current=Request.QueryString["status"]??"",filter="<form method='get' class='d-flex flex-wrap gap-2'><input type='hidden' name='queue' value='"+selected+"'><label>Work status<select name='status' class='form-select'><option value=''>All statuses</option>";
            if(!statuses.Contains(current))current="";
            foreach(var status in statuses)filter+="<option value='"+H(status)+"'"+(status==current?" selected":"")+">"+H(status)+"</option>";
            filter+="</select></label><button class='btn btn-primary'>Filter work</button></form>";
            var filtered=worklist.Clone();foreach(System.Data.DataRow row in worklist.Rows)if(current==""||Convert.ToString(row["Status"])==current)filtered.ImportRow(row);
            b.Append(Panel(selected+" worklist","<p>"+action+"</p>"+filter+"<p>Showing "+filtered.Rows.Count+" of up to 200 retrieved records. Status filtering applies to this snapshot; use reports for wider review. Dates are recorded times, or batch expiry for stock. A missing date is not inferred.</p>"+Table(filtered)));
            var alerts=Workflows.GetOperationalAlerts(CurrentUser.UserId);bool showAll=Request.QueryString["alerts"]=="all";
            var active=alerts.Clone();foreach(System.Data.DataRow row in alerts.Rows)if(showAll||Convert.ToInt32(row["AffectedRecords"])>0)active.ImportRow(row);
            b.Append(Panel("Operational alert rules","<p>"+(showAll?"All configured rules":"Only rules with affected records")+" · <a href='CommandCenter.aspx?queue="+selected+"&alerts="+(showAll?"active":"all")+"'>"+(showAll?"Show active alerts":"Show all rules")+"</a></p>"+Table(active)));
            return b.ToString();
        }
    }
}
