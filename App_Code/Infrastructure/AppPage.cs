using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;

namespace HospitalMSWebApp.Infrastructure
{
    public abstract class AppPage : Page
    {
        protected AuthenticatedUser CurrentUser
        {
            get
            {
                if (Session["uid"] == null || Session["role"] == null) return null;
                int id;
                return int.TryParse(Session["uid"].ToString(), out id)
                    ? new AuthenticatedUser { UserId = id, Role = Session["role"].ToString(), Name = Convert.ToString(Session["userName"]), Email = Convert.ToString(Session["userEmail"]), PatientId = Session["patientId"] as int? }
                    : null;
            }
        }

        protected bool RequireRoles(params string[] roles)
        {
            var user = CurrentUser;
            if (user == null || !roles.Contains(user.Role, StringComparer.OrdinalIgnoreCase))
            {
                FormsAuthentication.SignOut();
                Response.Redirect("~/Views/login.aspx?returnUrl=" + Server.UrlEncode(Request.RawUrl), true);
                return false;
            }
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            return true;
        }

        protected string SafeReturnUrl(string fallback)
        {
            var value = Request.QueryString["returnUrl"];
            return !string.IsNullOrEmpty(value) && UrlIsLocal(value) ? value : fallback;
        }

        private static bool UrlIsLocal(string value)
        {
            return value.StartsWith("/") && !value.StartsWith("//") && !value.StartsWith("/\\");
        }
    }
}
