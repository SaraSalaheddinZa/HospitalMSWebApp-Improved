using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace HospitalMSWebApp.Views.Laboratorian
{
    public partial class LabTest : System.Web.UI.Page
    {
        Models.Functions conn;

        private int selectedTestId
        {
            get { return ViewState["selectedTestId"] != null ? (int)ViewState["selectedTestId"] : 0; }
            set { ViewState["selectedTestId"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["role"] == null || Session["role"].ToString() != "Laboratorian")
            {
                Response.Redirect("../login.aspx");
                return;
            }

            conn = new Models.Functions();

            if (!IsPostBack)
            {
                ShowTests();
                LoadStatistics();
                ClearAlert();
            }
        }

        #region Data Loading

        private void ShowTests()
        {
            try
            {
                string query = "SELECT testId, testName, testCost FROM labTestTbl ORDER BY testName";
                LabTestGV.DataSource = conn.GetDatas(query);
                LabTestGV.DataBind();
            }
            catch (Exception ex)
            {
                ShowAlert("Error loading tests: " + ex.Message, "error");
            }
        }

        private void LoadStatistics()
        {
            try
            {
                int count = Convert.ToInt32(conn.GetDatas("SELECT COUNT(*) FROM labTestTbl").Rows[0][0]);
                decimal avg = Convert.ToDecimal(conn.GetDatas("SELECT ISNULL(AVG(CAST(testCost AS FLOAT)),0) FROM labTestTbl").Rows[0][0]);
                decimal total = Convert.ToDecimal(conn.GetDatas("SELECT ISNULL(SUM(testCost),0) FROM labTestTbl").Rows[0][0]);

                string avgFmt = "$" + avg.ToString("N0");
                string totalFmt = "$" + total.ToString("N0");

                // Hero badges
                totalTests.InnerText = count.ToString();
                avgCostHero.InnerText = avgFmt;
                totalValHero.InnerText = totalFmt;

                // Stats strip
                statTotal.InnerText = count.ToString();
                avgCost.InnerText = avgFmt;
                totalValue.InnerText = totalFmt;
            }
            catch { }
        }

        #endregion

        #region CRUD Operations

        protected void addBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInputs()) return;

                string userId = Session["uid"]?.ToString();
                if (string.IsNullOrEmpty(userId))
                {
                    ShowAlert("Session expired. Please log in again.", "error");
                    return;
                }

                string name = LabNameTb.Value.Trim();
                int cost = Convert.ToInt32(testCostTb.Text);

                if (TestNameExists(name))
                {
                    ShowAlert("A test with this name already exists.", "error");
                    return;
                }

                ExecuteParameterizedQuery(
                    "INSERT INTO labTestTbl (testName, testCost, addBy) VALUES (@Name, @Cost, @By)",
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Cost", cost),
                    new SqlParameter("@By", int.TryParse(userId, out int uid) ? uid : 0)
                );

                ShowTests();
                LoadStatistics();
                ClearForm();
                ShowAlert("Test added successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error adding test: " + ex.Message, "error");
            }
        }

        protected void editBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedTestId == 0)
                {
                    ShowAlert("Please select a test to update.", "error");
                    return;
                }

                if (!ValidateInputs()) return;

                string name = LabNameTb.Value.Trim();
                int cost = Convert.ToInt32(testCostTb.Text);

                if (TestNameExists(name, selectedTestId))
                {
                    ShowAlert("Another test with this name already exists.", "error");
                    return;
                }

                ExecuteParameterizedQuery(
                    "UPDATE labTestTbl SET testName = @Name, testCost = @Cost WHERE testId = @ID",
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Cost", cost),
                    new SqlParameter("@ID", selectedTestId)
                );

                ShowTests();
                LoadStatistics();
                ClearForm();
                ShowAlert("Test updated successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error updating test: " + ex.Message, "error");
            }
        }

        protected void deleteBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedTestId == 0)
                {
                    ShowAlert("Please select a test to delete.", "error");
                    return;
                }

                if (IsTestUsed(selectedTestId))
                {
                    ShowAlert("Cannot delete — this test is used in existing prescriptions.", "error");
                    return;
                }

                ExecuteParameterizedQuery(
                    "DELETE FROM labTestTbl WHERE testId = @ID",
                    new SqlParameter("@ID", selectedTestId)
                );

                ShowTests();
                LoadStatistics();
                ClearForm();
                ShowAlert("Test deleted successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error deleting test: " + ex.Message, "error");
            }
        }

        protected void clearBtn_Click(object sender, EventArgs e)
        {
            ClearForm();
            ClearAlert();
        }

        protected void LabTestGV_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                selectedTestId = Convert.ToInt32(LabTestGV.SelectedDataKey.Value);

                DataTable dt = ExecuteParameterizedQuery(
                    "SELECT * FROM labTestTbl WHERE testId = @ID",
                    new SqlParameter("@ID", selectedTestId));

                if (dt.Rows.Count > 0)
                {
                    LabNameTb.Value = dt.Rows[0]["testName"].ToString();
                    testCostTb.Text = dt.Rows[0]["testCost"].ToString();
                }
            }
            catch (Exception ex)
            {
                ShowAlert("Error loading test details: " + ex.Message, "error");
            }
        }

        protected void LabTestGV_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            LabTestGV.PageIndex = e.NewPageIndex;
            ShowTests();
        }

        #endregion

        #region Helpers

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(LabNameTb.Value))
            {
                ShowAlert("Please enter the test name.", "error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(testCostTb.Text))
            {
                ShowAlert("Please enter the test cost.", "error");
                return false;
            }

            if (!int.TryParse(testCostTb.Text, out int cost) || cost < 0)
            {
                ShowAlert("Please enter a valid whole-number cost.", "error");
                return false;
            }

            return true;
        }

        private bool TestNameExists(string name, int excludeId = 0)
        {
            string q = excludeId > 0
                ? "SELECT COUNT(*) FROM labTestTbl WHERE testName = @Name AND testId != @ID"
                : "SELECT COUNT(*) FROM labTestTbl WHERE testName = @Name";

            var p = new System.Collections.Generic.List<SqlParameter>
                { new SqlParameter("@Name", name) };

            if (excludeId > 0)
                p.Add(new SqlParameter("@ID", excludeId));

            return Convert.ToInt32(ExecuteParameterizedQuery(q, p.ToArray()).Rows[0][0]) > 0;
        }

        private bool IsTestUsed(int testId)
        {
            try
            {
                return Convert.ToInt32(ExecuteParameterizedQuery(
                    "SELECT COUNT(*) FROM prescriptionTbl WHERE testId = @ID",
                    new SqlParameter("@ID", testId)).Rows[0][0]) > 0;
            }
            catch { return false; }
        }

        private DataTable ExecuteParameterizedQuery(string query, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            string cs = Models.Functions.ConnectionString;

            if (string.IsNullOrEmpty(cs))
            {
                string mq = query;
                foreach (var p in parameters)
                    mq = mq.Replace(p.ParameterName, $"'{p.Value}'");
                return conn.GetDatas(mq);
            }

            using (SqlConnection con = new SqlConnection(cs))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddRange(parameters);
                new SqlDataAdapter(cmd).Fill(dt);
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
            LabNameTb.Value = "";
            testCostTb.Text = "";
            selectedTestId = 0;
        }

        public override void VerifyRenderingInServerForm(Control control) { }

        #endregion
    }
}
