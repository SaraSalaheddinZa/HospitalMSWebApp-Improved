using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace HospitalMSWebApp.Views.Admin
{
    public partial class Doctors : HospitalMSWebApp.Infrastructure.LegacyDirectoryPage
    {
        protected override string DirectoryRole { get { return "Admin"; } }
        Models.Functions conn;
        private int selectedDoctorId
        {
            get { return ViewState["selectedDoctorId"] == null ? 0 : (int)ViewState["selectedDoctorId"]; }
            set { ViewState["selectedDoctorId"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            //admin authentication
            if (Session["role"] == null || Session["role"].ToString() != "Admin")
            {
                Response.Redirect("../login.aspx");
                return;
            }

            conn = new Models.Functions();

            if (!IsPostBack)
            {
                LoadDoctors();
                LoadStatistics();
                ClearAlert();
            }
        }

        #region Data Loading
        private void LoadDoctors()
        {
            try
            {
                string query = "SELECT doctorID, doctorName, doctorEmail, doctorPhone, doctorSpec, doctorExp, doctorStatus FROM doctorTbl ORDER BY doctorName";
                DataTable dt = conn.GetDatas(query);
                DoctorsGV.DataSource = dt;
                DoctorsGV.DataBind();
            }
            catch (Exception ex)
            {
                ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex, "Error loading doctors. Please review the form and try again; contact your administrator if the problem continues."), "error");
            }
        }

        private void LoadStatistics()
        {
            try
            {
                string totalQuery = "SELECT COUNT(*) FROM doctorTbl";
                DataTable totalDt = conn.GetDatas(totalQuery);
                totalDoctors.InnerText = totalDt.Rows[0][0].ToString();

                string activeQuery = "SELECT COUNT(*) FROM doctorTbl WHERE doctorStatus = 'Active'";
                DataTable activeDt = conn.GetDatas(activeQuery);
                activeDoctors.InnerText = activeDt.Rows[0][0].ToString();

                string specQuery = "SELECT COUNT(DISTINCT doctorSpec) FROM doctorTbl WHERE doctorSpec IS NOT NULL AND doctorSpec != ''";
                DataTable specDt = conn.GetDatas(specQuery);
                specializations.InnerText = specDt.Rows[0][0].ToString();
            }
            catch { }
        }

        #endregion

        #region CRUD Operations

        protected void addBtn_Click(object sender, EventArgs e) { SaveDirectoryRecord(false); }
        protected void editBtn_Click(object sender, EventArgs e) { SaveDirectoryRecord(true); }
        private void SaveDirectoryRecord(bool update)
        {
            if (update && selectedDoctorId == 0) { ShowAlert("Select a staff member to update.", "error"); return; }
            if (!ValidateInputs(update)) return;
            DateTime parsed;DateTime? dob=DateTime.TryParseExact(DOBTb.Value,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out parsed)?(DateTime?)parsed:null;
            int years;int? experience=int.TryParse(DocExpTb.Value,out years)?(int?)years:null;
            try
            {
                var entry = new HospitalMSWebApp.Services.StaffDirectoryEntry {
                    Role="Doctor",Id=update?(int?)selectedDoctorId:null,Name=DocNameTb.Value,Email=emailTb.Text,Phone=DocPhoneTb.Value,Address=AddressTb.Text,Gender=GenderCb.SelectedValue,Status=StatusCb.SelectedValue,Password=PasswordTb.Value,Specialty=SpecialisationTb.Text,BirthDate=dob,Experience=experience,ConfirmEmailChange=Request.Form["confirmLoginEmail"]=="on",ConfirmPasswordChange=Request.Form["confirmPasswordChange"]=="on"
                };
                new HospitalMSWebApp.Services.HospitalWorkflowService().SaveStaffDirectoryEntry(entry,CurrentUser.UserId);
                LoadDoctors(); LoadStatistics(); ClearForm();
                ShowAlert("Staff profile and sign-in access saved.", "success");
            }
            catch (ArgumentException ex) { ShowAlert(ex.Message, "error"); }
            catch (Exception ex) { ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex,"The staff profile could not be saved. Check the email and selected record, then retry."),"error"); }
        }
        protected void deleteBtn_Click(object sender, EventArgs e)
        {
            if (selectedDoctorId == 0) { ShowAlert("Select a staff member to deactivate.", "error"); return; }
            try
            {
                new HospitalMSWebApp.Services.HospitalWorkflowService().SetDirectoryStaffActive("Doctor",selectedDoctorId,false,CurrentUser.UserId);
                LoadDoctors(); LoadStatistics(); ClearForm();
                ShowAlert("Staff access deactivated. Historical records were retained.","success");
            }
            catch (Exception ex) { ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex,"Staff access could not be changed. Refresh the directory and retry."),"error"); }
        }

        protected void clearBtn_Click(object sender, EventArgs e)
        {
            ClearForm();
            selectedDoctorId = 0;
            ClearAlert();
        }

        protected void DoctorsGV_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                selectedDoctorId = Convert.ToInt32(DoctorsGV.SelectedDataKey.Value);

                string query = "SELECT * FROM doctorTbl WHERE doctorID = @ID";
                DataTable dt = ExecuteParameterizedQuery(query, new SqlParameter("@ID", selectedDoctorId));

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    DocNameTb.Value = row["doctorName"].ToString();
                    DocPhoneTb.Value = row["doctorPhone"].ToString();
                    DocExpTb.Value = row["doctorExp"].ToString();
                    SpecialisationTb.Text = row["doctorSpec"].ToString();
                    GenderCb.SelectedValue = row["doctorGender"].ToString();
                    AddressTb.Text = row["doctorAdd"].ToString();
                    DOBTb.Value = Convert.ToDateTime(row["doctorDOB"]).ToString("yyyy-MM-dd");
                    emailTb.Text = row["doctorEmail"].ToString();
                    StatusCb.SelectedValue = row["doctorStatus"].ToString();
                    PasswordTb.Value = ""; // Don't show password
                }
            }
            catch (Exception ex)
            {
                ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex, "Error loading doctor details. Please review the form and try again; contact your administrator if the problem continues."), "error");
            }
        }

        protected void DoctorsGV_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            DoctorsGV.PageIndex = e.NewPageIndex;
            LoadDoctors();
        }

        #endregion

        #region Helper Methods

        private bool ValidateInputs(bool isUpdate = false)
        {
            if (string.IsNullOrWhiteSpace(DocNameTb.Value))
            {
                ShowAlert("Please enter doctor name.", "error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(emailTb.Text))
            {
                ShowAlert("Please enter email address.", "error");
                return false;
            }

            string emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(emailTb.Text.Trim(), emailPattern))
            {
                ShowAlert("Please enter a valid email address.", "error");
                return false;
            }

            if (string.IsNullOrEmpty(GenderCb.SelectedValue))
            {
                ShowAlert("Please select gender.", "error");
                return false;
            }

            if (!isUpdate && string.IsNullOrEmpty(PasswordTb.Value))
            {
                ShowAlert("Please enter a password.", "error");
                return false;
            }

            string passwordError;
            if (!string.IsNullOrEmpty(PasswordTb.Value) && !HospitalMSWebApp.Infrastructure.PasswordSecurity.IsAcceptable(PasswordTb.Value, out passwordError))
            {
                ShowAlert(passwordError, "error");
                return false;
            }

            if (!string.IsNullOrEmpty(DocExpTb.Value) && !int.TryParse(DocExpTb.Value, out _))
            {
                ShowAlert("Experience must be a valid number.", "error");
                return false;
            }

            return true;
        }

        private bool EmailExists(string email, int excludeId = 0)
        {
            string query = excludeId > 0
                ? "SELECT COUNT(*) FROM doctorTbl WHERE doctorEmail = @Email AND doctorID != @ID"
                : "SELECT COUNT(*) FROM doctorTbl WHERE doctorEmail = @Email";

            var parameters = new System.Collections.Generic.List<SqlParameter>
            {
                new SqlParameter("@Email", email)
            };

            if (excludeId > 0)
            {
                parameters.Add(new SqlParameter("@ID", excludeId));
            }

            DataTable dt = ExecuteParameterizedQuery(query, parameters.ToArray());
            return Convert.ToInt32(dt.Rows[0][0]) > 0;
        }

        private bool HasAssociatedRecords(int doctorId)
        {
            string apptQuery = "SELECT COUNT(*) FROM appointmentTbl WHERE doctorID = @ID";
            DataTable apptDt = ExecuteParameterizedQuery(apptQuery, new SqlParameter("@ID", doctorId));
            if (Convert.ToInt32(apptDt.Rows[0][0]) > 0) return true;

            string prescQuery = "SELECT COUNT(*) FROM prescriptionTbl WHERE doctorID = @ID";
            DataTable prescDt = ExecuteParameterizedQuery(prescQuery, new SqlParameter("@ID", doctorId));
            if (Convert.ToInt32(prescDt.Rows[0][0]) > 0) return true;

            return false;
        }

        private string HashPassword(string password)
        {
            return HospitalMSWebApp.Infrastructure.PasswordSecurity.Hash(password);
        }

        private DataTable ExecuteParameterizedQuery(string query, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            string connString = Models.Functions.ConnectionString;

            if (string.IsNullOrEmpty(connString)) throw new InvalidOperationException("Database configuration is unavailable.");

            using (SqlConnection connection = new SqlConnection(connString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters);
                    Models.Functions.OpenConfiguredConnection(connection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    adapter.Fill(dt);
                }
            }

            return dt;
        }

        private void ShowAlert(string message, string type)
        {
            string icon = type == "success" ? "check-circle" : type == "error" ? "exclamation-circle" : "info-circle";
            alertBox.InnerHtml = $"<i class='fas fa-{icon}'></i> {HttpUtility.HtmlEncode(message)}";
            alertBox.Attributes["class"] = $"alert alert-{type} show";
        }

        private void ClearAlert()
        {
            alertBox.Attributes["class"] = "alert";
            alertBox.InnerHtml = "";
        }

        private void ClearForm()
        {
            DocNameTb.Value = "";
            DocPhoneTb.Value = "";
            DocExpTb.Value = "";
            SpecialisationTb.Text = "";
            GenderCb.SelectedIndex = 0;
            AddressTb.Text = "";
            DOBTb.Value = "";
            PasswordTb.Value = "";
            emailTb.Text = "";
            StatusCb.SelectedIndex = 0;
            selectedDoctorId = 0;
        }

        public override void VerifyRenderingInServerForm(Control control)
        {
        }

        #endregion
    }
}
