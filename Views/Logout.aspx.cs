using System;
using System.Web.UI;
using HospitalMSWebApp.Infrastructure;

namespace HospitalMSWebApp.Views
{
    public partial class Logout : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) PerformLogout();
        }

        private void PerformLogout()
        {
            try
            {
                int id;
                var actor = new AuthenticatedUser
                {
                    UserId = int.TryParse(Convert.ToString(Session["uid"]), out id) ? id : 0,
                    Role = Convert.ToString(Session["role"]), Name = Convert.ToString(Session["userName"])
                };
                if (actor.UserId > 0)
                    AuditService.Write(actor, "Logout", "UserAccount", actor.UserId.ToString(), "Interactive sign-out", Request);
                AuthenticationService.SignOut(Context);
                Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
                Response.Cache.SetNoStore();
                Response.Expires = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Logout failure: " + ex.Message);
            }
        }
    }
}
