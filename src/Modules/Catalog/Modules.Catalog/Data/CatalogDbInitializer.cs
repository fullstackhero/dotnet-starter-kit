using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Catalog.Data;

public sealed class CatalogDbInitializer(
    CatalogDbContext dbContext,
    ILogger<CatalogDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Catalog] applied migrations");
        }
    }

    /// <summary>
    /// Catalog has NO per-tenant auto-seed. A fresh tenant comes up with an empty
    /// catalog and is expected to be populated by the operator via the API / UI.
    /// Demo content for the <c>acme</c> and <c>globex</c> tenants lives in the
    /// DbMigrator's <c>seed-demo</c> command, which calls
    /// <see cref="CatalogSeedData"/> directly under a tenant-scoped DbContext.
    /// </summary>
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
