using System;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;
using HospitalMSWebApp.Infrastructure;

namespace HospitalMSWebApp.Views
{
    public partial class ForgotPassword : Page
    {
        private readonly Database database = new Database();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) alertBox.Attributes["class"] = "alert";
        }

        protected void submitBtn_Click(object sender, EventArgs e)
        {
            var email = emailTb.Text.Trim();
            var role = roleCb.SelectedValue;
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(role))
            {
                ShowAlert("Enter your email address and select your role.", "error");
                return;
            }

            try
            {
                var user = FindAccount(email, role);
                if (user != null)
                {
                    var token = GenerateToken();
                    SaveToken(user.UserId, token);
                    SendResetEmail(user.Email, user.Name, token);
                }
                // The response stays identical to prevent account enumeration.
                ShowAlert("If an active account matches those details, password-reset instructions will be sent shortly.", "success");
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Password reset request failed: " + ex);
                ShowAlert("We could not process that request. Please try again later.", "error");
            }
        }

        private ResetUser FindAccount(string email, string role)
        {
            var accounts = database.Query(@"SELECT TOP (1) UserId, DisplayName, Email FROM gh.UserAccounts
                WHERE NormalizedEmail = @Email AND RoleName = @Role AND IsActive = 1",
                Database.Parameter("@Email", email.ToUpperInvariant()), Database.Parameter("@Role", role));
            if (accounts.Rows.Count == 0) return null;
            return new ResetUser
            {
                UserId = Convert.ToInt32(accounts.Rows[0]["UserId"]), Name = Convert.ToString(accounts.Rows[0]["DisplayName"]), Email = Convert.ToString(accounts.Rows[0]["Email"])
            };
        }

        private void SaveToken(int userId, string token)
        {
            database.Execute("UPDATE gh.PasswordResetTokens SET UsedAt = SYSUTCDATETIME() WHERE UserId = @UserId AND UsedAt IS NULL",
                Database.Parameter("@UserId", userId));
            database.Execute(@"INSERT INTO gh.PasswordResetTokens(UserId, TokenHash, ExpiresAt, RequestedIp)
                VALUES(@UserId, @TokenHash, DATEADD(MINUTE, 30, SYSUTCDATETIME()), @Ip)", Database.Parameter("@UserId", userId),
                Database.Parameter("@TokenHash", TokenHash(token)), Database.Parameter("@Ip", Request.UserHostAddress));
        }

        private bool SendResetEmail(string email, string name, string token)
        {
            var host = System.Configuration.ConfigurationManager.AppSettings["SmtpHost"];
            var username = System.Configuration.ConfigurationManager.AppSettings["SmtpUsername"];
            var password = System.Configuration.ConfigurationManager.AppSettings["SmtpPassword"];
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return false;
            int port;
            if (!int.TryParse(System.Configuration.ConfigurationManager.AppSettings["SmtpPort"], out port)) port = 587;
            var resetUrl = Request.Url.Scheme + "://" + Request.Url.Authority + ResolveUrl("~/Views/ResetPassword.aspx") + "?token=" + HttpUtility.UrlEncode(token);
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

        private static string GenerateToken()
        {
            var bytes = new byte[32];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
        internal static byte[] TokenHash(string token)
        {
            using (var hash = SHA256.Create()) return hash.ComputeHash(Encoding.UTF8.GetBytes(token));
        }
        private void ShowAlert(string message, string type)
        {
            alertBox.InnerText = message;
            alertBox.Attributes["class"] = "alert alert-" + type + " show";
        }
        private void ClearForm() { emailTb.Text = ""; roleCb.SelectedIndex = 0; }
        private sealed class ResetUser { public int UserId; public string Name; public string Email; }
    }
}
