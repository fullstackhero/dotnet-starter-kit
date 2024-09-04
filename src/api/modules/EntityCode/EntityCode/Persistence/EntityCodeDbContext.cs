using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.Tenant;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using FSH.Starter.WebApi.Setting.EntityCode.Domain;

namespace FSH.Starter.WebApi.Setting.EntityCode.Persistence;
public sealed class EntityCodeDbContext : FshDbContext
{
    public EntityCodeDbContext(IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor, DbContextOptions<EntityCodeDbContext> options, IPublisher publisher, IOptions<DatabaseOptions> settings)
        : base(multiTenantContextAccessor, options, publisher, settings)
    {
    }

    public DbSet<Domain.EntityCode> EntityCodes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EntityCodeDbContext).Assembly);
        modelBuilder.HasDefaultSchema(SchemaNames.Setting);
    }
}
