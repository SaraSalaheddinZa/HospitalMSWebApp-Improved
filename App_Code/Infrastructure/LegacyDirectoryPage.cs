using System;
using System.Collections.Generic;

namespace HospitalMSWebApp.Infrastructure
{
    // Protect existing server-control directories without changing their postback contracts.
    public abstract class LegacyDirectoryPage : AppPage
    {
        protected abstract string DirectoryRole { get; }
        private string Key { get { return "DirectorySubmissions:" + Request.AppRelativeCurrentExecutionFilePath; } }
        protected override void OnInit(EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
            base.OnInit(e);
        }
        protected override void OnLoad(EventArgs e)
        {
            if (!RequireRoles(DirectoryRole)) return;
            if (Request.HttpMethod == "POST")
            {
                var tokens = Session[Key] as Dictionary<string, DateTime>;
                string token = Convert.ToString(ViewState["DirectorySubmission"]);
                DateTime expires;
                if (tokens == null || !tokens.TryGetValue(token, out expires) || expires < DateTime.UtcNow)
                {
                    Session["DirectoryError"] = "This form was already submitted or has expired. Review the refreshed page before trying again.";
                    // End this lifecycle before Web Forms dispatches any button/row events.
                    Response.Redirect(Request.Path, true);
                    return;
                }
                tokens.Remove(token);
            }
            base.OnLoad(e);
        }
        protected override void OnPreRender(EventArgs e)
        {
            var tokens = Session[Key] as Dictionary<string, DateTime>;
            if (tokens == null) { tokens = new Dictionary<string, DateTime>(); Session[Key] = tokens; }
            if (tokens.Count > 200) tokens.Clear();
            string token = Guid.NewGuid().ToString("N");
            tokens[token] = DateTime.UtcNow.AddMinutes(30);
            ViewState["DirectorySubmission"] = token;
            base.OnPreRender(e);
        }
    }
}
