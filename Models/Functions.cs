using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace HospitalMSWebApp.Models
{
    /// <summary>
    /// Compatibility facade for the original Web Forms pages. New code should use
    /// Database directly and always pass SqlParameter values.
    /// </summary>
    public sealed class Functions
    {
        private const string RequiredSessionOptions = @"SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;";

        public static string ConnectionString
        {
            get
            {
                var variableName = ConfigurationManager.AppSettings["HospitalDbEnvironmentVariable"]
                    ?? "HOSPITALMS_CONNECTIONSTRING";
                var value = Environment.GetEnvironmentVariable(variableName);
                if (!string.IsNullOrWhiteSpace(value)) return value;

                var configured = ConfigurationManager.ConnectionStrings["HospitalDB"];
                if (configured != null && !string.IsNullOrWhiteSpace(configured.ConnectionString))
                    return configured.ConnectionString;

                throw new ConfigurationErrorsException(
                    "No database connection is configured. Set HOSPITALMS_CONNECTIONSTRING in the application environment.");
            }
        }

        public int SetDatas(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0) command.Parameters.AddRange(parameters);
                OpenConfiguredConnection(connection);
                return command.ExecuteNonQuery();
            }
        }

        public DataTable GetDatas(string sql, params SqlParameter[] parameters)
        {
            var table = new DataTable();
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                if (parameters != null && parameters.Length > 0) command.Parameters.AddRange(parameters);
                OpenConfiguredConnection(connection);
                adapter.Fill(table);
            }
            return table;
        }

        /// <summary>Opens an ADO.NET connection with SQL Server settings required by Guardian Health's indexed tables.</summary>
        public static void OpenConfiguredConnection(SqlConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            connection.Open();
            using (var settings = new SqlCommand(RequiredSessionOptions, connection)) settings.ExecuteNonQuery();
        }
    }
}
