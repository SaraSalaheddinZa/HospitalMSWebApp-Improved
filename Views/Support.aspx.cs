using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Web.UI;

namespace HospitalMSWebApp.Views
{
    public partial class Support : System.Web.UI.Page
    {
        Models.Functions conn;

        protected void Page_Load(object sender, EventArgs e)
        {
            conn = new Models.Functions();

            if (!IsPostBack)
            {
                ClearAlert();
            }
        }

        protected void submitBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInputs())
                    return;

                string name = nameTb.Text.Trim();
                string email = emailTb.Text.Trim().ToLower();
                string category = categoryCb.SelectedValue;
                string priority = priorityCb.SelectedValue;
                string subject = subjectTb.Value.Trim();
                string description = descriptionTb.Text.Trim();

                string ticketNumber = GenerateTicketNumber();

                if (SaveSupportTicket(ticketNumber, name, email, category, priority, subject, description))
                {
                    SendTicketConfirmation(email, name, ticketNumber, subject);
                    ShowAlert($"Ticket submitted! Your reference number is <strong>{ticketNumber}</strong>. We'll be in touch shortly.", "success");
                    ClearForm();
                }
                else
                {
                    ShowAlert("Something went wrong while submitting your ticket. Please try again.", "error");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Support Error: {ex.Message}");
                ShowAlert("An unexpected error occurred. Please try again later.", "error");
            }
        }

        #region Validation

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(nameTb.Text))
            {
                ShowAlert("Please enter your full name.", "error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(emailTb.Text))
            {
                ShowAlert("Please enter your email address.", "error");
                return false;
            }

            string emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(emailTb.Text.Trim(), emailPattern))
            {
                ShowAlert("Please enter a valid email address.", "error");
                return false;
            }

            if (string.IsNullOrEmpty(categoryCb.SelectedValue))
            {
                ShowAlert("Please select an issue category.", "error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(subjectTb.Value))
            {
                ShowAlert("Please enter a subject for your ticket.", "error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(descriptionTb.Text))
            {
                ShowAlert("Please provide a description of your issue.", "error");
                return false;
            }

            if (descriptionTb.Text.Trim().Length < 20)
            {
                ShowAlert("Please provide more detail in your description (at least 20 characters).", "error");
                return false;
            }

            return true;
        }

        #endregion

        #region Ticket Helpers

        private string GenerateTicketNumber()
        {
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            string randomPart = new Random().Next(1000, 9999).ToString();
            return $"SUP-{datePart}-{randomPart}";
        }

        private bool SaveSupportTicket(string ticketNumber, string name, string email,
            string category, string priority, string subject, string description)
        {
            try
            {
                string query = @"INSERT INTO SupportTickets 
                    (TicketNumber, FullName, Email, Category, Priority, Subject, Description, Status, CreatedAt, IPAddress) 
                    VALUES 
                    (@TicketNumber, @Name, @Email, @Category, @Priority, @Subject, @Description, 'Open', @CreatedAt, @IP)";

                ExecuteParameterizedQuery(query,
                    new SqlParameter("@TicketNumber", ticketNumber),
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Email", email),
                    new SqlParameter("@Category", category),
                    new SqlParameter("@Priority", priority),
                    new SqlParameter("@Subject", subject),
                    new SqlParameter("@Description", description),
                    new SqlParameter("@CreatedAt", DateTime.Now),
                    new SqlParameter("@IP", GetClientIP())
                );

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSupportTicket Error: {ex.Message}");
                return false;
            }
        }

        private void SendTicketConfirmation(string email, string name, string ticketNumber, string subject)
        {
            try
            {
                string smtpServer = System.Configuration.ConfigurationManager.AppSettings["SmtpServer"] ?? "";
                string smtpUsername = System.Configuration.ConfigurationManager.AppSettings["SmtpUsername"] ?? "";
                string smtpPassword = System.Configuration.ConfigurationManager.AppSettings["SmtpPassword"] ?? "";

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername))
                {
                    System.Diagnostics.Debug.WriteLine($"[Dev] Ticket confirmation would be sent to: {email}");
                    return;
                }

                string mailSubject = $"Guardian Health — Support Ticket #{ticketNumber}";
                string body = $@"
                    <html><body style='font-family:sans-serif;color:#333;line-height:1.6'>
                    <div style='max-width:600px;margin:0 auto;padding:32px'>
                        <h2 style='color:#0ea5e9'>Guardian Health Support</h2>
                        <p>Hello {name},</p>
                        <p>Your support ticket has been received. Here are your details:</p>
                        <div style='background:#f7f8fc;border-radius:10px;padding:20px;margin:20px 0'>
                            <p><strong>Ticket #:</strong> {ticketNumber}</p>
                            <p><strong>Subject:</strong> {subject}</p>
                            <p><strong>Submitted:</strong> {DateTime.Now:yyyy-MM-dd HH:mm}</p>
                        </div>
                        <p>We'll respond within 2 hours for high-priority issues, or within 24 hours otherwise.</p>
                        <p style='font-size:0.8rem;color:#888'>This is an automated message — please do not reply directly.</p>
                    </div></body></html>";

                int smtpPort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["SmtpPort"] ?? "587");

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(smtpUsername, "Guardian Health Support");
                    mail.To.Add(email);
                    mail.Subject = mailSubject;
                    mail.Body = body;
                    mail.IsBodyHtml = true;

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
                System.Diagnostics.Debug.WriteLine($"SendTicketConfirmation Error: {ex.Message}");
            }
        }

        #endregion

        #region Infrastructure

        private DataTable ExecuteParameterizedQuery(string query, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["HospitalDB"]?.ConnectionString;

            if (string.IsNullOrEmpty(connString))
            {
                string modifiedQuery = query;
                foreach (var param in parameters)
                    modifiedQuery = modifiedQuery.Replace(param.ParameterName, $"'{param.Value}'");
                return conn.GetDatas(modifiedQuery);
            }

            using (SqlConnection connection = new SqlConnection(connString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddRange(parameters);
                new SqlDataAdapter(command).Fill(dt);
            }

            return dt;
        }

        private string GetClientIP()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip))
                ip = Request.ServerVariables["REMOTE_ADDR"];
            return ip ?? "Unknown";
        }

        // Alert classes match the frontend CSS (.alert-box.show.success / .error)
        private void ShowAlert(string message, string type)
        {
            string icon = type == "success" ? "check-circle" : "exclamation-circle";
            alertBox.InnerHtml = $"<i class='fas fa-{icon}'></i> {message}";
            alertBox.Attributes["class"] = $"alert-box show {type}";
        }

        private void ClearAlert()
        {
            alertBox.Attributes["class"] = "alert-box";
            alertBox.InnerHtml = "";
        }

        private void ClearForm()
        {
            nameTb.Text = "";
            emailTb.Text = "";
            categoryCb.SelectedIndex = 0;
            priorityCb.SelectedIndex = 1; // Medium
            subjectTb.Value = "";
            descriptionTb.Text = "";
        }

        #endregion
    }
}