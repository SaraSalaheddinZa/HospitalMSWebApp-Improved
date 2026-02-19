using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Data;

namespace HospitalMSWebApp.Models
{
    public class Functions
    {
        private SqlConnection conn;
        private SqlCommand cmd;
        private DataTable dtb;
        private string ConnStr;
        private SqlDataAdapter sda;

        public Functions()
        {
            ConnStr = @"Data Source=Sara\SQLEXP2017;Initial Catalog=HospitalMS-Sara7593-Wasan7639;Integrated Security=True";
            conn = new SqlConnection(ConnStr);
            cmd = new SqlCommand();
            cmd.Connection = conn;
        }

        public int SetDatas(string sql)
        {
            int cnt = 0;
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            cmd.CommandText = sql;
            cnt = cmd.ExecuteNonQuery();
            conn.Close();
            return cnt;
        }

        public DataTable GetDatas(string Query)
        {
            dtb = new DataTable();
            sda = new SqlDataAdapter(Query, ConnStr);
            sda.Fill(dtb);
            return dtb;
        }
    }
}