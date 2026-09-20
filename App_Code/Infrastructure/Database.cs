using System;
using System.Data;
using System.Data.SqlClient;
using HospitalMSWebApp.Models;

namespace HospitalMSWebApp.Infrastructure
{
    public sealed class Database
    {
        public DataTable Query(string sql, params SqlParameter[] parameters)
        {
            return new Functions().GetDatas(sql, parameters);
        }

        public int Execute(string sql, params SqlParameter[] parameters)
        {
            return new Functions().SetDatas(sql, parameters);
        }

        public object Scalar(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(Functions.ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0) command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        public T InTransaction<T>(Func<SqlConnection, SqlTransaction, T> work)
        {
            using (var connection = new SqlConnection(Functions.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var result = work(connection, transaction);
                        transaction.Commit();
                        return result;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public static SqlParameter Parameter(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }
    }
}
