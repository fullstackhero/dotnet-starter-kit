// using FSH.Framework.Core.Persistence;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
//
// namespace FSH.Starter.WebApi.Setting.Persistence;
// internal sealed class DimensionDbInitializer(
//     ILogger<DimensionDbInitializer> logger,
//     DimensionDbContext context) : IDbInitializer
// {
//     public async Task MigrateAsync(CancellationToken cancellationToken)
//     {
//         if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
//         {
//             await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
//             logger.LogInformation("[{Tenant}] applied database migrations for dimension module", context.TenantInfo!.Identifier);
//         }
//     }
//
//     public async Task SeedAsync(CancellationToken cancellationToken)
//     {
//
//         // const string Title = "Hello World!";
//         //const string Note = "This is your first task";
//         //if (await context.Dimensions.FirstOrDefaultAsync(t => t.Title == Title, cancellationToken).ConfigureAwait(false) is null)
//         //{
//         //    var dimension = Dimension.Create(Title, Note);
//         //    await context.Dimensions.AddAsync(dimension, cancellationToken);
//         //    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
//         //    logger.LogInformation("[{Tenant}] seeding default dimension data", context.TenantInfo!.Identifier);
//         //}
//     }
// }
