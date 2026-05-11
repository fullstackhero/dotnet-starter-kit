using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Catalog.Data;

public sealed class CatalogDbInitializer(
    CatalogDbContext dbContext,
    IHostEnvironment environment,
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

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        // Skip if anything already exists for this tenant — idempotent across restarts.
        bool alreadySeeded = await dbContext.Brands.AnyAsync(cancellationToken).ConfigureAwait(false)
            || await dbContext.Categories.AnyAsync(cancellationToken).ConfigureAwait(false)
            || await dbContext.Products.AnyAsync(cancellationToken).ConfigureAwait(false);
        if (alreadySeeded)
        {
            return;
        }

        var brands = CatalogSeedData.Brands;
        dbContext.Brands.AddRange(brands);

        var (roots, children) = CatalogSeedData.BuildCategories();
        dbContext.Categories.AddRange(roots);
        dbContext.Categories.AddRange(children);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var brandsByName = brands.ToDictionary(b => b.Name, b => b);
        var categoriesByName = roots.Concat(children).ToDictionary(c => c.Name, c => c);
        var products = CatalogSeedData.BuildProducts(brandsByName, categoriesByName);
        dbContext.Products.AddRange(products);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[Catalog] seeded demo data: {BrandCount} brands, {CategoryCount} categories, {ProductCount} products",
                brands.Count,
                roots.Count + children.Count,
                products.Count);
        }
    }
}
