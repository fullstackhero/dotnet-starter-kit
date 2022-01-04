using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data.SqlClient;
using DN.WebApi.Infrastructure.Common;

namespace DN.WebApi.Infrastructure.Multitenancy
{
    public class HideSecureConnectionString
    {
        private const string HiddenValueDefault = "*******";

        public static string? GetSecureConnectionString(string? dbProvider, string? connectionString)
        {
            if (connectionString == null)
            {
                return connectionString;
            }

            string? result = connectionString;

            switch (dbProvider?.ToLower())
            {
                case DbProviderConstants.Npgsql:
                    var npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

                    if (!string.IsNullOrEmpty(npgsqlConnectionStringBuilder.Password) || !npgsqlConnectionStringBuilder.IntegratedSecurity)
                    {
                        npgsqlConnectionStringBuilder.Password = HiddenValueDefault;
                    }

                    if (!string.IsNullOrEmpty(npgsqlConnectionStringBuilder.Username) || !npgsqlConnectionStringBuilder.IntegratedSecurity)
                    {
                        npgsqlConnectionStringBuilder.Username = HiddenValueDefault;
                    }

                    result = npgsqlConnectionStringBuilder.ToString();
                    break;
                case DbProviderConstants.SqlServer:
                    var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

                    if (!string.IsNullOrEmpty(sqlConnectionStringBuilder.Password) || !sqlConnectionStringBuilder.IntegratedSecurity)
                    {
                        sqlConnectionStringBuilder.Password = HiddenValueDefault;
                    }

                    if (!string.IsNullOrEmpty(sqlConnectionStringBuilder.UserID) || !sqlConnectionStringBuilder.IntegratedSecurity)
                    {
                        sqlConnectionStringBuilder.UserID = HiddenValueDefault;
                    }

                    result = sqlConnectionStringBuilder.ToString();
                    break;
                case DbProviderConstants.MySql:
                    var mySqlConnectionStringBuilder = new MySqlConnectionStringBuilder(connectionString);

                    if (!string.IsNullOrEmpty(mySqlConnectionStringBuilder.Password))
                    {
                        mySqlConnectionStringBuilder.Password = HiddenValueDefault;
                    }

                    if (!string.IsNullOrEmpty(mySqlConnectionStringBuilder.UserID))
                    {
                        mySqlConnectionStringBuilder.UserID = HiddenValueDefault;
                    }

                    result = mySqlConnectionStringBuilder.ToString();
                    break;
                case DbProviderConstants.Oracle:
                    var oracleConnectionStringBuilder = new OracleConnectionStringBuilder(connectionString);

                    if (!string.IsNullOrEmpty(oracleConnectionStringBuilder.Password))
                    {
                        oracleConnectionStringBuilder.Password = HiddenValueDefault;
                    }

                    if (!string.IsNullOrEmpty(oracleConnectionStringBuilder.UserID))
                    {
                        oracleConnectionStringBuilder.UserID = HiddenValueDefault;
                    }

                    result = oracleConnectionStringBuilder.ToString();
                    break;
            }

            return result;
        }
    }
}