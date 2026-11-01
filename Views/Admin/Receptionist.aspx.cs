using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web;
using HospitalMSWebApp.Services;

namespace HospitalMSWebApp.Views.Admin
{
    public partial class Receptionist : HospitalMSWebApp.Infrastructure.LegacyDirectoryPage
    {
        protected override string DirectoryRole { get { return "Admin"; } }
        Models.Functions conn;
        private readonly HospitalWorkflowService workflows = new HospitalWorkflowService();
        private int selectedRecId
        {
            get { return ViewState["selectedRecId"] != null ? (int)ViewState["selectedRecId"] : 0; }
            set { ViewState["selectedRecId"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["role"] == null || Session["role"].ToString() != "Admin")
            {
                Response.Redirect("../login.aspx");
                return;
            }

            conn = new Models.Functions();

            if (!IsPostBack)
            {
                LoadReceptionists();
                LoadStatistics();
                ClearAlert();
            }
        }

        #region Data Loading

        private void LoadReceptionists()
        {
            try
            {
                string query = "SELECT receptionistID, receptionistName, receptionistEmail, receptionistPhone, receptionistAdd, receptionistStatus FROM receptionistTbl ORDER BY receptionistName";
                DataTable dt = conn.GetDatas(query);
                ReceptionistGV.DataSource = dt;
                ReceptionistGV.DataBind();
            }
            catch (Exception ex)
            {
                ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex, "Error loading receptionists. Please review the form and try again; contact your administrator if the problem continues."), "error");
            }
        }

        private void LoadStatistics()
        {
            try
            {
                string totalQuery = "SELECT COUNT(*) FROM receptionistTbl";
                DataTable totalDt = conn.GetDatas(totalQuery);
                totalReceptionists.InnerText = totalDt.Rows[0][0].ToString();

                string activeQuery = "SELECT COUNT(*) FROM receptionistTbl WHERE receptionistStatus = 'Active'";
                DataTable activeDt = conn.GetDatas(activeQuery);
                activeReceptionists.InnerText = activeDt.Rows[0][0].ToString();
            }
            catch { }
        }

        #endregion

        #region CRUD Operations

        protected void addBtn_Click(object sender, EventArgs e) { SaveDirectoryRecord(false); }
        protected void editBtn_Click(object sender, EventArgs e) { SaveDirectoryRecord(true); }
        private void SaveDirectoryRecord(bool update)
        {
            if (update && selectedRecId == 0) { ShowAlert("Select a staff member to update.", "error"); return; }
            if (!ValidateInputs(update)) return;

            try
            {
                var entry = new HospitalMSWebApp.Services.StaffDirectoryEntry {
                    Role="Receptionist",Id=update?(int?)selectedRecId:null,Name=RecNameTb.Value,Email=RecEmailTb.Value,Phone=PhoneTb.Value,Address=AddressTb.Value,Status=StatusCb.SelectedValue,Password=PasswordTb.Value,ConfirmEmailChange=Request.Form["confirmLoginEmail"]=="on",ConfirmPasswordChange=Request.Form["confirmPasswordChange"]=="on"
                };
                new HospitalMSWebApp.Services.HospitalWorkflowService().SaveStaffDirectoryEntry(entry,CurrentUser.UserId);
                LoadReceptionists(); LoadStatistics(); ClearForm();
                ShowAlert("Staff profile and sign-in access saved.", "success");
            }
            catch (ArgumentException ex) { ShowAlert(ex.Message, "error"); }
            catch (Exception ex) { ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex,"The staff profile could not be saved. Check the email and selected record, then retry."),"error"); }
        }
        protected void deleteBtn_Click(object sender, EventArgs e)
        {
            if (selectedRecId == 0) { ShowAlert("Select a staff member to deactivate.", "error"); return; }
            try
            {
                new HospitalMSWebApp.Services.HospitalWorkflowService().SetDirectoryStaffActive("Receptionist",selectedRecId,false,CurrentUser.UserId);
                LoadReceptionists(); LoadStatistics(); ClearForm();
                ShowAlert("Staff access deactivated. Historical records were retained.","success");
            }
            catch (Exception ex) { ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex,"Staff access could not be changed. Refresh the directory and retry."),"error"); }
        }

        protected void clearBtn_Click(object sender, EventArgs e)
        {
            ClearForm();
            selectedRecId = 0;
            ClearAlert();
        }

        protected void ReceptionistGV_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                selectedRecId = Convert.ToInt32(ReceptionistGV.SelectedDataKey.Value);

                string query = "SELECT * FROM receptionistTbl WHERE receptionistID = @ID";
                DataTable dt = ExecuteParameterizedQuery(query, new SqlParameter("@ID", selectedRecId));

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    RecNameTb.Value = row["receptionistName"].ToString();
                    RecEmailTb.Value = row["receptionistEmail"].ToString();
                    PhoneTb.Value = row["receptionistPhone"].ToString();
                    AddressTb.Value = row["receptionistAdd"].ToString();
                    StatusCb.SelectedValue = row["receptionistStatus"].ToString();
                    PasswordTb.Value = "";
                }
            }
            catch (Exception ex)
            {
                ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex, "Error loading receptionist details. Please review the form and try again; contact your administrator if the problem continues."), "error");
            }
        }

        protected void ReceptionistGV_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            ReceptionistGV.PageIndex = e.NewPageIndex;
            LoadReceptionists();
        }

        #endregion

        #region Validation & Helpers

        private bool ValidateInputs(bool isUpdate = false)
        {
            if (string.IsNullOrWhiteSpace(RecNameTb.Value))
            {
                ShowAlert("Please enter a full name.", "error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(RecEmailTb.Value))
            {
                ShowAlert("Please enter an email address.", "error");
                return false;
            }

            string emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(RecEmailTb.Value.Trim(), emailPattern))
            {
                ShowAlert("Please enter a valid email address.", "error");
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
                ? "SELECT COUNT(*) FROM receptionistTbl WHERE receptionistEmail = @Email AND receptionistID != @ID"
                : "SELECT COUNT(*) FROM receptionistTbl WHERE receptionistEmail = @Email";

            var parameters = new System.Collections.Generic.List<SqlParameter>
            {
                new SqlParameter("@Email", email)
            };

            if (excludeId > 0)
                parameters.Add(new SqlParameter("@ID", excludeId));

            DataTable dt = ExecuteParameterizedQuery(query, parameters.ToArray());
            return Convert.ToInt32(dt.Rows[0][0]) > 0;
        }

        private bool HasAssociatedRecords(int recId)
        {
            string apptQuery = "SELECT COUNT(*) FROM appointmentTbl WHERE createdBy = @ID";
            DataTable apptDt = ExecuteParameterizedQuery(apptQuery, new SqlParameter("@ID", recId));
            if (Convert.ToInt32(apptDt.Rows[0][0]) > 0) return true;

            string patientQuery = "SELECT COUNT(*) FROM patientTbl WHERE addBy = @ID";
            DataTable patientDt = ExecuteParameterizedQuery(patientQuery, new SqlParameter("@ID", recId));
            if (Convert.ToInt32(patientDt.Rows[0][0]) > 0) return true;

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
            string icon = type == "success" ? "check-circle" : "exclamation-circle";
            alertBox.InnerHtml = $"<i class='fas fa-{icon}'></i> {HttpUtility.HtmlEncode(message)}";
            alertBox.Attributes["class"] = $"alert-box show {type}";
        }

        private void ClearAlert()
        {
            alertBox.Attributes["class"] = "alert-box";
            alertBox.InnerHtml = "";
        }

        private void ClearForm()
        {
            RecNameTb.Value = "";
            RecEmailTb.Value = "";
            PhoneTb.Value = "";
            AddressTb.Value = "";
            PasswordTb.Value = "";
            StatusCb.SelectedIndex = 0;
            selectedRecId = 0;
        }

        public override void VerifyRenderingInServerForm(Control control) { }

        #endregion
    }
}
