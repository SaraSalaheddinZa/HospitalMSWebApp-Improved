using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace HospitalMSWebApp.Views.Doctors
{
    public partial class Prescriptions : System.Web.UI.Page
    {
        Models.Functions conn;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["role"] == null || (Session["role"].ToString() != "Doctors" && Session["role"].ToString() != "Doctor"))
            {
                Response.Redirect("../login.aspx");
                return;
            }

            conn = new Models.Functions();

            if (!IsPostBack)
            {
                LoadPatients();
                LoadTests();
                LoadPrescriptions();
                LoadStatistics();
                ClearAlert();
            }
        }

        #region Data Loading

        private void LoadPrescriptions()
        {
            try
            {
                string query = @"
                    SELECT 
                        p.PrescId AS prescriptionId,
                        pat.patientName,
                        d.doctorName,
                        p.medicines,
                        t.testName,
                        p.cost,
                        NULL AS createdAt
                    FROM prescriptionTbl p
                    LEFT JOIN patientTbl pat ON pat.patientId = p.patientId
                    LEFT JOIN doctorTbl d ON d.doctorID = p.doctorId
                    LEFT JOIN labTestTbl t ON t.testId = p.testId
                    ORDER BY p.PrescId DESC";

                DataTable dt = conn.GetDatas(query);
                prescriptionGV.DataSource = dt;
                prescriptionGV.DataBind();
            }
            catch (Exception ex)
            {
                ShowAlert("Error loading prescriptions: " + ex.Message, "error");
            }
        }

        private void LoadPatients()
        {
            try
            {
                string query = "SELECT patientId, patientName FROM patientTbl ORDER BY patientName";
                DataTable dt = conn.GetDatas(query);
                patientCb.DataTextField = "patientName";
                patientCb.DataValueField = "patientId";
                patientCb.DataSource = dt;
                patientCb.DataBind();
                patientCb.Items.Insert(0, new ListItem("— Select Patient —", ""));
            }
            catch (Exception ex)
            {
                ShowAlert("Error loading patients: " + ex.Message, "error");
            }
        }

        private void LoadTests()
        {
            try
            {
                string query = "SELECT testId, testName FROM labTestTbl ORDER BY testName";
                DataTable dt = conn.GetDatas(query);
                testCb.DataTextField = "testName";
                testCb.DataValueField = "testId";
                testCb.DataSource = dt;
                testCb.DataBind();
                testCb.Items.Insert(0, new ListItem("— Select Test —", ""));
            }
            catch (Exception ex)
            {
                ShowAlert("Error loading lab tests: " + ex.Message, "error");
            }
        }

        private void LoadStatistics()
        {
            try
            {
                string totalQuery = "SELECT COUNT(*) FROM prescriptionTbl";
                DataTable totalDt = conn.GetDatas(totalQuery);
                totalPrescriptions.InnerText = totalDt.Rows[0][0].ToString();

                string patientsQuery = "SELECT COUNT(DISTINCT patientId) FROM prescriptionTbl";
                DataTable patientsDt = conn.GetDatas(patientsQuery);
                totalPatients.InnerText = patientsDt.Rows[0][0].ToString();

                string testsQuery = "SELECT COUNT(DISTINCT testId) FROM prescriptionTbl";
                DataTable testsDt = conn.GetDatas(testsQuery);
                totalTests.InnerText = testsDt.Rows[0][0].ToString();

                string valueQuery = "SELECT ISNULL(SUM(cost), 0) FROM prescriptionTbl";
                DataTable valueDt = conn.GetDatas(valueQuery);
                decimal total = Convert.ToDecimal(valueDt.Rows[0][0]);
                totalValue.InnerText = "$" + total.ToString("N0");
            }
            catch { }
        }

        #endregion

        #region CRUD Operations

        protected void addBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(patientCb.SelectedValue))
                {
                    ShowAlert("Please select a patient.", "error");
                    return;
                }

                if (string.IsNullOrWhiteSpace(medicineTb.Value))
                {
                    ShowAlert("Please enter at least one medicine.", "error");
                    return;
                }

                if (string.IsNullOrEmpty(testCb.SelectedValue))
                {
                    ShowAlert("Please select a lab test.", "error");
                    return;
                }

                if (string.IsNullOrWhiteSpace(costTb.Text) || !decimal.TryParse(costTb.Text, out decimal cost))
                {
                    ShowAlert("Please enter a valid cost.", "error");
                    return;
                }

                int doctorId = 0;
                if (Session["uid"] != null)
                {
                    doctorId = Convert.ToInt32(Session["uid"]);
                }
                else
                {
                    ShowAlert("Session expired. Please login again.", "error");
                    return;
                }

                int patientId = Convert.ToInt32(patientCb.SelectedValue);
                int testId = Convert.ToInt32(testCb.SelectedValue);
                string medicines = medicineTb.Value.Trim();

                string query = @"INSERT INTO prescriptionTbl 
                                 (doctorId, patientId, medicines, testId, cost) 
                                 VALUES (@DoctorId, @PatientId, @Medicines, @TestId, @Cost)";

                ExecuteParameterizedQuery(query,
                    new SqlParameter("@DoctorId", doctorId),
                    new SqlParameter("@PatientId", patientId),
                    new SqlParameter("@Medicines", medicines),
                    new SqlParameter("@TestId", testId),
                    new SqlParameter("@Cost", cost)
                );

                LoadPrescriptions();
                LoadStatistics();
                ClearForm();
                ShowAlert("Prescription saved successfully!", "success");
            }
            catch (Exception ex)
            {
                ShowAlert("Error saving prescription: " + ex.Message, "error");
            }
        }

        protected void clearBtn_Click(object sender, EventArgs e)
        {
            ClearForm();
            ClearAlert();
        }

        protected void prescriptionGV_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            prescriptionGV.PageIndex = e.NewPageIndex;
            LoadPrescriptions();
        }

        protected void prescriptionGV_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                e.Row.Attributes["onclick"] = Page.ClientScript.GetPostBackClientHyperlink(prescriptionGV, "Select$" + e.Row.RowIndex);
                e.Row.Attributes["style"] = "cursor:pointer";
            }
        }

        #endregion

        #region Helper Methods

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
            patientCb.SelectedIndex = 0;
            medicineTb.Value = "";
            testCb.SelectedIndex = 0;
            costTb.Text = "";
        }

        public override void VerifyRenderingInServerForm(Control control) { }

        #endregion
    }
}