using FSH.Framework.Core.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FSH.Framework.Infrastructure.Persistence.Services;
internal sealed class ConnectionStringValidator(IOptions<DbConfig> dbSettings, ILogger<ConnectionStringValidator> logger) : IConnectionStringValidator
{
    private readonly DbConfig _dbSettings = dbSettings.Value;
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
                default:
                    break;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Connection String Validation Exception : {Error}", ex.Message);
            return false;
        }
    }
}
