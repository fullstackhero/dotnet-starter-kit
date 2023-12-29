using Finbuckle.MultiTenant;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Multitenancy;
using FSH.Framework.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.WebApi.Todo.Persistence;
internal sealed class TodoDbBootstrapper(
    ILogger<TodoDbBootstrapper> logger,
    TodoDbContext context,
    IOptions<DbConfig> config,
    IServiceProvider serviceProvider) : IDbBootstrapper
{
    public async Task BootstrapAsync(FshTenantInfo? tenant, CancellationToken cancellationToken)
    {
        if (!config.Value.UseInMemoryDb)
        {
            if (tenant != null)
            {
                using var scope = serviceProvider.CreateScope();
                serviceProvider.GetRequiredService<IMultiTenantContextAccessor>()
                    .MultiTenantContext = new MultiTenantContext<FshTenantInfo>()
                    {
                        TenantInfo = tenant
                    };
            }
            if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
            {
                await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("applied database migrations for todo module");
            }
        }
    }
}
