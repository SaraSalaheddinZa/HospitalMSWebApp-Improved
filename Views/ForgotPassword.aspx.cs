using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.UI;
using HospitalMSWebApp.Infrastructure;

namespace HospitalMSWebApp.Views
{
    public partial class ForgotPassword : PublicRequestPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Headers["Referrer-Policy"] = "no-referrer";
            if (!IsPostBack) alertBox.Attributes["class"] = "alert";
        }

        protected void submitBtn_Click(object sender, EventArgs e)
        {
            if (!ConsumeSubmission()) { ShowAlert("This request was already submitted or has expired. Refresh and try again.", "error"); return; }
            var email = emailTb.Text.Trim();
            var role = roleCb.SelectedValue;
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(role))
            {
                ShowAlert("Enter your email address and select your role.", "error");
                return;
            }

            try
            {
                var user = new PasswordRecoveryService().RequestReset(email, role, Request.UserHostAddress);
                if (user != null)
                {
                    try { SendResetEmail(user.Email, user.Name, user.Token); }
                    catch (Exception ex) { PasswordRecoveryService.ReportFailure("delivery", ex); }
                }
                // The response stays identical to prevent account enumeration.
                ShowAlert("If an active account matches those details, password-reset instructions will be sent shortly.", "success");
                ClearForm();
            }
            catch (Exception ex)
            {
                PasswordRecoveryService.ReportFailure("request", ex);
                // Never reveal whether only the matching-account branch failed.
                ShowAlert("If an active account matches those details, password-reset instructions will be sent shortly.", "success");
                ClearForm();
            }
        }

        private bool SendResetEmail(string email, string name, string token)
        {
            var host = System.Configuration.ConfigurationManager.AppSettings["SmtpHost"];
            var username = System.Configuration.ConfigurationManager.AppSettings["SmtpUsername"];
            var password = System.Configuration.ConfigurationManager.AppSettings["SmtpPassword"];
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return false;
            int port;
            if (!int.TryParse(System.Configuration.ConfigurationManager.AppSettings["SmtpPort"], out port)) port = 587;
            Uri origin;
            var configuredOrigin=Environment.GetEnvironmentVariable("HOSPITALMS_PUBLIC_ORIGIN")??ConfigurationManager.AppSettings["PublicOrigin"];
            if(!Uri.TryCreate(configuredOrigin,UriKind.Absolute,out origin)||origin.Scheme!=Uri.UriSchemeHttps||!string.IsNullOrEmpty(origin.UserInfo)||origin.AbsolutePath!="/"||!string.IsNullOrEmpty(origin.Query)||!string.IsNullOrEmpty(origin.Fragment)){
                System.Diagnostics.Trace.TraceError("Password reset email requires a configured HTTPS HOSPITALMS_PUBLIC_ORIGIN (origin only).");
                return false;
            }
            var resetUrl = origin.GetLeftPart(UriPartial.Authority) + ResolveUrl("~/Views/ResetPassword.aspx") + "?token=" + HttpUtility.UrlEncode(token);
            using (var message = new MailMessage())
            {
                message.From = new MailAddress(username, "Guardian Health");
                message.To.Add(email);
                message.Subject = "Guardian Health password reset";
                message.IsBodyHtml = true;
                message.Body = "<p>Hello " + HttpUtility.HtmlEncode(name) + ",</p><p>Use this link to reset your password. It expires in 30 minutes:</p><p><a href='" + HttpUtility.HtmlAttributeEncode(resetUrl) + "'>Reset password</a></p><p>If you did not make this request, you can ignore this message.</p>";
                using (var client = new SmtpClient(host, port))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(username, password);
                    client.Send(message);
                }
            }
            return true;
        }

        private void ShowAlert(string message, string type)
        {
            alertBox.InnerText = message;
            alertBox.Attributes["class"] = "alert alert-" + type + " show";
        }
        private void ClearForm() { emailTb.Text = ""; roleCb.SelectedIndex = 0; }
    }
}
