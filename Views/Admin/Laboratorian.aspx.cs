using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace HospitalMSWebApp.Views.Admin
{
    public partial class Laboratorian : System.Web.UI.Page
    {
        Models.Functions conn;
        private int selectedLabId = 0;

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
                ShowAlert("Error loading staff: " + ex.Message, "error");
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

        protected void addBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInputs())
                    return;

                string labName = LabNameTb.Value.Trim();
                string labEmail = EmailTb.Value.Trim().ToLower();
                string labPassword = HashPassword(PasswordTb.Value);
                string labPhone = PhoneTb.Value.Trim();
                string labAddress = LabAddressTb.Value.Trim();
                string labGender = GenderCb.SelectedValue;
                string labStatus = StatusCb.SelectedValue;

                if (EmailExists(labEmail))
                {
                    ShowAlert("A staff member with this email already exists.", "error");
                    return;
                }

                string query = @"INSERT INTO laboratorianTbl (labName, labEmail, labPassword, labPhone, labAddress, labGender, labStatus) 
                                 VALUES (@Name, @Email, @Password, @Phone, @Address, @Gender, @Status)";

                ExecuteParameterizedQuery(query,
                    new SqlParameter("@Name", labName),
                    new SqlParameter("@Email", labEmail),
                    new SqlParameter("@Password", labPassword),
                    new SqlParameter("@Phone", labPhone),
                    new SqlParameter("@Address", labAddress),
                    new SqlParameter("@Gender", labGender),
                    new SqlParameter("@Status", labStatus)
                );

                LoadLaboratorians();
                LoadStatistics();
                ClearForm();
                ShowAlert("Laboratory staff added successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error adding staff: " + ex.Message, "error");
            }
        }

        protected void editBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedLabId == 0)
                {
                    ShowAlert("Please select a staff member to update.", "error");
                    return;
                }

                if (!ValidateInputs(true))
                    return;

                string labName = LabNameTb.Value.Trim();
                string labEmail = EmailTb.Value.Trim().ToLower();
                string labPhone = PhoneTb.Value.Trim();
                string labAddress = LabAddressTb.Value.Trim();
                string labGender = GenderCb.SelectedValue;
                string labStatus = StatusCb.SelectedValue;

                if (EmailExists(labEmail, selectedLabId))
                {
                    ShowAlert("Another staff member with this email already exists.", "error");
                    return;
                }

                string query = @"UPDATE laboratorianTbl SET 
                    labName = @Name, 
                    labEmail = @Email, 
                    labPhone = @Phone, 
                    labAddress = @Address, 
                    labGender = @Gender, 
                    labStatus = @Status";

                if (!string.IsNullOrEmpty(PasswordTb.Value))
                {
                    query += ", labPassword = @Password";
                }

                query += " WHERE labID = @ID";

                var parameters = new System.Collections.Generic.List<SqlParameter>
                {
                    new SqlParameter("@Name", labName),
                    new SqlParameter("@Email", labEmail),
                    new SqlParameter("@Phone", labPhone),
                    new SqlParameter("@Address", labAddress),
                    new SqlParameter("@Gender", labGender),
                    new SqlParameter("@Status", labStatus),
                    new SqlParameter("@ID", selectedLabId)
                };

                if (!string.IsNullOrEmpty(PasswordTb.Value))
                {
                    parameters.Add(new SqlParameter("@Password", HashPassword(PasswordTb.Value)));
                }

                ExecuteParameterizedQuery(query, parameters.ToArray());

                LoadLaboratorians();
                LoadStatistics();
                ClearForm();
                ShowAlert("Staff member updated successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error updating staff: " + ex.Message, "error");
            }
        }

        protected void deleteBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedLabId == 0)
                {
                    ShowAlert("Please select a staff member to delete.", "error");
                    return;
                }

                if (HasAssociatedRecords(selectedLabId))
                {
                    ShowAlert("Cannot delete staff member with associated lab tests. Consider setting status to Inactive instead.", "error");
                    return;
                }

                string query = "DELETE FROM laboratorianTbl WHERE labID = @ID";
                ExecuteParameterizedQuery(query, new SqlParameter("@ID", selectedLabId));

                LoadLaboratorians();
                LoadStatistics();
                ClearForm();
                selectedLabId = 0;
                ShowAlert("Staff member deleted successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error deleting staff: " + ex.Message, "error");
            }
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
                ShowAlert("Error loading staff details: " + ex.Message, "error");
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
            string icon = type == "success" ? "check-circle" : "exclamation-circle";
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