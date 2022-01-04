using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data.SqlClient;

namespace DN.WebApi.Infrastructure.Multitenancy
{
    public class HideSecureConnectionString
    {
        private const string HIDED_STRING = "*******";



        public static string? GetSecureConnectionString(string? dbProvider, string? connectionString)
        {
            if (connectionString == null)
            {
                return connectionString;
            }

            string? result = connectionString;

            switch (dbProvider?.ToLower())
            {
                case "postgresql":
                    var npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

                    if (!string.IsNullOrEmpty(npgsqlConnectionStringBuilder.Password) || !npgsqlConnectionStringBuilder.IntegratedSecurity)
                    {
                        npgsqlConnectionStringBuilder.Password = HIDED_STRING;
                    }

                    if (!string.IsNullOrEmpty(npgsqlConnectionStringBuilder.Username) || !npgsqlConnectionStringBuilder.IntegratedSecurity)
                    {
                        npgsqlConnectionStringBuilder.Username = HIDED_STRING;
                    }

                    result = npgsqlConnectionStringBuilder.ToString();
                    break;
                case "mssql":
                    var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

                    if (!string.IsNullOrEmpty(sqlConnectionStringBuilder.Password) || !sqlConnectionStringBuilder.IntegratedSecurity)
                    {
                        sqlConnectionStringBuilder.Password = HIDED_STRING;
                    }

                    if (!string.IsNullOrEmpty(sqlConnectionStringBuilder.UserID) || !sqlConnectionStringBuilder.IntegratedSecurity)
                    {
                        sqlConnectionStringBuilder.UserID = HIDED_STRING;
                    }

                    result = sqlConnectionStringBuilder.ToString();
                    break;
                case "mysql":
                    var mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder(connectionString);

                    if (!string.IsNullOrEmpty(mySqlConnectionStringBuilder.Password))
                    {
                        mySqlConnectionStringBuilder.Password = HIDED_STRING;
                    }

                    if (!string.IsNullOrEmpty(mySqlConnectionStringBuilder.UserID))
                    {
                        mySqlConnectionStringBuilder.UserID = HIDED_STRING;
                    }

                    result = mySqlConnectionStringBuilder.ToString();
                    break;
                case "oracle":
                    var oracleConnectionStringBuilder = new OracleConnectionStringBuilder(connectionString);

                    if (!string.IsNullOrEmpty(oracleConnectionStringBuilder.Password))
                    {
                        oracleConnectionStringBuilder.Password = HIDED_STRING;
                    }

                    if (!string.IsNullOrEmpty(oracleConnectionStringBuilder.UserID))
                    {
                        oracleConnectionStringBuilder.UserID = HIDED_STRING;
                    }

                    result = oracleConnectionStringBuilder.ToString();
                    break;
            }

            return result;
        }
    }
}