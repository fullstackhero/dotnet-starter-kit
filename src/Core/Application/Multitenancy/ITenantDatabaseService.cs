namespace FSH.WebApi.Application.Multitenancy;

public interface ITenantDatabaseService : ITransientService
{
    string DefaultConnectionString { get; }

    bool TryValidateConnectionString(string connectionString, string? tenantKey);

    void InitializeDatabase(Tenant tenant);
}