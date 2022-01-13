using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using FSH.WebApi.Application.Common.Events;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Domain.Catalog.Brands;
using FSH.WebApi.Domain.Catalog.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;

namespace FSH.WebApi.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext
{
    public ApplicationDbContext(ITenantInfo currentTenant, DbContextOptions options, ICurrentUser currentUser, ISerializerService serializer, IOptions<DatabaseSettings> dbSettings, IEventService eventService)
        : base(currentTenant, options, currentUser, serializer, dbSettings, eventService)
    {
    }

    public IDbConnection Connection => Database.GetDbConnection();

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Brand> Brands => Set<Brand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mark entities which are multi tenant here.
        // As an alternative we could introduce a marker interface (e.g. IMultiTenantEntity)
        // and do the model configuration automatically based on that marker.
        modelBuilder.Entity<Product>().IsMultiTenant();
        modelBuilder.Entity<Brand>().IsMultiTenant();
    }
}