using System;
using System.Data;
using System.Text;
using System.Web;
using HospitalMSWebApp.Infrastructure;
namespace HospitalMSWebApp.Views.Guest
{
    public partial class Home : System.Web.UI.Page
    {
        protected string DepartmentMarkup = "";
        protected string ContactMarkup = "";
        protected void Page_Load(object sender, EventArgs e)
        {
            // Publish department names only, never staff accounts or internal clinical activity.
            try
            {
                var db = new Database();
                var departments = db.Query(@"SELECT DepartmentName FROM gh.Departments d WHERE IsActive=1
                    AND NOT EXISTS(SELECT 1 FROM gh.SyntheticDemoRecords s WHERE s.TableName='[gh].[Departments]' AND s.RecordId=d.DepartmentId)
                    ORDER BY CASE DepartmentName WHEN 'General Medicine' THEN 0 WHEN 'Laboratory' THEN 1 WHEN 'Pharmacy' THEN 2 WHEN 'Emergency Department' THEN 3 ELSE 4 END,DepartmentName");
                var html = new StringBuilder();
                int index = 0;
                foreach (DataRow row in departments.Rows)
                {
                    string name = Convert.ToString(row["DepartmentName"]);
                    string artwork = "medicine", description = "Explore the medical team and published availability, or ask reception about this department.", action = "Find a Doctor", route = "../Patient/MedicalTeam.aspx";
                    if (name == "Laboratory") { artwork = "laboratory"; description = "Tests ordered by your clinical team connect to your record. Return to finalized results in your Patient Portal."; action = "View your results"; route = "../login.aspx?role=Patient"; }
                    else if (name == "Pharmacy") { artwork = "pharmacy"; description = "Stay connected to prescribed medicines and recorded dispensing, with the details kept in your own care record."; action = "View prescriptions"; route = "../login.aspx?role=Patient"; }
                    else if (name == "Emergency Department") { artwork = "emergency"; description = "For urgent medical help, contact your local emergency services. Do not use online appointments or support forms for an emergency."; action = null; }
                    html.Append("<details name='care-department' data-dept='").Append(artwork).Append(index == 0 ? "' open>" : "'>")
                        .Append("<summary><span><small>").Append((++index).ToString("D2")).Append("</small> <span class='public-department-name'>")
                        .Append(HttpUtility.HtmlEncode(name)).Append("</span></span><b aria-hidden='true'>↗</b></summary><div><p>")
                        .Append(HttpUtility.HtmlEncode(description)).Append("</p>");
                    if (action != null) html.Append("<a class='line-link' href='").Append(HttpUtility.HtmlAttributeEncode(route)).Append("'>").Append(action).Append(" <span aria-hidden='true'>↗</span></a>");
                    html.Append("</div></details>");
                }
                DepartmentMarkup = html.Length == 0
                    ? "<p class='public-empty'>Please ask reception about available services.</p>"
                    : html.ToString();
                var contact = Convert.ToString(db.Scalar("SELECT SettingValue FROM gh.SystemSettings WHERE SettingKey='ReceptionContact'"));
                if (!string.IsNullOrWhiteSpace(contact))
                    ContactMarkup = "<p class='public-contact-detail'><strong>Reception</strong><br />" + HttpUtility.HtmlEncode(contact) + "</p>";
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                System.Diagnostics.Trace.TraceError("Public hospital information unavailable: {0}", ex.Number);
                DepartmentMarkup = "<p class='public-empty'>Department information is temporarily unavailable. Please contact reception during your visit.</p>";
            }
        }
    }
}
