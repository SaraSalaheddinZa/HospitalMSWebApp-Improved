using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;

namespace HospitalMSWebApp.Views.Receptionist
{
    public partial class Patients : LegacyDirectoryPage
    {
        protected override string DirectoryRole { get { return "Receptionist"; } }
        Models.Functions conn;
        private readonly HospitalWorkflowService workflows = new HospitalWorkflowService();

        private int selectedPatientId
        {
            get { return ViewState["selectedPatientId"] != null ? (int)ViewState["selectedPatientId"] : 0; }
            set { ViewState["selectedPatientId"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("Receptionist")) return;

            conn = new Models.Functions();

            if (!IsPostBack)
            {
                LoadPatients();
                LoadStatistics();
                ClearAlert();
            }
        }

        #region Data Loading

        private void LoadPatients()
        {
            try
            {
                string query = "SELECT patientId, MedicalRecordNumber, patientName, patientPhone, patientGender, patientDOB, patientAdd, patientAllergies FROM patientTbl WHERE IsActive=1 ORDER BY patientName";
                DataTable dt = conn.GetDatas(query);
                PatientListGV.DataSource = dt;
                PatientListGV.DataBind();
            }
            catch (Exception ex)
            {
                ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex, "Error loading patients. Please review the form and try again; contact your administrator if the problem continues."), "error");
            }
        }

        private void LoadStatistics()
        {
            try
            {
                string totalQuery = "SELECT COUNT(*) FROM patientTbl WHERE IsActive=1";
                DataTable totalDt = conn.GetDatas(totalQuery);
                totalPatients.InnerText = totalDt.Rows[0][0].ToString();

                string todayQuery = "SELECT COUNT(*) FROM patientTbl WHERE IsActive=1 AND CAST(registeredAt AS DATE) = CAST(GETDATE() AS DATE)";
                try
                {
                    DataTable todayDt = conn.GetDatas(todayQuery);
                    todayPatients.InnerText = todayDt.Rows[0][0].ToString();
                }
                catch { todayPatients.InnerText = "—"; }
            }
            catch { }
        }

        public string GetGenderClass(string gender)
        {
            switch (gender?.ToLower())
            {
                case "male": return "gender-m";
                case "female": return "gender-f";
                default: return "gender-o";
            }
        }

        #endregion

        #region CRUD Operations

        protected void addBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInputs())
                    return;

                string name = PatNameTb.Value.Trim();
                string phone = PhoneTb.Value.Trim();
                string gender = GenderCb.SelectedValue;
                string address = AddressTb.Value.Trim();
                string allergies = AllergyTb.Value.Trim();
                var receptionistId = CurrentUser == null ? 0 : CurrentUser.RoleEntityId.GetValueOrDefault();
                if (receptionistId < 1) throw new InvalidOperationException("Your account is not linked to a receptionist profile.");
                var patientId = workflows.RegisterPatient(name, phone, gender, ParseDate(DOBTb.Value), address, allergies,
                    null, null, null, null, null, receptionistId);
                AuditService.Write(CurrentUser, "PatientCreated", "Patient", patientId.ToString(), "Patient registered from receptionist workspace", Request);

                LoadPatients();
                LoadStatistics();
                ClearForm();
                ShowAlert("Patient registered successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex, "Error registering patient. Please review the form and try again; contact your administrator if the problem continues."), "error");
            }
        }

        protected void editBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedPatientId == 0)
                {
                    ShowAlert("Please select a patient to update.", "error");
                    return;
                }

                if (!ValidateInputs())
                    return;

                string name = PatNameTb.Value.Trim();
                string phone = PhoneTb.Value.Trim();
                string gender = GenderCb.SelectedValue;
                string address = AddressTb.Value.Trim();
                string allergies = AllergyTb.Value.Trim();
                workflows.UpdatePatientBasic(selectedPatientId, name, phone, gender, ParseDate(DOBTb.Value), address, allergies);
                AuditService.Write(CurrentUser, "PatientUpdated", "Patient", selectedPatientId.ToString(), "Patient demographics updated from receptionist workspace", Request);

                LoadPatients();
                LoadStatistics();
                ClearForm();
                ShowAlert("Patient updated successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex, "Error updating patient. Please review the form and try again; contact your administrator if the problem continues."), "error");
            }
        }

        protected void deleteBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedPatientId == 0)
                {
                    ShowAlert("Please select a patient to deactivate.", "error");
                    return;
                }

                workflows.DeactivatePatient(selectedPatientId);
                AuditService.Write(CurrentUser, "PatientDeactivated", "Patient", selectedPatientId.ToString(), "Patient record deactivated from receptionist workspace", Request);

                LoadPatients();
                LoadStatistics();
                ClearForm();
                ShowAlert("Patient deactivated. Historical records have been preserved.", "success");
            }
            catch (Exception ex)
            {
                ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex, "Error deactivating patient. Please review the form and try again; contact your administrator if the problem continues."), "error");
            }
        }

        protected void clearBtn_Click(object sender, EventArgs e)
        {
            ClearForm();
            ClearAlert();
        }

        protected void PatientListGV_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                selectedPatientId = Convert.ToInt32(PatientListGV.SelectedDataKey.Value);

                string query = "SELECT * FROM patientTbl WHERE patientId = @ID";
                DataTable dt = ExecuteParameterizedQuery(query, new SqlParameter("@ID", selectedPatientId));

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    PatNameTb.Value = row["patientName"].ToString();
                    PhoneTb.Value = row["patientPhone"].ToString();
                    GenderCb.SelectedValue = row["patientGender"].ToString();
                    AddressTb.Value = row["patientAdd"].ToString();
                    AllergyTb.Value = row["patientAllergies"].ToString();

                    if (row["patientDOB"] != DBNull.Value && DateTime.TryParse(row["patientDOB"].ToString(), out DateTime dob))
                        DOBTb.Value = dob.ToString("yyyy-MM-dd");
                    else
                        DOBTb.Value = "";
                }
            }
            catch (Exception ex)
            {
                ShowAlert(HospitalMSWebApp.Infrastructure.ApplicationErrors.Report(ex, "Error loading patient details. Please review the form and try again; contact your administrator if the problem continues."), "error");
            }
        }

        protected void PatientListGV_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            PatientListGV.PageIndex = e.NewPageIndex;
            LoadPatients();
        }

        #endregion

        #region Validation & Helpers

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(PatNameTb.Value))
            {
                ShowAlert("Please enter the patient's full name.", "error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneTb.Value))
            {
                ShowAlert("Please enter a phone number.", "error");
                return false;
            }

            if (string.IsNullOrEmpty(GenderCb.SelectedValue))
            {
                ShowAlert("Please select a gender.", "error");
                return false;
            }

            DateTime date;
            if (!string.IsNullOrEmpty(DOBTb.Value) && (!DateTime.TryParse(DOBTb.Value, out date) || date.Date > DateTime.UtcNow.Date))
            {
                ShowAlert("Enter a valid date of birth.", "error");
                return false;
            }

            return true;
        }

        private static DateTime? ParseDate(string value)
        {
            DateTime date;
            return DateTime.TryParse(value, out date) ? (DateTime?)date : null;
        }

        private DataTable ExecuteParameterizedQuery(string query, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            string connString = Models.Functions.ConnectionString;

            if (string.IsNullOrEmpty(connString)) throw new InvalidOperationException("Database configuration is unavailable.");

            using (SqlConnection connection = new SqlConnection(connString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddRange(parameters);
                Models.Functions.OpenConfiguredConnection(connection);
                new SqlDataAdapter(command).Fill(dt);
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
            PatNameTb.Value = "";
            PhoneTb.Value = "";
            GenderCb.SelectedIndex = 0;
            DOBTb.Value = "";
            AddressTb.Value = "";
            AllergyTb.Value = "";
            selectedPatientId = 0;
        }

        public override void VerifyRenderingInServerForm(Control control) { }

        #endregion
    }
}
