using FSH.Framework.Infrastructure.Multitenancy;

namespace FSH.Framework.Core.Persistence;
public interface IDbBootstrapper
{
    Task StartAsync(FshTenantInfo? tenant, CancellationToken cancellationToken);
}
