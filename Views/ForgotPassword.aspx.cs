using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;

namespace HospitalMSWebApp.Views
{
    public partial class ForgotPassword : System.Web.UI.Page
    {
        Models.Functions conn;
        private const int TOKEN_EXPIRY_HOURS = 24;

        protected void Page_Load(object sender, EventArgs e)
        {
            conn = new Models.Functions();

            if (!IsPostBack)
            {
                alertBox.Attributes["class"] = "alert";
            }
        }

        protected void submitBtn_Click(object sender, EventArgs e)
        {
            try
            {
                string email = emailTb.Text.Trim().ToLower();
                string role = roleCb.SelectedValue;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(role))
                {
                    ShowAlert("Please fill in all required fields.", "error");
                    return;
                }

                UserInfo user = GetUserByEmailAndRole(email, role);

                if (user == null)
                {
                    // Don't reveal if email exists for security
                    ShowAlert("If an account exists with this email, you will receive a password reset link shortly.", "info");
                    ClearForm();
                    return;
                }

                string resetToken = GenerateResetToken();
                DateTime expiryDate = DateTime.Now.AddHours(TOKEN_EXPIRY_HOURS);

                if (SaveResetToken(user.UserId, role, resetToken, expiryDate))
                {
                    if (SendResetEmail(email, user.Name, resetToken, role))
                    {
                        ShowAlert("Password reset link has been sent to your email. Please check your inbox.", "success");
                        ClearForm();
                    }
                    else
                    {
                        ShowAlert("Unable to send email at this time. Please try again later or contact support.", "error");
                    }
                }
                else
                {
                    ShowAlert("An error occurred. Please try again later.", "error");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ForgotPassword Error: {ex.Message}");
                ShowAlert("An unexpected error occurred. Please try again later.", "error");
            }
        }

        #region Helper Methods

        private UserInfo GetUserByEmailAndRole(string email, string role)
        {
            string query = "";
            string idColumn = "";
            string nameColumn = "";
            string emailColumn = "";

            switch (role)
            {
                case "Admin":
                    query = "SELECT AdminId, AdminName, AdminEmail FROM AdminTbl WHERE AdminEmail = @Email AND IsActive = 1";
                    idColumn = "AdminId";
                    nameColumn = "AdminName";
                    emailColumn = "AdminEmail";
                    break;
                case "Doctor":
                    query = "SELECT doctorID, doctorName, doctorEmail FROM doctorTbl WHERE doctorEmail = @Email AND doctorStatus = 'Active'";
                    idColumn = "doctorID";
                    nameColumn = "doctorName";
                    emailColumn = "doctorEmail";
                    break;
                case "Receptionist":
                    query = "SELECT receptionistID, receptionistName, receptionistEmail FROM receptionistTbl WHERE receptionistEmail = @Email AND receptionistStatus = 'Active'";
                    idColumn = "receptionistID";
                    nameColumn = "receptionistName";
                    emailColumn = "receptionistEmail";
                    break;
                case "Laboratorian":
                    query = "SELECT labID, labName, labEmail FROM laboratorianTbl WHERE labEmail = @Email AND labStatus = 'Active'";
                    idColumn = "labID";
                    nameColumn = "labName";
                    emailColumn = "labEmail";
                    break;
                default:
                    return null;
            }

            try
            {
                DataTable dt = ExecuteParameterizedQuery(query, new SqlParameter("@Email", email));

                if (dt.Rows.Count > 0)
                {
                    return new UserInfo
                    {
                        UserId = dt.Rows[0][idColumn].ToString(),
                        Name = dt.Rows[0][nameColumn].ToString(),
                        Email = dt.Rows[0][emailColumn].ToString(),
                        Role = role
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetUserByEmail Error: {ex.Message}");
            }

            return null;
        }

        private bool SaveResetToken(string userId, string role, string token, DateTime expiryDate)
        {
            try
            {
                // First, invalidate any existing tokens for this user
                string invalidateQuery = "UPDATE PasswordResetTokens SET IsUsed = 1 WHERE UserId = @UserId AND UserRole = @Role AND IsUsed = 0";
                ExecuteParameterizedQuery(invalidateQuery,
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Role", role));

                // Insert new token
                string insertQuery = @"INSERT INTO PasswordResetTokens (UserId, UserRole, ResetToken, ExpiryDate, CreatedAt, IsUsed) 
                                       VALUES (@UserId, @Role, @Token, @Expiry, @Created, 0)";

                ExecuteParameterizedQuery(insertQuery,
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Role", role),
                    new SqlParameter("@Token", token),
                    new SqlParameter("@Expiry", expiryDate),
                    new SqlParameter("@Created", DateTime.Now));

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveResetToken Error: {ex.Message}");
                return false;
            }
        }

        private bool SendResetEmail(string email, string name, string token, string role)
        {
            try
            {
                string resetUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}/Views/ResetPassword.aspx?token={token}&role={role}";

                string subject = "Guardian Health - Password Reset Request";
                string body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h1 style='color: #2563eb;'>Guardian Health</h1>
                            </div>
                            <h2>Password Reset Request</h2>
                            <p>Hello {name},</p>
                            <p>We received a request to reset your password for your Guardian Health account. Click the button below to reset your password:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetUrl}' style='background: linear-gradient(135deg, #2563eb, #7c3aed); color: white; padding: 14px 32px; text-decoration: none; border-radius: 8px; display: inline-block; font-weight: 600;'>Reset Password</a>
                            </div>
                            <p>Or copy and paste this link into your browser:</p>
                            <p style='background: #f1f5f9; padding: 12px; border-radius: 6px; word-break: break-all;'>{resetUrl}</p>
                            <p>This link will expire in {TOKEN_EXPIRY_HOURS} hours.</p>
                            <p>If you didn't request this password reset, please ignore this email or contact your administrator if you have concerns.</p>
                            <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 30px 0;'>
                            <p style='font-size: 0.875rem; color: #64748b;'>This is an automated message from Guardian Health Hospital Management System.</p>
                        </div>
                    </body>
                    </html>";

                // Configure SMTP settings from web.config
                string smtpServer = System.Configuration.ConfigurationManager.AppSettings["SmtpServer"] ?? "smtp.gmail.com";
                int smtpPort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                string smtpUsername = System.Configuration.ConfigurationManager.AppSettings["SmtpUsername"] ?? "";
                string smtpPassword = System.Configuration.ConfigurationManager.AppSettings["SmtpPassword"] ?? "";
                string fromEmail = System.Configuration.ConfigurationManager.AppSettings["FromEmail"] ?? "noreply@guardianhealth.com";

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    // Log that email would be sent (for development)
                    System.Diagnostics.Debug.WriteLine($"Email would be sent to: {email}");
                    System.Diagnostics.Debug.WriteLine($"Reset URL: {resetUrl}");
                    return true; // Return true for development without SMTP
                }

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(fromEmail, "Guardian Health");
                    mail.To.Add(email);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = true;

                    using (SmtpClient smtp = new SmtpClient(smtpServer, smtpPort))
                    {
                        smtp.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendResetEmail Error: {ex.Message}");
                return false;
            }
        }

        private string GenerateResetToken()
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
            }
        }

        private DataTable ExecuteParameterizedQuery(string query, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["HospitalDB"]?.ConnectionString;

            if (string.IsNullOrEmpty(connString))
            {
                // Fallback to Functions class
                if (parameters.Length > 0)
                {
                    string modifiedQuery = query;
                    foreach (var param in parameters)
                    {
                        modifiedQuery = modifiedQuery.Replace(param.ParameterName, $"'{param.Value}'");
                    }
                    return conn.GetDatas(modifiedQuery);
                }
                return conn.GetDatas(query);
            }

            using (SqlConnection connection = new SqlConnection(connString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    adapter.Fill(dt);
                }
            }

            return dt;
        }

        private void ShowAlert(string message, string type)
        {
            alertBox.InnerHtml = $"<i class='fas fa-{GetIconForType(type)}'></i> {message}";
            alertBox.Attributes["class"] = $"alert alert-{type} show";
        }

        private string GetIconForType(string type)
        {
            switch (type)
            {
                case "success":
                    return "check-circle";
                case "error":
                    return "exclamation-circle";
                default:
                    return "info-circle";
            }
        }

        private void ClearForm()
        {
            emailTb.Text = "";
            roleCb.SelectedIndex = 0;
        }

        #endregion

        #region Inner Classes

        private class UserInfo
        {
            public string UserId { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
        }

        #endregion
    }
}