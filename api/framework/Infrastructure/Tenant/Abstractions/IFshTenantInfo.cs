using Finbuckle.MultiTenant.Abstractions;

namespace FSH.Framework.Core.Tenant.Abstractions;
public interface IFshTenantInfo : ITenantInfo
{
    string? ConnectionString { get; set; }
}
