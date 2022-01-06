using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data.SqlClient;
using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Infrastructure.Common;

namespace DN.WebApi.Infrastructure.Multitenancy
{
    public class MakeSecureConnectionString : IMakeSecureConnectionString
    {
        private const string HiddenValueDefault = "*******";

        public string? MakeSecure(string? connectionString, string? dbProvider)
        {
            if (connectionString == null)
            {
                return connectionString;
            }

            return dbProvider?.ToLower() switch
            {
                DbProviderConstants.Npgsql => MakeSecureNpgsqlConnectionString(connectionString),
                DbProviderConstants.SqlServer => MakeSecureSqlConnectionString(connectionString),
                DbProviderConstants.MySql => MakeSecureMySqlConnectionString(connectionString),
                DbProviderConstants.Oracle => MakeSecureOracleConnectionString(connectionString),
                _ => connectionString
            };
        }

        private string MakeSecureOracleConnectionString(string connectionString)
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

        private string MakeSecureMySqlConnectionString(string connectionString)
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

        private string MakeSecureSqlConnectionString(string connectionString)
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

        private string MakeSecureNpgsqlConnectionString(string connectionString)
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