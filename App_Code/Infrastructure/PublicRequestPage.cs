using System;
using System.Web.UI;
namespace HospitalMSWebApp.Infrastructure
{
    // Session-bound ViewState plus one-use submission tokens protect anonymous assistance forms.
    public abstract class PublicRequestPage : Page
    {
        private string Key { get { return "PublicRequest:"+GetType().BaseType.FullName; } }
        protected override void OnInit(EventArgs e) { ViewStateUserKey=Session.SessionID;base.OnInit(e); }
        protected override void OnLoad(EventArgs e) { if(!IsPostBack)IssueToken();base.OnLoad(e); }
        private void IssueToken() { var token=Guid.NewGuid().ToString("N");Session[Key]=token;ViewState["RequestToken"]=token;Session[Key+":expires"]=DateTime.UtcNow.AddMinutes(30); }
        protected bool ConsumeSubmission()
        {
            var token=Convert.ToString(ViewState["RequestToken"]);
            var expires=Session[Key+":expires"] as DateTime?;
            bool valid=token.Length>0&&token==Convert.ToString(Session[Key])&&expires.HasValue&&expires.Value>DateTime.UtcNow;
            IssueToken();
            return valid;
        }
    }
}
