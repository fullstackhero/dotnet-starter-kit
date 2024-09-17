// using Finbuckle.MultiTenant.Abstractions;
// using FSH.Framework.Core.Persistence;
// using FSH.Framework.Infrastructure.Persistence;
// using FSH.Framework.Infrastructure.Tenant;
// using FSH.Starter.WebApi.Setting.Domain;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Options;
//
// namespace FSH.Starter.WebApi.Setting.Persistence;
// public sealed class DimensionDbContext : FshDbContext
// {
//     public DimensionDbContext(IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor, DbContextOptions<DimensionDbContext> options, IPublisher publisher, IOptions<DatabaseOptions> settings)
//         : base(multiTenantContextAccessor, options, publisher, settings)
//     {
//     }
//
//     public DbSet<Dimension> Dimensions { get; set; } = null!;
//
//     protected override void OnModelCreating(ModelBuilder modelBuilder)
//     {
//         ArgumentNullException.ThrowIfNull(modelBuilder);
//         modelBuilder.ApplyConfigurationsFromAssembly(typeof(DimensionDbContext).Assembly);
//         modelBuilder.HasDefaultSchema(SchemaNames.Setting);
//     }
// }
