using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Infrastructure.Persistence;
public class DbMigrationService<T>(ILogger<DbMigrationService<T>> logger, IServiceScopeFactory scopeFactory) : IHostedService
    where T : DbContext
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateAsyncScope();

            var dbConfig = scope.ServiceProvider.GetRequiredService<IOptions<DbConfig>>().Value;
            if (dbConfig.UseInMemoryDb) return;

            var context = scope.ServiceProvider.GetRequiredService<T>();
            if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
            {
                await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("applied database migrations for {Module} module",
                    typeof(T).Name.ToUpperInvariant()
                    .Replace("DBCONTEXT", "", StringComparison.InvariantCultureIgnoreCase));
            }
        }
        catch (Exception ex)
        {

            logger.LogError(ex, "An error occurred while applying the database migrations.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
