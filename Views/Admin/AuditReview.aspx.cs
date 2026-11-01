using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Admin
{
    public class AuditReview:WorkspacePage
    {
        protected override string Role{get{return "Admin";}}
        protected override string Heading{get{return "Workflow accountability & audit";}}
        protected override void Submit(){throw new ArgumentException("Audit history is read-only.");}
        protected override string Content()
        {
            string source=Request.QueryString["source"]??"Workflow",entity=Request.QueryString["entity"]??"",search=Request.QueryString["q"]??"";
            DateTime? from,through;int page=1,parsed;int? actor=null;
            if(!ReadDate("from",out from)||!ReadDate("through",out through)||from<System.Data.SqlTypes.SqlDateTime.MinValue.Value||through<System.Data.SqlTypes.SqlDateTime.MinValue.Value||through<from||through>=DateTime.MaxValue.Date)
                return Panel("Invalid filters","Use a valid inclusive UTC date range. <a href='AuditReview.aspx'>Clear filters</a>");
            if(Request.QueryString["page"]!=null&&(!int.TryParse(Request.QueryString["page"],out page)||page<1||page>100000))return Panel("Invalid page","Choose a valid page. <a href='AuditReview.aspx'>Start again</a>");
            if(!string.IsNullOrEmpty(Request.QueryString["actor"])){
                if(!int.TryParse(Request.QueryString["actor"],out parsed)||parsed<1)return Panel("Invalid actor","Choose an account from the filter.");actor=parsed;
            }
            if((source!="Workflow"&&source!="Access")||entity.Length>80||search.Length>100)return Panel("Invalid filters","Choose supported audit filters. <a href='AuditReview.aspx'>Clear filters</a>");
            var form=new StringBuilder("<form method='get' class='row g-3 mb-4'><label class='col-md-4'>History source<select class='form-select' name='source'>");
            form.Append(Option("Workflow","Transactional workflow history",source)).Append(Option("Access","Access and request audit",source)).Append("</select></label>");
            form.Append("<label class='col-md-4'>Recorded by<select class='form-select' name='actor'><option value=''>All accounts</option>");
            foreach(DataRow r in Db.Query("SELECT UserId,DisplayName,RoleName FROM gh.UserAccounts ORDER BY DisplayName").Rows)
                form.Append(Option(Convert.ToString(r[0]),Convert.ToString(r[1])+" · "+Convert.ToString(r[2]),actor.HasValue?actor.Value.ToString():""));
            form.Append("</select></label><label class='col-md-4'>Record type<select class='form-select' name='entity'><option value=''>All record types</option>");
            foreach(DataRow r in Db.Query("SELECT EntityType FROM gh.WorkflowHistory UNION SELECT EntityType FROM gh.AuditEvents ORDER BY EntityType").Rows)form.Append(Option(Convert.ToString(r[0]),Convert.ToString(r[0]),entity));
            form.Append("</select></label><label class='col-md-4'>Action, detail or exact reference<input class='form-control' name='q' maxlength='100' value='").Append(H(search)).Append("'></label>");
            foreach(string key in new[]{"from","through"})form.Append("<label class='col-md-3'>").Append(key=="from"?"From":"Through").Append(" (UTC)<input type='date' class='form-control' name='").Append(key).Append("' value='").Append(H(Request.QueryString[key])).Append("'></label>");
            form.Append("<div class='col-12'><button class='btn btn-primary'>Filter history</button> <a href='AuditReview.aspx'>Clear filters</a></div></form>");
            var rows=Workflows.GetAuditHistory(CurrentUser.UserId,source,entity,actor,from,through,search,page);
            bool next=rows.Rows.Count>100;if(next)rows.Rows.RemoveAt(100);
            // Record reads after querying so this request cannot displace its own first result.
            Audit("AuditHistoryViewed","AuditReview",source);
            string links="<nav aria-label='Audit pages'>";
            if(page>1)links+="<a class='btn btn-outline-primary' href='"+PageLink(page-1)+"'>Newer events</a> ";
            links+="<span>Page "+page+" · "+rows.Rows.Count+" events</span> ";
            if(next)links+="<a class='btn btn-outline-primary' href='"+PageLink(page+1)+"'>Older events</a>";
            return form+Panel("Recorded history","<p>Workflow history is saved with the business transaction. Access/request audit also records reads and failed requests. Missing historical information is not reconstructed. New events may shift pages; narrow the dates and actor to trace an action.</p>"+links+"</nav>"+Table(rows));
        }
        private bool ReadDate(string key,out DateTime? value)
        {
            value=null;string input=Request.QueryString[key];if(string.IsNullOrEmpty(input))return true;
            DateTime parsed;if(!DateTime.TryParseExact(input,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out parsed))return false;value=parsed;return true;
        }
        private string Option(string value,string label,string selected){return "<option value='"+H(value)+"'"+(value==selected?" selected":"")+">"+H(label)+"</option>";}
        private string PageLink(int page)
        {
            var values=HttpUtility.ParseQueryString(Request.QueryString.ToString());values["page"]=page.ToString(CultureInfo.InvariantCulture);return H("AuditReview.aspx?"+values);
        }
    }
}
