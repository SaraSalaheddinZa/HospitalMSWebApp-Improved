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
    public partial class Laboratorian : HospitalMSWebApp.Infrastructure.LegacyDirectoryPage
    {
        protected override string DirectoryRole { get { return "Admin"; } }
        Models.Functions conn;
        private int selectedLabId
        {
            get { return ViewState["selectedLabId"] == null ? 0 : (int)ViewState["selectedLabId"]; }
            set { ViewState["selectedLabId"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Check admin authentication
            if (Session["role"] == null || Session["role"].ToString() != "Admin")
            {
                Response.Redirect("../login.aspx");
                return;
            }

            conn = new Models.Functions();

            if (!IsPostBack)
            {
                LoadLaboratorians();
                LoadStatistics();
                ClearAlert();
            }
        }

        #region Data Loading

        private void LoadLaboratorians()
        {
            try
            {
                string query = "SELECT labID, labName, labEmail, labPhone, labAddress, labStatus FROM laboratorianTbl ORDER BY labName";
                DataTable dt = conn.GetDatas(query);
                LaboratorianGV.DataSource = dt;
                LaboratorianGV.DataBind();
            }
            catch (Exception ex)
            {
                ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex, "Error loading staff. Please review the form and try again; contact your administrator if the problem continues."), "error");
            }
        }

        private void LoadStatistics()
        {
            try
            {
                string totalQuery = "SELECT COUNT(*) FROM laboratorianTbl";
                DataTable totalDt = conn.GetDatas(totalQuery);
                totalStaff.InnerText = totalDt.Rows[0][0].ToString();

                string activeQuery = "SELECT COUNT(*) FROM laboratorianTbl WHERE labStatus = 'Active'";
                DataTable activeDt = conn.GetDatas(activeQuery);
                activeStaff.InnerText = activeDt.Rows[0][0].ToString();
            }
            catch { }
        }

        #endregion

        #region CRUD Operations

        protected void addBtn_Click(object sender, EventArgs e) { SaveDirectoryRecord(false); }
        protected void editBtn_Click(object sender, EventArgs e) { SaveDirectoryRecord(true); }
        private void SaveDirectoryRecord(bool update)
        {
            if (update && selectedLabId == 0) { ShowAlert("Select a staff member to update.", "error"); return; }
            if (!ValidateInputs(update)) return;

            try
            {
                var entry = new HospitalMSWebApp.Services.StaffDirectoryEntry {
                    Role="Laboratorian",Id=update?(int?)selectedLabId:null,Name=LabNameTb.Value,Email=EmailTb.Value,Phone=PhoneTb.Value,Address=LabAddressTb.Value,Gender=GenderCb.SelectedValue,Status=StatusCb.SelectedValue,Password=PasswordTb.Value,ConfirmEmailChange=Request.Form["confirmLoginEmail"]=="on",ConfirmPasswordChange=Request.Form["confirmPasswordChange"]=="on"
                };
                new HospitalMSWebApp.Services.HospitalWorkflowService().SaveStaffDirectoryEntry(entry,CurrentUser.UserId);
                LoadLaboratorians(); LoadStatistics(); ClearForm();
                ShowAlert("Staff profile and sign-in access saved.", "success");
            }
            catch (ArgumentException ex) { ShowAlert(ex.Message, "error"); }
            catch (Exception ex) { ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex,"The staff profile could not be saved. Check the email and selected record, then retry."),"error"); }
        }
        protected void deleteBtn_Click(object sender, EventArgs e)
        {
            if (selectedLabId == 0) { ShowAlert("Select a staff member to deactivate.", "error"); return; }
            try
            {
                new HospitalMSWebApp.Services.HospitalWorkflowService().SetDirectoryStaffActive("Laboratorian",selectedLabId,false,CurrentUser.UserId);
                LoadLaboratorians(); LoadStatistics(); ClearForm();
                ShowAlert("Staff access deactivated. Historical records were retained.","success");
            }
            catch (Exception ex) { ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex,"Staff access could not be changed. Refresh the directory and retry."),"error"); }
        }

        protected void clearBtn_Click(object sender, EventArgs e)
        {
            ClearForm();
            selectedLabId = 0;
            ClearAlert();
        }

        protected void LaboratorianGV_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                selectedLabId = Convert.ToInt32(LaboratorianGV.SelectedDataKey.Value);

                string query = "SELECT * FROM laboratorianTbl WHERE labID = @ID";
                DataTable dt = ExecuteParameterizedQuery(query, new SqlParameter("@ID", selectedLabId));

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    LabNameTb.Value = row["labName"].ToString();
                    EmailTb.Value = row["labEmail"].ToString();
                    PhoneTb.Value = row["labPhone"].ToString();
                    LabAddressTb.Value = row["labAddress"].ToString();
                    GenderCb.SelectedValue = row["labGender"].ToString();
                    StatusCb.SelectedValue = row["labStatus"].ToString();
                    PasswordTb.Value = "";
                }
            }
            catch (Exception ex)
            {
                ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex, "Error loading staff details. Please review the form and try again; contact your administrator if the problem continues."), "error");
            }
        }

        protected void LaboratorianGV_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            LaboratorianGV.PageIndex = e.NewPageIndex;
            LoadLaboratorians();
        }

        #endregion

        #region Helper Methods

        private bool ValidateInputs(bool isUpdate = false)
        {
            if (string.IsNullOrWhiteSpace(LabNameTb.Value))
            {
                ShowAlert("Please enter full name.", "error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTb.Value))
            {
                ShowAlert("Please enter email address.", "error");
                return false;
            }

            string emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(EmailTb.Value.Trim(), emailPattern))
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

            return true;
        }

        private bool EmailExists(string email, int excludeId = 0)
        {
            string query = excludeId > 0
                ? "SELECT COUNT(*) FROM laboratorianTbl WHERE labEmail = @Email AND labID != @ID"
                : "SELECT COUNT(*) FROM laboratorianTbl WHERE labEmail = @Email";

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

        private bool HasAssociatedRecords(int labId)
        {
            string query = "SELECT COUNT(*) FROM labTestTbl WHERE labID = @ID";
            DataTable dt = ExecuteParameterizedQuery(query, new SqlParameter("@ID", labId));
            return Convert.ToInt32(dt.Rows[0][0]) > 0;
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
            string icon = type == "success" ? "check-circle" : "exclamation-circle";
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
            LabNameTb.Value = "";
            EmailTb.Value = "";
            PhoneTb.Value = "";
            LabAddressTb.Value = "";
            GenderCb.SelectedIndex = 0;
            PasswordTb.Value = "";
            StatusCb.SelectedIndex = 0;
            selectedLabId = 0;
        }

        public override void VerifyRenderingInServerForm(Control control)
        {
        }

        #endregion
    }
}
