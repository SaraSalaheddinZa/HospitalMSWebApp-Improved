using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Web.Services.Description;
using System.Web.UI;

namespace HospitalMSWebApp.Views
{
    public partial class ContactAdmin : HospitalMSWebApp.Infrastructure.PublicRequestPage
    {
        Models.Functions conn;

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
            if(!ConsumeSubmission()){ShowAlert("This form expired or was already submitted. Refresh and try again.", "error");return;}
            try
            {
                if (!ValidateInputs())
                {
                    return;
                }

                string name = nameTb.Value.Trim();
                string email = emailTb.Value.Trim().ToLower();
                string requestType = requestTypeCb.SelectedValue;
                string message = messageTb.Value.Trim();

                if (SaveContactRequest(name, email, requestType, message))
                {
                    SendAdminNotification(name, email, requestType, message);

                    ShowAlert("Your request has been recorded. Contact reception if you need immediate account assistance.", "success");
                    ClearForm();
                }
                else
                {
                    ShowAlert("An error occurred while sending your message. Please try again.", "error");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ContactAdmin Error: {ex.Message}");
                ShowAlert("An unexpected error occurred. Please try again later.", "error");
            }
        }

        #region Helper Methods

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(nameTb.Value))
            {
                ShowAlert("Please enter your full name.", "error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(emailTb.Value))
            {
                ShowAlert("Please enter your email address.", "error");
                return false;
            }

            string emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(emailTb.Value.Trim(), emailPattern))
            {
                ShowAlert("Please enter a valid email address.", "error");
                return false;
            }

            if (string.IsNullOrEmpty(requestTypeCb.SelectedValue))
            {
                ShowAlert("Please select a request type.", "error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(messageTb.Value))
            {
                ShowAlert("Please enter your message.", "error");
                return false;
            }

            if (messageTb.Value.Length < 10)
            {
                ShowAlert("Please provide more details in your message (at least 10 characters).", "error");
                return false;
            }

            return true;
        }

        private bool SaveContactRequest(string name, string email, string requestType, string message)
        {
            try
            {
                string query = @"INSERT INTO ContactRequests (FullName, Email, RequestType, Message, Status, CreatedAt, IPAddress) 
                                 VALUES (@Name, @Email, @Type, @Message, 'Pending', @CreatedAt, @IP)";

                ExecuteParameterizedQuery(query,
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Email", email),
                    new SqlParameter("@Type", requestType),
                    new SqlParameter("@Message", message),
                    new SqlParameter("@CreatedAt", DateTime.Now),
                    new SqlParameter("@IP", GetClientIP())
                );

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveContactRequest Error: {ex.Message}");
                return false;
            }
        }

        private void SendAdminNotification(string name, string email, string requestType, string message)
        {
            try
            {
                string subject = $"Guardian Health - New Contact Request: {requestType}";
                name=System.Web.HttpUtility.HtmlEncode(name);
                message=System.Web.HttpUtility.HtmlEncode(message);
                email=System.Web.HttpUtility.HtmlEncode(email);
                requestType=System.Web.HttpUtility.HtmlEncode(requestType);
                string body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <h2>New Contact Request</h2>
                        <table style='border-collapse: collapse; width: 100%; max-width: 600px;'>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Name:</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{name}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Email:</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{email}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Request Type:</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{requestType}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Message:</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{message}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Date:</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td>
                            </tr>
                        </table>
                        <p>Sign in through your usual Guardian Health staff address to review this request.</p>
                    </body>
                    </html>";

                string adminEmail = System.Configuration.ConfigurationManager.AppSettings["AdminEmail"];
                if(string.IsNullOrWhiteSpace(adminEmail))return;

                SendEmail(adminEmail, subject, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendAdminNotification Error: {ex.Message}");
            }
        }

        private void SendEmail(string to, string subject, string body)
        {
            try
            {
                string smtpServer = System.Configuration.ConfigurationManager.AppSettings["SmtpServer"] ?? "";
                string smtpUsername = System.Configuration.ConfigurationManager.AppSettings["SmtpUsername"] ?? "";
                string smtpPassword = System.Configuration.ConfigurationManager.AppSettings["SmtpPassword"] ?? "";

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername))
                {
                    System.Diagnostics.Debug.WriteLine($"Email would be sent to: {to}");
                    System.Diagnostics.Debug.WriteLine($"Subject: {subject}");
                    return;
                }

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(smtpUsername, "Guardian Health System");
                    mail.To.Add(to);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = true;

                    int smtpPort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                    using (SmtpClient smtp = new SmtpClient(smtpServer, smtpPort))
                    {
                        smtp.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendEmail Error: {ex.Message}");
            }
        }

        private DataTable ExecuteParameterizedQuery(string query, params SqlParameter[] parameters)
        {
            return new Models.Functions().GetDatas(query,parameters);
        }

        private string GetClientIP()
        {
            // A forwarded header is not trustworthy without a configured proxy boundary.
            return Request.UserHostAddress ?? "Unknown";
        }

        private void ShowAlert(string message, string type)
        {
            string icon = type == "success" ? "check-circle" : "exclamation-circle";
            alertBox.InnerHtml = System.Web.HttpUtility.HtmlEncode(message);
            alertBox.Attributes["class"] = type=="success"?"alert alert-success":"alert alert-danger";
        }

        private void ClearForm()
        {
            nameTb.Value = "";
            emailTb.Value = "";
            requestTypeCb.SelectedIndex = 0;
            messageTb.Value = "";
        }

        #endregion
    }
}
