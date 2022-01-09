using DN.WebApi.Application.Multitenancy;
using System.Diagnostics.CodeAnalysis;

namespace DN.WebApi.Infrastructure.Multitenancy;

public interface ICurrentTenant
{
    TenantDto Tenant { get; }

    string Key { get; }
    string DbProvider { get; }

    public bool TryGetKey([NotNullWhen(true)] out string? tenantKey);
    public bool TryGetConnectionString([NotNullWhen(true)] out string? connectionString);
}