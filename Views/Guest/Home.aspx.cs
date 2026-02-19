using System;
using System.Data;
using System.Web.UI;

namespace HospitalMSWebApp.Views.Guest
{
    public partial class Home : System.Web.UI.Page
    {
        Models.Functions conn;

        protected void Page_Load(object sender, EventArgs e)
        {
            conn = new Models.Functions();

            if (!IsPostBack)
            {
                LoadStatistics();
            }
        }

        private void LoadStatistics()
        {
            try
            {
                ViewState["PatientCount"] = GetPatientCount();

                ViewState["DoctorCount"] = GetDoctorCount();

                ViewState["AppointmentCount"] = GetAppointmentCount();

                ViewState["LabTestCount"] = GetLabTestCount();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading statistics: {ex.Message}");

                ViewState["PatientCount"] = 0;
                ViewState["DoctorCount"] = 0;
                ViewState["AppointmentCount"] = 0;
                ViewState["LabTestCount"] = 0;
            }
        }

        private int GetPatientCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM patientTbl";
                DataTable dt = conn.GetDatas(query);

                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                {
                    return Convert.ToInt32(dt.Rows[0][0]);
                }
            }
            catch { }

            return 0;
        }

        private int GetDoctorCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM doctorTbl WHERE doctorStatus = 'Active'";
                DataTable dt = conn.GetDatas(query);

                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                {
                    return Convert.ToInt32(dt.Rows[0][0]);
                }
            }
            catch { }

            return 0;
        }

        private int GetAppointmentCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM appointmentTbl";
                DataTable dt = conn.GetDatas(query);

                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                {
                    return Convert.ToInt32(dt.Rows[0][0]);
                }
            }
            catch { }

            return 0;
        }

        private int GetLabTestCount()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM labTestTbl";
                DataTable dt = conn.GetDatas(query);

                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                {
                    return Convert.ToInt32(dt.Rows[0][0]);
                }
            }
            catch { }

            return 0;
        }

        protected int GetTodaysAppointments()
        {
            try
            {
                string query = $"SELECT COUNT(*) FROM appointmentTbl WHERE CAST(appointmentDate AS DATE) = CAST(GETDATE() AS DATE)";
                DataTable dt = conn.GetDatas(query);

                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                {
                    return Convert.ToInt32(dt.Rows[0][0]);
                }
            }
            catch { }

            return 0;
        }

        protected int GetPendingLabTests()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM labTestTbl WHERE testStatus = 'Pending'";
                DataTable dt = conn.GetDatas(query);

                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                {
                    return Convert.ToInt32(dt.Rows[0][0]);
                }
            }
            catch { }

            return 0;
        }

        protected int PatientCount
        {
            get { return ViewState["PatientCount"] != null ? (int)ViewState["PatientCount"] : 0; }
        }

        protected int DoctorCount
        {
            get { return ViewState["DoctorCount"] != null ? (int)ViewState["DoctorCount"] : 0; }
        }

        protected int AppointmentCount
        {
            get { return ViewState["AppointmentCount"] != null ? (int)ViewState["AppointmentCount"] : 0; }
        }

        protected int LabTestCount
        {
            get { return ViewState["LabTestCount"] != null ? (int)ViewState["LabTestCount"] : 0; }
        }
    }
}
