using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using HospitalMSWebApp.Infrastructure;

namespace HospitalMSWebApp.Views
{
    public partial class login : Page
    {
        private const int MaxSessionAttempts = 5;
        private const int LockoutMinutes = 15;

        // Presentation only. Authenticate still checks the selected role against the account.
        protected string EntryMode
        {
            get
            {
                if (roleCb.SelectedValue == "Patient") return "patient";
                if (!string.IsNullOrEmpty(roleCb.SelectedValue)) return "staff";
                return Request.Form["entryMode"] == "staff" || Request.QueryString["access"] == "staff" ? "staff" : "patient";
            }
        }

        protected override void OnInit(EventArgs e)
        {
            // Persist the anonymous session cookie before binding ViewState to its ID.
            // Merely reading SessionID on a fresh GET does not persist an ASP.NET session.
            if (Session["PublicLoginInitialized"] == null) Session["PublicLoginInitialized"] = true;
            ViewStateUserKey = Session.SessionID;
            base.OnInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            ExpireLegacyPasswordCookie();
            if (!IsPostBack)
            {
                LoadRememberedIdentity();
                // Entry-point preference only; authentication still validates the selected role against the account.
                var requestedRole=Request.QueryString["role"];
                if(!string.IsNullOrEmpty(requestedRole)&&roleCb.Items.FindByValue(requestedRole)!=null)roleCb.SelectedValue=requestedRole;
                else if(Request.QueryString["access"]=="staff")roleCb.SelectedIndex=0;
                else if(string.IsNullOrEmpty(roleCb.SelectedValue))roleCb.SelectedValue="Patient";
                CheckSessionLockout();
            }
        }

        protected void loginBtn_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;
            if (IsSessionLocked())
            {
                ShowError("Too many attempts. Please wait before trying again.");
                return;
            }

            try
            {
                var email = emailTb.Value.Trim();
                var role = roleCb.SelectedValue;
                var user = AuthenticationService.Authenticate(email, passwordTb.Value, role);
                if (user == null)
                {
                    RegisterFailedAttempt();
                    ShowError("Invalid email, password, or role.");
                    return;
                }

                AuthenticationService.SignIn(Context, user, rememberMeCb.Checked);
                RememberIdentity(email, role, rememberMeCb.Checked);
                Session["LoginAttempts"] = 0;
                Session.Remove("LockoutTime");
                AuditService.Write(user, "Login", "UserAccount", user.UserId.ToString(), "Successful interactive sign-in", Request);
                Response.Redirect(SafeDestination(role), false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Login failure: " + ex.Message);
                ShowError("We could not complete sign-in. Please try again or contact support.");
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(emailTb.Value) || string.IsNullOrWhiteSpace(passwordTb.Value))
            {
                ShowError("Enter your email address and password.");
                return false;
            }
            if (!Regex.IsMatch(emailTb.Value.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ShowError("Enter a valid email address.");
                return false;
            }
            if (string.IsNullOrEmpty(roleCb.SelectedValue))
            {
                ShowError("Select your role.");
                return false;
            }
            return true;
        }

        private void LoadRememberedIdentity()
        {
            var email = Request.Cookies["GuardianRememberedEmail"];
            var role = Request.Cookies["GuardianRememberedRole"];
            if (email != null && role != null)
            {
                emailTb.Value = Server.HtmlEncode(email.Value);
                if (roleCb.Items.FindByValue(role.Value) != null) roleCb.SelectedValue = role.Value;
                rememberMeCb.Checked = true;
            }
        }

        private void RememberIdentity(string email, string role, bool remember)
        {
            ExpireCookie("GuardianRememberedEmail");
            ExpireCookie("GuardianRememberedRole");
            if (!remember) return;
            var expiry = DateTime.UtcNow.AddDays(30);
            AddCookie("GuardianRememberedEmail", email, expiry);
            AddCookie("GuardianRememberedRole", role, expiry);
        }

        private void ExpireLegacyPasswordCookie()
        {
            ExpireCookie("Password");
            ExpireCookie("Email");
            ExpireCookie("Role");
        }

        private void AddCookie(string name, string value, DateTime expiry)
        {
            Response.Cookies.Add(new HttpCookie(name, value)
            {
                Expires = expiry, HttpOnly = true, Secure = Request.IsSecureConnection, SameSite = SameSiteMode.Lax
            });
        }

        private void ExpireCookie(string name)
        {
            if (Request.Cookies[name] == null) return;
            Response.Cookies.Add(new HttpCookie(name, "")
            {
                Expires = DateTime.UtcNow.AddDays(-1), HttpOnly = true, Secure = Request.IsSecureConnection, SameSite = SameSiteMode.Lax
            });
        }

        private bool IsSessionLocked()
        {
            var attempts = Session["LoginAttempts"] as int? ?? 0;
            var lockedAt = Session["LockoutTime"] as DateTime?;
            return attempts >= MaxSessionAttempts && lockedAt.HasValue && DateTime.UtcNow < lockedAt.Value.AddMinutes(LockoutMinutes);
        }

        private void CheckSessionLockout()
        {
            if (IsSessionLocked())
            {
                loginBtn.Enabled = false;
                ShowError("Too many attempts. Please wait before trying again.");
            }
            else if (Session["LockoutTime"] != null)
            {
                Session["LoginAttempts"] = 0;
                Session.Remove("LockoutTime");
            }
        }

        private void RegisterFailedAttempt()
        {
            var attempts = (Session["LoginAttempts"] as int? ?? 0) + 1;
            Session["LoginAttempts"] = attempts;
            if (attempts >= MaxSessionAttempts)
            {
                Session["LockoutTime"] = DateTime.UtcNow;
                loginBtn.Enabled = false;
            }
        }

        private string SafeDestination(string role)
        {
            var returnUrl = Request.QueryString["returnUrl"];
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/") && !returnUrl.StartsWith("//") && !returnUrl.Contains("\\")) return returnUrl;
            switch(role) {
                case "Doctor": return "Doctors/ClinicalWorkspace.aspx";
                case "Nurse": return "Nurse/Workspace.aspx";
                case "Pharmacist": return "Pharmacist/Workspace.aspx";
                case "Receptionist": return "Receptionist/Workspace.aspx";
                case "Laboratorian": return "Laboratorian/Workspace.aspx";
                case "Admin": return "Admin/Workspace.aspx";
                case "Patient": return "Patient/Workspace.aspx";
                default: return "Portal.aspx";
            }
        }

        private void ShowError(string message)
        {
            ErrMsg.InnerText = message;
            ErrMsg.Attributes["class"] = "feedback error";
        }

        public override void VerifyRenderingInServerForm(System.Web.UI.Control control) { }
    }
}
