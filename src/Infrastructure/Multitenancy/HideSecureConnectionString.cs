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

            return dbProvider?.ToLower() switch
            {
                DbProviderConstants.Npgsql => SecureNpgsqlConnectionString(connectionString),
                DbProviderConstants.SqlServer => SecureSqlConnectionString(connectionString),
                DbProviderConstants.MySql => SecureMySqlConnectionString(connectionString),
                DbProviderConstants.Oracle => SecureOracleConnectionString(connectionString),
                _ => connectionString
            };
        }

        private static string SecureOracleConnectionString(string connectionString)
        {
            var builder = new OracleConnectionStringBuilder(connectionString);

            if (!string.IsNullOrEmpty(builder.Password))
            {
                builder.Password = HiddenValueDefault;
            }

            if (!string.IsNullOrEmpty(builder.UserID))
            {
                builder.UserID = HiddenValueDefault;
            }

            return builder.ToString();
        }

        private static string SecureMySqlConnectionString(string connectionString)
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);

            if (!string.IsNullOrEmpty(builder.Password))
            {
                builder.Password = HiddenValueDefault;
            }

            if (!string.IsNullOrEmpty(builder.UserID))
            {
                builder.UserID = HiddenValueDefault;
            }

            return builder.ToString();
        }

        private static string SecureSqlConnectionString(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            if (!string.IsNullOrEmpty(builder.Password) || !builder.IntegratedSecurity)
            {
                builder.Password = HiddenValueDefault;
            }

            if (!string.IsNullOrEmpty(builder.UserID) || !builder.IntegratedSecurity)
            {
                builder.UserID = HiddenValueDefault;
            }

            return builder.ToString();
        }

        private static string SecureNpgsqlConnectionString(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            if (!string.IsNullOrEmpty(builder.Password) || !builder.IntegratedSecurity)
            {
                builder.Password = HiddenValueDefault;
            }

            if (!string.IsNullOrEmpty(builder.Username) || !builder.IntegratedSecurity)
            {
                builder.Username = HiddenValueDefault;
            }

            return builder.ToString();
        }
    }
}