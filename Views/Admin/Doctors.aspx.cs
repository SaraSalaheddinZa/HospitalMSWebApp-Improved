using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace HospitalMSWebApp.Views.Admin
{
    public partial class Doctors : System.Web.UI.Page
    {
        Models.Functions conn;
        private int selectedDoctorId = 0;

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
                ShowAlert("Error loading doctors: " + ex.Message, "error");
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

        protected void addBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInputs())
                    return;

                string doctorName = DocNameTb.Value.Trim();
                string doctorPhone = DocPhoneTb.Value.Trim();
                string doctorExp = DocExpTb.Value.Trim();
                string doctorSpec = SpecialisationTb.Text.Trim();
                string doctorGen = GenderCb.SelectedValue;
                string doctorAdd = AddressTb.Text.Trim();
                string doctorDOB = DOBTb.Value;
                string doctorPassword = HashPassword(PasswordTb.Value);
                string doctorEmail = emailTb.Text.Trim().ToLower();
                string doctorStatus = StatusCb.SelectedValue;

                if (EmailExists(doctorEmail))
                {
                    ShowAlert("A doctor with this email already exists.", "error");
                    return;
                }

                string query = @"INSERT INTO doctorTbl (doctorName, doctorPhone, doctorExp, doctorSpec, doctorGender, doctorAdd, doctorDOB, doctorPassword, doctorEmail, doctorStatus) 
                                 VALUES (@Name, @Phone, @Exp, @Spec, @Gender, @Add, @DOB, @Password, @Email, @Status)";

                ExecuteParameterizedQuery(query,
                    new SqlParameter("@Name", doctorName),
                    new SqlParameter("@Phone", doctorPhone),
                    new SqlParameter("@Exp", Convert.ToInt32(doctorExp)),
                    new SqlParameter("@Spec", doctorSpec),
                    new SqlParameter("@Gender", doctorGen),
                    new SqlParameter("@Add", doctorAdd),
                    new SqlParameter("@DOB", doctorDOB),
                    new SqlParameter("@Password", doctorPassword),
                    new SqlParameter("@Email", doctorEmail),
                    new SqlParameter("@Status", doctorStatus)
                );

                LoadDoctors();
                LoadStatistics();
                ClearForm();
                ShowAlert("Doctor added successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error adding doctor: " + ex.Message, "error");
            }
        }

        protected void editBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedDoctorId == 0)
                {
                    ShowAlert("Please select a doctor to update.", "error");
                    return;
                }

                if (!ValidateInputs(true))
                    return;

                string doctorName = DocNameTb.Value.Trim();
                string doctorPhone = DocPhoneTb.Value.Trim();
                string doctorExp = DocExpTb.Value.Trim();
                string doctorSpec = SpecialisationTb.Text.Trim();
                string doctorGen = GenderCb.SelectedValue;
                string doctorAdd = AddressTb.Text.Trim();
                string doctorDOB = DOBTb.Value;
                string doctorEmail = emailTb.Text.Trim().ToLower();
                string doctorStatus = StatusCb.SelectedValue;

                if (EmailExists(doctorEmail, selectedDoctorId))
                {
                    ShowAlert("Another doctor with this email already exists.", "error");
                    return;
                }

                string query = @"UPDATE doctorTbl SET 
                    doctorName = @Name, 
                    doctorPhone = @Phone, 
                    doctorExp = @Exp, 
                    doctorSpec = @Spec, 
                    doctorGender = @Gender, 
                    doctorAdd = @Add, 
                    doctorDOB = @DOB, 
                    doctorEmail = @Email, 
                    doctorStatus = @Status";

                if (!string.IsNullOrEmpty(PasswordTb.Value))
                {
                    query += ", doctorPassword = @Password";
                }

                query += " WHERE doctorID = @ID";

                var parameters = new System.Collections.Generic.List<SqlParameter>
                {
                    new SqlParameter("@Name", doctorName),
                    new SqlParameter("@Phone", doctorPhone),
                    new SqlParameter("@Exp", Convert.ToInt32(doctorExp)),
                    new SqlParameter("@Spec", doctorSpec),
                    new SqlParameter("@Gender", doctorGen),
                    new SqlParameter("@Add", doctorAdd),
                    new SqlParameter("@DOB", doctorDOB),
                    new SqlParameter("@Email", doctorEmail),
                    new SqlParameter("@Status", doctorStatus),
                    new SqlParameter("@ID", selectedDoctorId)
                };

                if (!string.IsNullOrEmpty(PasswordTb.Value))
                {
                    parameters.Add(new SqlParameter("@Password", HashPassword(PasswordTb.Value)));
                }

                ExecuteParameterizedQuery(query, parameters.ToArray());

                LoadDoctors();
                LoadStatistics();
                ClearForm();
                ShowAlert("Doctor updated successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error updating doctor: " + ex.Message, "error");
            }
        }

        protected void deleteBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedDoctorId == 0)
                {
                    ShowAlert("Please select a doctor to delete.", "error");
                    return;
                }

                // Check if doctor has associated records
                if (HasAssociatedRecords(selectedDoctorId))
                {
                    ShowAlert("Cannot delete doctor with associated appointments or prescriptions. Consider setting status to Inactive instead.", "error");
                    return;
                }

                string query = "DELETE FROM doctorTbl WHERE doctorID = @ID";
                ExecuteParameterizedQuery(query, new SqlParameter("@ID", selectedDoctorId));

                LoadDoctors();
                LoadStatistics();
                ClearForm();
                selectedDoctorId = 0;
                ShowAlert("Doctor deleted successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error deleting doctor: " + ex.Message, "error");
            }
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
                ShowAlert("Error loading doctor details: " + ex.Message, "error");
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
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private DataTable ExecuteParameterizedQuery(string query, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            string connString = System.Configuration.ConfigurationManager.ConnectionStrings["HospitalDB"]?.ConnectionString;

            if (string.IsNullOrEmpty(connString))
            {
                string modifiedQuery = query;
                foreach (var param in parameters)
                {
                    modifiedQuery = modifiedQuery.Replace(param.ParameterName, $"'{param.Value}'");
                }
                return conn.GetDatas(modifiedQuery);
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
            string icon = type == "success" ? "check-circle" : type == "error" ? "exclamation-circle" : "info-circle";
            alertBox.InnerHtml = $"<i class='fas fa-{icon}'></i> {message}";
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