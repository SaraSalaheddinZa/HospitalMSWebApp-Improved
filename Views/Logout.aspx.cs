using System;
using System.Web.UI;

namespace HospitalMSWebApp.Views
{
    public partial class Logout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                PerformLogout();
            }
        }

        private void PerformLogout()
        {
            try
            {
                if (Session["uid"] != null && Session["role"] != null)
                {
                    LogLogoutEvent(Session["uid"].ToString(), Session["role"].ToString());
                }

                // Clear all session data
                Session.Clear();
                Session.Abandon();

                // Clear authentication cookies
                if (Request.Cookies["Email"] != null)
                {
                    Response.Cookies["Email"].Expires = DateTime.Now.AddDays(-1);
                }
                if (Request.Cookies["Role"] != null)
                {
                    Response.Cookies["Role"].Expires = DateTime.Now.AddDays(-1);
                }

                Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
                Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
                Response.Cache.SetNoStore();
                Response.Expires = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout Error: {ex.Message}");
            }
        }

        private void LogLogoutEvent(string userId, string role)
        {
            try
            {
                string connString = System.Configuration.ConfigurationManager.ConnectionStrings["HospitalDB"]?.ConnectionString;

                if (!string.IsNullOrEmpty(connString))
                {
                    using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(connString))
                    {
                        string query = @"INSERT INTO LoginLogTbl (UserId, Role, Action, ActionTime, IPAddress) 
                                         VALUES (@UserId, @Role, 'Logout', @Time, @IP)";

                        using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@Role", role);
                            cmd.Parameters.AddWithValue("@Time", DateTime.Now);
                            cmd.Parameters.AddWithValue("@IP", GetClientIP());

                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private string GetClientIP()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip))
            {
                ip = Request.ServerVariables["REMOTE_ADDR"];
            }
            return ip ?? "Unknown";
        }
    }
}