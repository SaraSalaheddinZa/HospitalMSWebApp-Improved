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
                int accountId;
                if (Session["accountId"] == null || Session["role"] == null || !int.TryParse(Session["accountId"].ToString(), out accountId)) return null;
                int roleEntityId;
                int patientId;
                return new AuthenticatedUser
                {
                    UserId = accountId,
                    RoleEntityId = int.TryParse(Convert.ToString(Session["uid"]), out roleEntityId) ? (int?)roleEntityId : null,
                    Role = Session["role"].ToString(), Name = Convert.ToString(Session["userName"]), Email = Convert.ToString(Session["userEmail"]),
                    PatientId = int.TryParse(Convert.ToString(Session["patientId"]), out patientId) ? (int?)patientId : null
                };
            }
        }

        protected bool RequireRoles(params string[] roles)
        {
            var user = CurrentUser;
            if (user == null || !roles.Contains(user.Role, StringComparer.OrdinalIgnoreCase))
            {
                AuthenticationService.SignOut(Context);
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
