using System;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using HospitalMSWebApp.Infrastructure;

namespace HospitalMSWebApp
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            DevelopmentDataSeeder.EnsureDevelopmentAccounts();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // Retire obsolete mutation endpoints before Web Forms can dispatch their postbacks.
            string path=Request.AppRelativeCurrentExecutionFilePath;
            string destination=null;
            // Keep bookmarked scaffold/legacy pages inside the maintained public experience.
            if(path.Equals("~/About.aspx",StringComparison.OrdinalIgnoreCase)||path.Equals("~/About",StringComparison.OrdinalIgnoreCase)) destination="~/Views/Guest/Home.aspx#about";
            if(path.Equals("~/Contact.aspx",StringComparison.OrdinalIgnoreCase)||path.Equals("~/Contact",StringComparison.OrdinalIgnoreCase)) destination="~/Views/HelpCenter.aspx";
            if(path.Equals("~/WebForm1.aspx",StringComparison.OrdinalIgnoreCase)||path.Equals("~/WebForm1",StringComparison.OrdinalIgnoreCase)) destination="~/Views/Guest/Home.aspx";
            if(path.Equals("~/Views/Logout.aspx",StringComparison.OrdinalIgnoreCase)||path.Equals("~/Views/Logout",StringComparison.OrdinalIgnoreCase)) destination="~/Views/SignOut.aspx";
            if(path.Equals("~/Views/Doctors/Prescriptions.aspx",StringComparison.OrdinalIgnoreCase)||path.Equals("~/Views/Doctors/Prescriptions",StringComparison.OrdinalIgnoreCase)) destination="~/Views/Doctors/ClinicalWorkspace.aspx";
            if(path.Equals("~/Views/Laboratorian/LabTest.aspx",StringComparison.OrdinalIgnoreCase)||path.Equals("~/Views/Laboratorian/LabTest",StringComparison.OrdinalIgnoreCase)) destination="~/Views/Laboratorian/Workspace.aspx";
            if(path.Equals("~/Default.aspx",StringComparison.OrdinalIgnoreCase)||path.Equals("~/",StringComparison.OrdinalIgnoreCase)) destination="~/Views/Guest/Home.aspx";
            if(destination!=null){Response.Redirect(destination,false);Context.ApplicationInstance.CompleteRequest();}
        }

        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            if (Context == null || Context.Session == null || !Request.IsAuthenticated) return;
            if (!AuthenticationService.RestoreSession(Context)) AuthenticationService.SignOut(Context);
        }

        protected void Application_AuthorizeRequest(object sender, EventArgs e)
        {
            var requiredRole = RequiredRole(Request.AppRelativeCurrentExecutionFilePath);
            if (requiredRole == null) return;
            var user = AuthenticationService.GetTicketUser(Context);
            if (user == null || (requiredRole != "Any" && !string.Equals(user.Role, requiredRole, StringComparison.OrdinalIgnoreCase)))
            {
                AuthenticationService.SignOut(Context);
                Response.Redirect("~/Views/login.aspx?returnUrl=" + Server.UrlEncode(Request.RawUrl), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
        }

        private static string RequiredRole(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (path.Equals("~/Views/Portal.aspx", StringComparison.OrdinalIgnoreCase)) return "Any";
            if (path.Equals("~/Default.aspx", StringComparison.OrdinalIgnoreCase)) return "Admin";
            if (path.StartsWith("~/Views/Admin/", StringComparison.OrdinalIgnoreCase)) return "Admin";
            if (path.StartsWith("~/Views/Doctors/", StringComparison.OrdinalIgnoreCase)) return "Doctor";
            if (path.StartsWith("~/Views/Receptionist/", StringComparison.OrdinalIgnoreCase)) return "Receptionist";
            if (path.StartsWith("~/Views/Laboratorian/", StringComparison.OrdinalIgnoreCase)) return "Laboratorian";
            if (path.StartsWith("~/Views/Nurse/", StringComparison.OrdinalIgnoreCase)) return "Nurse";
            if (path.StartsWith("~/Views/Pharmacist/", StringComparison.OrdinalIgnoreCase)) return "Pharmacist";
            if (path.StartsWith("~/Views/Patient/", StringComparison.OrdinalIgnoreCase)) return "Patient";
            if (path.StartsWith("~/Views/Documents/", StringComparison.OrdinalIgnoreCase)) return "Any";
            if (path.StartsWith("~/Views/Records/", StringComparison.OrdinalIgnoreCase)) return "Any";
            if (path.StartsWith("~/Views/Search/", StringComparison.OrdinalIgnoreCase)) return "Any";
            if (path.StartsWith("~/Views/Notifications/", StringComparison.OrdinalIgnoreCase)) return "Any";
            return null;
        }
    }
}
