using FSH.Framework.Shared.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FSH.Framework.Persistence;

/// <summary>
/// Validates database connection strings for supported providers (PostgreSQL, SQL Server).
/// </summary>
/// <param name="dbSettings">Database configuration options.</param>
/// <param name="logger">Logger instance for error tracking.</param>
public sealed class ConnectionStringValidator(IOptions<DatabaseOptions> dbSettings, ILogger<ConnectionStringValidator> logger) : IConnectionStringValidator
{
    private readonly DatabaseOptions _dbSettings = dbSettings.Value;
    private readonly ILogger<ConnectionStringValidator> _logger = logger;

    public bool TryValidate(string connectionString, string? dbProvider = null)
    {
        if (string.IsNullOrWhiteSpace(dbProvider))
        {
            dbProvider = _dbSettings.Provider;
        }

        try
        {
            switch (dbProvider?.ToUpperInvariant())
            {
                case DbProviders.PostgreSQL:
                    _ = new NpgsqlConnectionStringBuilder(connectionString);
                    break;
                case DbProviders.MSSQL:
                    _ = new SqlConnectionStringBuilder(connectionString);
                    break;
                default:
                    break;
            }

            return true;
        }
#pragma warning disable CA1031 // Validation should not throw to callers; we log and return false.
        catch (Exception ex)
        {
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            _logger.LogError("Connection String Validation Exception : {Error}", ex.Message);
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            return false;
        }
#pragma warning restore CA1031
    }
}
