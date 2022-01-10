using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Domain.Multitenancy;

namespace DN.WebApi.Application.Multitenancy;

public interface ITenantDatabaseService : ITransientService
{
    string DefaultConnectionString { get; }

    bool TryValidateConnectionString(string connectionString, string? tenantKey);

    void InitializeDatabase(Tenant tenant);
}