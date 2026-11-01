using System;
using System.Globalization;
using System.Text;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
namespace HospitalMSWebApp.Views.Admin
{
    public class Reports:WorkspacePage
    {
        protected override string Role{get{return "Admin";}}
        protected override string Heading{get{return "Operational reports";}}
        protected override void Submit(){throw new ArgumentException("Reports are read-only.");}
        protected override string Content()
        {
            DateTime from=DateTime.UtcNow.Date.AddDays(-29),through=DateTime.UtcNow.Date,parsed;
            if(Request.QueryString["from"]!=null){
                if(!DateTime.TryParseExact(Request.QueryString["from"],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out parsed))return Panel("Invalid date","Use a valid start date.");from=parsed;
            }
            if(Request.QueryString["to"]!=null){
                if(!DateTime.TryParseExact(Request.QueryString["to"],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out parsed))return Panel("Invalid date","Use a valid end date.");through=parsed;
            }
            if(from<System.Data.SqlTypes.SqlDateTime.MinValue.Value||through<from||through>=DateTime.MaxValue.Date)return Panel("Invalid period","Use dates from 1753 onward, with the end on or after the start and before 9999-12-31.");
            Audit("OperationalReportsViewed","Report",from.ToString("yyyy-MM-dd")+" through "+through.ToString("yyyy-MM-dd"));
            var html=new StringBuilder("<form method='get' class='row g-3 mb-4'><label class='col-md-4'>From (UTC)<input class='form-control' type='date' name='from' value='"+from.ToString("yyyy-MM-dd")+"' required></label><label class='col-md-4'>Through (UTC)<input class='form-control' type='date' name='to' value='"+through.ToString("yyyy-MM-dd")+"' required></label><div class='col-md-4 align-self-end'><button class='btn btn-primary'>Run reports</button></div></form><p>Inclusive UTC dates. Period reports group actual activity and current statuses; they are not historical balance snapshots. Current stock and receivables use today's database state regardless of date filters. Missing timestamps are excluded from duration averages, not estimated. Each section is refreshed when queried.</p>");
            foreach(string key in HospitalWorkflowService.OperationalReportKeys){
                var rows=Workflows.GetOperationalReport(key,CurrentUser.UserId,from,through);
                string export="<p><a href='ReportDownload.aspx?report="+key+"&amp;from="+from.ToString("yyyy-MM-dd")+"&amp;to="+through.ToString("yyyy-MM-dd")+"'>Download this report (CSV)</a></p>";
                html.Append(Panel(HospitalWorkflowService.OperationalReportTitle(key),export+Table(rows)));
            }
            return html.ToString();
        }
    }
}
