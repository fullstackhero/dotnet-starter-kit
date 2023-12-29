using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Infrastructure.Multitenancy.Services;
public sealed class TenantDbBootstrapper(
    ILogger<TenantDbBootstrapper> logger,
    TenantDbContext context,
    IOptions<DbConfig> config) : IDbBootstrapper
{
    public async Task BootstrapAsync(FshTenantInfo? tenant, CancellationToken cancellationToken)
    {
        if (!config.Value.UseInMemoryDb && (await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("applied database migrations for tenant module");
        }

        if (await context.TenantInfo.FindAsync([MultitenancyConstants.Root.Id], cancellationToken).ConfigureAwait(false) is null)
        {
            var rootTenant = new FshTenantInfo(
                MultitenancyConstants.Root.Id,
                MultitenancyConstants.Root.Name,
                string.Empty,
                MultitenancyConstants.Root.EmailAddress);

            rootTenant.SetValidity(DateTime.UtcNow.AddYears(1));

            context.TenantInfo.Add(rootTenant);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
