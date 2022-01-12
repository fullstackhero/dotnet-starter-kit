using FSH.WebApi.Application.Multitenancy;
using FSH.WebApi.Domain.Multitenancy;
using FSH.WebApi.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace FSH.WebApi.Infrastructure.Multitenancy;

internal class TenantDatabaseService : ITenantDatabaseService
{
    private readonly DatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;

    public TenantDatabaseService(IOptions<DatabaseSettings> dbSettings, IServiceProvider services) =>
        (_dbSettings, _services) = (dbSettings.Value, services);

    public string DefaultConnectionString =>
        string.IsNullOrWhiteSpace(_dbSettings.ConnectionString)
            ? throw new InvalidOperationException("No default connectionstring configured.")
            : _dbSettings.ConnectionString;

    protected string DefaultDbProvider =>
        string.IsNullOrWhiteSpace(_dbSettings.DBProvider)
            ? throw new InvalidOperationException("DB Provider is not configured.")
            : _dbSettings.DBProvider;

    public bool TryValidateConnectionString(string connectionString, string? tenantKey) =>
        TenantBootstrapper.TryValidateConnectionString(DefaultDbProvider, connectionString, tenantKey);

    public void InitializeDatabase(Tenant tenant) =>
        DatabaseInitializer.InitializeTenantDatabase(_services, DefaultDbProvider, DefaultConnectionString, tenant);
}