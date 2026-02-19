using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text.RegularExpressions;

namespace HospitalMSWebApp.Views
{
    public partial class login : System.Web.UI.Page
    {
        Models.Functions conn;
        private const int MAX_LOGIN_ATTEMPTS = 5;
        private const int LOCKOUT_DURATION_MINUTES = 15;

        protected void Page_Load(object sender, EventArgs e)
        {
            conn = new Models.Functions();

            if (!IsPostBack)
            {
                LoadSavedCredentials();

                CheckAccountLockout();
            }
        }

        public override void VerifyRenderingInServerForm(Control control)
        {
        }

        private void LoadSavedCredentials()
        {
            try
            {
                if (Request.Cookies["Email"] != null &&
                    Request.Cookies["Password"] != null &&
                    Request.Cookies["Role"] != null)
                {
                    emailTb.Value = Server.HtmlEncode(Request.Cookies["Email"].Value);
                    passwordTb.Value = Server.HtmlEncode(Request.Cookies["Password"].Value);
                    roleCb.SelectedValue = Server.HtmlEncode(Request.Cookies["Role"].Value);
                    rememberMeCb.Checked = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading credentials: " + ex.Message);
            }
        }
        private void CheckAccountLockout()
        {
            if (Session["LoginAttempts"] != null && Session["LockoutTime"] != null)
            {
                int attempts = (int)Session["LoginAttempts"];
                DateTime lockoutTime = (DateTime)Session["LockoutTime"];

                if (attempts >= MAX_LOGIN_ATTEMPTS)
                {
                    TimeSpan timeSinceLockout = DateTime.Now - lockoutTime;

                    if (timeSinceLockout.TotalMinutes < LOCKOUT_DURATION_MINUTES)
                    {
                        int remainingMinutes = LOCKOUT_DURATION_MINUTES - (int)timeSinceLockout.TotalMinutes;
                        ShowError($"Account locked. Please try again after {remainingMinutes} minutes.");
                        loginBtn.Enabled = false;

                        string script = $"document.getElementById('loginAttempts').classList.add('show'); " +
                                      $"document.getElementById('attemptsText').textContent = 'Maximum attempts exceeded. Please wait {remainingMinutes} minutes';";
                        ClientScript.RegisterStartupScript(this.GetType(), "lockout", script, true);
                        return;
                    }
                    else
                    {
                        // Lockout period expired, reset
                        Session["LoginAttempts"] = 0;
                        Session["LockoutTime"] = null;
                        loginBtn.Enabled = true;
                    }
                }
            }
        }

        protected void loginBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInputs())
                {
                    return;
                }

                string email = SanitizeInput(emailTb.Value.Trim());
                string password = SanitizeInput(passwordTb.Value);
                int roleIndex = roleCb.SelectedIndex;

                if (!IsValidEmail(email))
                {
                    ShowError("Invalid email format");
                    return;
                }

                bool loginSuccess = false;

                switch (roleIndex)
                {
                    case 1: // Admin
                        loginSuccess = LoginAsAdmin(email, password);
                        break;
                    case 2: // Doctor
                        loginSuccess = LoginAsDoctor(email, password);
                        break;
                    case 3: // Receptionist
                        loginSuccess = LoginAsReceptionist(email, password);
                        break;
                    case 4: // Laboratorian
                        loginSuccess = LoginAsLaboratorian(email, password);
                        break;
                    default:
                        ShowError("Please select a role");
                        return;
                }

                if (loginSuccess)
                {
                    Session["LoginAttempts"] = 0;
                    Session["LockoutTime"] = null;

                    if (rememberMeCb.Checked)
                    {
                        SaveCredentials(email, password, roleCb.SelectedValue);
                    }
                    else
                    {
                        ClearSavedCredentials();
                    }

                    string script = "localStorage.setItem('loginAttempts', '0');";
                    ClientScript.RegisterStartupScript(this.GetType(), "loginSuccess", script, true);
                }
                else
                {
                    IncrementLoginAttempts();
                }
            }
            catch (Exception ex)
            {
                ShowError("An error occurred during login. Please try again later.");
                System.Diagnostics.Debug.WriteLine("Login error: " + ex.Message);
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(emailTb.Value))
            {
                ShowError("Please enter your email address");
                return false;
            }

            if (string.IsNullOrWhiteSpace(passwordTb.Value))
            {
                ShowError("Please enter your password");
                return false;
            }

            if (roleCb.SelectedIndex == 0)
            {
                ShowError("Please select a user role");
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Replace("'", "''")
                        .Replace("--", "")
                        .Replace(";", "")
                        .Replace("/*", "")
                        .Replace("*/", "")
                        .Replace("xp_", "")
                        .Replace("sp_", "");
        }
        private bool LoginAsAdmin(string email, string password)
        {
            if (email == "Admin@gmail.com" && password == "Admin")
            {
                Session["uid"] = 1;
                Session["role"] = "Admin";
                Session["userName"] = "Administrator";
                Session.Timeout = 30;

                ShowSuccess("Login successful!");

                // Delay redirect slightly to show success message
                Response.AddHeader("REFRESH", "1;URL=Admin/Doctors.aspx");
                return true;
            }
            else
            {
                ShowError("Invalid email or password");
                return false;
            }
        }

        private bool LoginAsDoctor(string email, string password)
        {
            try
            {
                string query = "SELECT doctorID, doctorName, doctorEmail FROM doctorTbl WHERE doctorEmail='{0}' AND doctorPassword='{1}'"; query = string.Format(query, email, password);
                query = string.Format(query, email, password);
                DataTable dataTbl = conn.GetDatas(query);

                if (dataTbl.Rows.Count == 0)
                {
                    ShowError("Invalid email or password");
                    return false;
                }
                else
                {
                    Session["uid"] = dataTbl.Rows[0]["doctorID"].ToString();
                    Session["role"] = "Doctors";
                    Session["userName"] = dataTbl.Rows[0]["doctorName"].ToString();
                    Session["userEmail"] = dataTbl.Rows[0]["doctorEmail"].ToString();
                    Session.Timeout = 30;

                    ShowSuccess("Welcome, Dr. " + dataTbl.Rows[0]["doctorName"].ToString());
                    Response.AddHeader("REFRESH", "1;URL=Doctors/Prescriptions.aspx");
                    return true;
                }
            }
            catch (Exception ex)
            {
                ShowError("Authentication error occurred");
                System.Diagnostics.Debug.WriteLine("Doctor login error: " + ex.Message);
                return false;
            }
        }

        private bool LoginAsReceptionist(string email, string password)
        {
            try
            {
                string query = "SELECT receptionistID, receptionistName, receptionistEmail FROM receptionistTbl WHERE receptionistEmail='{0}' AND receptionistPassword='{1}'";
                query = string.Format(query, email, password);
                DataTable dataTbl = conn.GetDatas(query);

                if (dataTbl.Rows.Count == 0)
                {
                    ShowError("Invalid email or password");
                    return false;
                }
                else
                {
                    Session["uid"] = dataTbl.Rows[0]["receptionistID"].ToString();
                    Session["role"] = "Receptionist";
                    Session["userName"] = dataTbl.Rows[0]["receptionistName"].ToString();
                    Session["userEmail"] = dataTbl.Rows[0]["receptionistEmail"].ToString();
                    Session.Timeout = 30;

                    ShowSuccess("Welcome, " + dataTbl.Rows[0]["receptionistName"].ToString());

                    Response.AddHeader("REFRESH", "1;URL=Receptionist/Patients.aspx");
                    return true;
                }
            }
            catch (Exception ex)
            {
                ShowError("Authentication error occurred");
                System.Diagnostics.Debug.WriteLine("Receptionist login error: " + ex.Message);
                return false;
            }
        }

        private bool LoginAsLaboratorian(string email, string password)
        {
            try
            {
                string query = "SELECT labID, labName, labEmail FROM laboratorianTbl WHERE labEmail='{0}' AND labPassword='{1}'";
                query = string.Format(query, email, password);
                DataTable dataTbl = conn.GetDatas(query);

                if (dataTbl.Rows.Count == 0)
                {
                    ShowError("Invalid email or password");
                    return false;
                }
                else
                {
                    Session["uid"] = dataTbl.Rows[0]["labID"].ToString();
                    Session["role"] = "Laboratorian";
                    Session["userName"] = dataTbl.Rows[0]["labName"].ToString();
                    Session["userEmail"] = dataTbl.Rows[0]["labEmail"].ToString();
                    Session.Timeout = 30;

                    ShowSuccess("Welcome, " + dataTbl.Rows[0]["labName"].ToString());

                    Response.AddHeader("REFRESH", "1;URL=Laboratorian/LabTest.aspx");
                    return true;
                }
            }
            catch (Exception ex)
            {
                ShowError("Authentication error occurred");
                System.Diagnostics.Debug.WriteLine("Laboratorian login error: " + ex.Message);
                return false;
            }
        }

        private void IncrementLoginAttempts()
        {
            int attempts = 0;
            if (Session["LoginAttempts"] != null)
            {
                attempts = (int)Session["LoginAttempts"];
            }

            attempts++;
            Session["LoginAttempts"] = attempts;

            if (attempts >= MAX_LOGIN_ATTEMPTS)
            {
                Session["LockoutTime"] = DateTime.Now;
                ShowError($"Maximum attempts exceeded. Please wait {LOCKOUT_DURATION_MINUTES} minutes.");
                loginBtn.Enabled = false;

                // Register JavaScript to update display
                string script = $"localStorage.setItem('loginAttempts', '{attempts}'); " +
                              $"document.getElementById('loginAttempts').classList.add('show'); " +
                              $"document.getElementById('attemptsText').textContent = 'Maximum attempts exceeded. Please wait {LOCKOUT_DURATION_MINUTES} minutes';";
                ClientScript.RegisterStartupScript(this.GetType(), "maxAttempts", script, true);
            }
            else
            {
                int remaining = MAX_LOGIN_ATTEMPTS - attempts;
                string script = $"localStorage.setItem('loginAttempts', '{attempts}'); " +
                              $"updateLoginAttempts();";
                ClientScript.RegisterStartupScript(this.GetType(), "updateAttempts", script, true);
            }
        }

        private void SaveCredentials(string email, string password, string role)
        {
            try
            {
                HttpCookie emailCookie = new HttpCookie("Email", email);
                HttpCookie passwordCookie = new HttpCookie("Password", password);
                HttpCookie roleCookie = new HttpCookie("Role", role);

                DateTime expiration = DateTime.Now.AddDays(30);
                emailCookie.Expires = expiration;
                passwordCookie.Expires = expiration;
                roleCookie.Expires = expiration;

                emailCookie.HttpOnly = true;
                passwordCookie.HttpOnly = true;
                roleCookie.HttpOnly = true;

                Response.Cookies.Add(emailCookie);
                Response.Cookies.Add(passwordCookie);
                Response.Cookies.Add(roleCookie);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving credentials: " + ex.Message);
            }
        }

        private void ClearSavedCredentials()
        {
            try
            {
                if (Request.Cookies["Email"] != null)
                {
                    HttpCookie emailCookie = new HttpCookie("Email");
                    emailCookie.Expires = DateTime.Now.AddDays(-1);
                    Response.Cookies.Add(emailCookie);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error clearing credentials: " + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            ErrMsg.InnerText = message;
            ErrMsg.Attributes["class"] = "error";
            string script = "setTimeout(function() { document.getElementById('ErrMsg').style.display = 'none'; }, 5000);";
            ClientScript.RegisterStartupScript(this.GetType(), "hideError", script, true);
        }

        private void ShowSuccess(string message)
        {
            ErrMsg.InnerText = message;
            ErrMsg.Attributes["class"] = "success";
        }
    }
}