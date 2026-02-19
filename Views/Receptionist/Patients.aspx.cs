using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace HospitalMSWebApp.Views.Receptionist
{
    public partial class Patients : System.Web.UI.Page
    {
        Models.Functions conn;

        private int selectedPatientId
        {
            get { return ViewState["selectedPatientId"] != null ? (int)ViewState["selectedPatientId"] : 0; }
            set { ViewState["selectedPatientId"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["role"] == null || Session["role"].ToString() != "Receptionist")
            {
                Response.Redirect("../login.aspx");
                return;
            }

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
                string query = "SELECT patientId, patientName, patientPhone, patientGender, patientDOB, patientAdd, patientAllergies FROM patientTbl ORDER BY patientName";
                DataTable dt = conn.GetDatas(query);
                PatientListGV.DataSource = dt;
                PatientListGV.DataBind();
            }
            catch (Exception ex)
            {
                ShowAlert("Error loading patients: " + ex.Message, "error");
            }
        }

        private void LoadStatistics()
        {
            try
            {
                string totalQuery = "SELECT COUNT(*) FROM patientTbl";
                DataTable totalDt = conn.GetDatas(totalQuery);
                totalPatients.InnerText = totalDt.Rows[0][0].ToString();

                string todayQuery = "SELECT COUNT(*) FROM patientTbl WHERE CAST(registeredAt AS DATE) = CAST(GETDATE() AS DATE)";
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

                string userId = Session["uid"]?.ToString() ?? "0";
                string name = PatNameTb.Value.Trim();
                string phone = PhoneTb.Value.Trim();
                string gender = GenderCb.SelectedValue;
                string dob = DOBTb.Value;
                string address = AddressTb.Value.Trim();
                string allergies = AllergyTb.Value.Trim();

                string query = @"INSERT INTO patientTbl 
                    (patientName, patientPhone, patientGender, patientDOB, patientAdd, patientAllergies, addBy) 
                    VALUES (@Name, @Phone, @Gender, @DOB, @Address, @Allergies, @AddBy)";

                ExecuteParameterizedQuery(query,
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Phone", phone),
                    new SqlParameter("@Gender", gender),
                    new SqlParameter("@DOB", string.IsNullOrEmpty(dob) ? (object)DBNull.Value : DateTime.Parse(dob)),
                    new SqlParameter("@Address", address),
                    new SqlParameter("@Allergies", allergies),
                    new SqlParameter("@AddBy", int.TryParse(userId, out int uid) ? uid : 0)
                );

                LoadPatients();
                LoadStatistics();
                ClearForm();
                ShowAlert("Patient registered successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error registering patient: " + ex.Message, "error");
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
                string dob = DOBTb.Value;
                string address = AddressTb.Value.Trim();
                string allergies = AllergyTb.Value.Trim();

                string query = @"UPDATE patientTbl SET 
                    patientName      = @Name,
                    patientPhone     = @Phone,
                    patientGender    = @Gender,
                    patientDOB       = @DOB,
                    patientAdd       = @Address,
                    patientAllergies = @Allergies
                    WHERE patientId  = @ID";

                ExecuteParameterizedQuery(query,
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Phone", phone),
                    new SqlParameter("@Gender", gender),
                    new SqlParameter("@DOB", string.IsNullOrEmpty(dob) ? (object)DBNull.Value : DateTime.Parse(dob)),
                    new SqlParameter("@Address", address),
                    new SqlParameter("@Allergies", allergies),
                    new SqlParameter("@ID", selectedPatientId)
                );

                LoadPatients();
                LoadStatistics();
                ClearForm();
                ShowAlert("Patient updated successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error updating patient: " + ex.Message, "error");
            }
        }

        protected void deleteBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedPatientId == 0)
                {
                    ShowAlert("Please select a patient to delete.", "error");
                    return;
                }

                string query = "DELETE FROM patientTbl WHERE patientId = @ID";
                ExecuteParameterizedQuery(query, new SqlParameter("@ID", selectedPatientId));

                LoadPatients();
                LoadStatistics();
                ClearForm();
                ShowAlert("Patient deleted successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error deleting patient: " + ex.Message, "error");
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
                ShowAlert("Error loading patient details: " + ex.Message, "error");
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

            return true;
        }

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