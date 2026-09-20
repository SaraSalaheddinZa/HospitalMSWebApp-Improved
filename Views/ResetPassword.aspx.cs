using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using HospitalMSWebApp.Infrastructure;

namespace HospitalMSWebApp.Views
{
    public partial class ResetPassword : Page
    {
        private readonly Database database = new Database();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.HttpMethod == "POST") Reset();
        }
        protected override void Render(HtmlTextWriter writer)
        {
            var token = Request.HttpMethod == "POST" ? Request.Form["token"] : Request.QueryString["token"];
            var message = Convert.ToString(Context.Items["ResetMessage"]);
            var error = Convert.ToString(Context.Items["ResetError"]);
            writer.Write("<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Reset password | Guardian Health</title><link rel='stylesheet' href='../Content/portal.css'></head><body><main class='workspace' style='margin:0;max-width:620px'><section class='panel form-panel'><p class='eyebrow'>GUARDIAN HEALTH</p><h1>Set a new password</h1><p>Create a password with at least 10 characters, including upper-case, lower-case, and numeric characters.</p>" +
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
                database.InTransaction((connection, transaction) =>
                {
                    var tokenId = new SqlCommand(@"SELECT TOP (1) PasswordResetTokenId FROM gh.PasswordResetTokens WITH (UPDLOCK, HOLDLOCK)
                        WHERE TokenHash=@Hash AND UsedAt IS NULL AND ExpiresAt > SYSUTCDATETIME() ORDER BY CreatedAt DESC", connection, transaction);
                    tokenId.Parameters.AddWithValue("@Hash", ForgotPassword.TokenHash(token));
                    var id = tokenId.ExecuteScalar();
                    if (id == null) throw new InvalidOperationException("This reset link is invalid or expired.");
                    using (var account = new SqlCommand(@"UPDATE gh.UserAccounts SET PasswordHash=@PasswordHash, PasswordChangedAt=SYSUTCDATETIME(), FailedLoginCount=0, LockedUntil=NULL
                        WHERE UserId=(SELECT UserId FROM gh.PasswordResetTokens WHERE PasswordResetTokenId=@TokenId); UPDATE gh.PasswordResetTokens SET UsedAt=SYSUTCDATETIME() WHERE PasswordResetTokenId=@TokenId;", connection, transaction))
                    {
                        account.Parameters.AddWithValue("@PasswordHash", PasswordSecurity.Hash(password)); account.Parameters.AddWithValue("@TokenId", id); account.ExecuteNonQuery();
                    }
                    return 0;
                });
                Context.Items["ResetMessage"] = "Your password has been updated. You can now sign in.";
            }
            catch (InvalidOperationException ex) { ShowResetError(ex.Message); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("Password reset failed: " + ex); ShowResetError("We could not update the password. Please request a new link."); }
        }
        private void ShowResetError(string message) { Context.Items["ResetError"] = message; }
        private static string H(string value) { return HttpUtility.HtmlEncode(value ?? string.Empty); }
    }
}
