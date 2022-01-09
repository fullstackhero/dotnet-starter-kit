using DN.WebApi.Application.Multitenancy;

namespace DN.WebApi.Infrastructure.Multitenancy;

public interface ICurrentTenant
{
    TenantDto Tenant { get; }

    string Key { get; }
    string DbProvider { get; }
    string ConnectionString { get; }

    public bool TryGetKey(out string? tenantKey);
}