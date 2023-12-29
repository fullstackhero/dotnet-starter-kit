using FSH.Framework.Infrastructure.Multitenancy;

namespace FSH.Framework.Core.Persistence;
public interface IDbBootstrapper
{
    Task BootstrapAsync(FshTenantInfo? tenant, CancellationToken cancellationToken);
}
