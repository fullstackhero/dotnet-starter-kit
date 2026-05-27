using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Auditing.Persistence;

internal sealed class AuditDbInitializer(
    ILogger<AuditDbInitializer> logger,
    AuditDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("[{Tenant}] applied database migrations for audit module", context.TenantInfo?.Identifier);
            }
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}