using System;
using System.Web.UI;
using HospitalMSWebApp.Infrastructure;

namespace HospitalMSWebApp.Views
{
    public partial class SignOut : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            var actor = AuthenticationService.GetTicketUser(Context);
            if (actor != null)
                AuditService.Write(actor, "Logout", "UserAccount", actor.UserId.ToString(), "Interactive sign-out", Request);
            AuthenticationService.SignOut(Context);
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Expires = -1;
        }
    }
}
