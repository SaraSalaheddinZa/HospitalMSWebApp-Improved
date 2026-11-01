using System;
using System.Web;
using System.Web.UI;
using HospitalMSWebApp.Infrastructure;

namespace HospitalMSWebApp.Views
{
    public partial class ResetPassword : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Headers["Referrer-Policy"] = "no-referrer";
            if (Request.HttpMethod == "POST") Reset();
        }
        protected override void Render(HtmlTextWriter writer)
        {
            var token = Request.HttpMethod == "POST" ? Request.Form["token"] : Request.QueryString["token"];
            var message = Convert.ToString(Context.Items["ResetMessage"]);
            var error = Convert.ToString(Context.Items["ResetError"]);
            writer.Write("<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Reset password | Guardian Health</title><link rel='stylesheet' href='../Content/guardian.css?v=20261008.r2'><link rel='stylesheet' href='../Content/guardian-auth.css?v=20261008.r2'></head><body class='gh-auth-simple'><main class='gh-auth-card'><section><p class='eyebrow'>GUARDIAN HEALTH</p><h1>Set a new password</h1><p>Create a password with at least 10 characters, including upper-case, lower-case, and numeric characters.</p>" +
                (string.IsNullOrEmpty(message) ? "" : "<div class='notice success'>" + H(message) + "</div>") + (string.IsNullOrEmpty(error) ? "" : "<div class='notice error'>" + H(error) + "</div>") +
                "<form method='post' class='form-grid'><input type='hidden' name='token' value='" + H(token) + "'><label>New password<input type='password' name='password' autocomplete='new-password' required></label><label>Confirm password<input type='password' name='confirmation' autocomplete='new-password' required></label><div class='form-action'><button class='button'>Update password</button></div></form><p><a href='login.aspx'>Return to sign in</a></p></section></main></body></html>");
        }
        private void Reset()
        {
            var token = Request.Form["token"] ?? string.Empty;
            var password = Request.Form["password"] ?? string.Empty;
            if (token.Length < 30) { ShowResetError("This reset link is invalid or expired."); return; }
            if (!string.Equals(password, Request.Form["confirmation"], StringComparison.Ordinal)) { ShowResetError("The two passwords do not match."); return; }
            string passwordError;
            if (!PasswordSecurity.IsAcceptable(password, out passwordError)) { ShowResetError(passwordError); return; }
            try
            {
                new PasswordRecoveryService().CompleteReset(token, password, Request.UserHostAddress);
                Context.Items["ResetMessage"] = "Your password has been updated. Return to sign in; existing access restrictions still apply.";
            }
            catch (ArgumentException ex) { ShowResetError(ex.Message); }
            catch (Exception ex) { PasswordRecoveryService.ReportFailure("redemption", ex); ShowResetError("We could not update the password. Please request a new link."); }
        }
        private void ShowResetError(string message) { Context.Items["ResetError"] = message; }
        private static string H(string value) { return HttpUtility.HtmlEncode(value ?? string.Empty); }
    }
}
