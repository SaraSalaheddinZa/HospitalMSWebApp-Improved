using System;
using System.Globalization;
using System.Text;
using System.Web.UI;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;
namespace HospitalMSWebApp.Views.Admin
{
    public class ReportDownload:AppPage
    {
        private string output;
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);if(!RequireRoles("Admin"))return;
            try{
                DateTime from,through;
                if(!DateTime.TryParseExact(Request.QueryString["from"],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out from)||!DateTime.TryParseExact(Request.QueryString["to"],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out through))throw new ArgumentException("Choose valid report dates.");
                string key=Request.QueryString["report"];
                var rows=new HospitalWorkflowService().GetOperationalReport(key,CurrentUser.UserId,from,through);
                output="\uFEFF"+CsvExport.Encode(rows);
                Response.ContentType="text/csv";Response.ContentEncoding=Encoding.UTF8;
                Response.AddHeader("Content-Disposition","attachment; filename=guardian-"+key+"-"+from.ToString("yyyyMMdd")+"-"+through.ToString("yyyyMMdd")+".csv");
                Response.AddHeader("X-Content-Type-Options","nosniff");
                AuditService.Write(CurrentUser,"OperationalReportExported","Report",key,from.ToString("yyyy-MM-dd")+" through "+through.ToString("yyyy-MM-dd"),Request);
            }catch(ArgumentException ex){Response.StatusCode=400;Response.TrySkipIisCustomErrors=true;Response.ContentType="text/plain";output=ex.Message;}
        }
        protected override void Render(HtmlTextWriter writer){if(output!=null&&!Response.IsRequestBeingRedirected)writer.Write(output);}
    }
}
