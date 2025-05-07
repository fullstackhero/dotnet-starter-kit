using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Starter.WebApi.Catalog.Infrastructure.Persistence;
using FSH.Framework.Core.Persistence;
using MediatR;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

namespace FSH.Starter.WebApi.Catalog.Infrastructure;

public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        var databaseOptions = Options.Create(configuration.GetSection("DatabaseOptions").Get<DatabaseOptions>()!);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMultiTenant<FshTenantInfo>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var multiTenantContextAccessor = serviceProvider.GetRequiredService<IMultiTenantContextAccessor<FshTenantInfo>>();

        return new CatalogDbContext(
            multiTenantContextAccessor,
            optionsBuilder.Options,
            new NoOpPublisher(),
            databaseOptions);
    }

    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            return Task.CompletedTask;
        }
    }
}
