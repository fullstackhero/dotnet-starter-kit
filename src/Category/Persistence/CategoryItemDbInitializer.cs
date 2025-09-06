using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Category.Domain;
using FSH.Framework.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Category.Persistence;
 
internal sealed class CategoryItemDbInitializer(
    ILogger<CategoryItemDbInitializer> logger,
   CategoryItemDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for CategoryItem module", context.TenantInfo!.Identifier);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        const string name = "Hello World!";
        const string description = "This is your first task";
        if (await context.CategoryItems.FirstOrDefaultAsync(t => t.Name == name, cancellationToken).ConfigureAwait(false) is null)
        {
            var categoryItem = CategoryItem.Create(name , description);
            await context.CategoryItems.AddAsync(categoryItem, cancellationToken);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] seeding default categoryItem data", context.TenantInfo!.Identifier);
        }
    }
}
