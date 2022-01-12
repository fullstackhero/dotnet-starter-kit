using System.Diagnostics.CodeAnalysis;
using FSH.WebApi.Application.Multitenancy;

namespace FSH.WebApi.Infrastructure.Multitenancy;

public interface ICurrentTenant
{
    TenantDto Tenant { get; }

    string Key { get; }
    string DbProvider { get; }

    public bool TryGetKey([NotNullWhen(true)] out string? tenantKey);
    public bool TryGetConnectionString([NotNullWhen(true)] out string? connectionString);
}