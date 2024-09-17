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
// public sealed class EntityCodeDbContext : FshDbContext
// {
//     public EntityCodeDbContext(IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor, DbContextOptions<EntityCodeDbContext> options, IPublisher publisher, IOptions<DatabaseOptions> settings)
//         : base(multiTenantContextAccessor, options, publisher, settings)
//     {
//     }
//
//     public DbSet<EntityCode> EntityCodes { get; set; } = null!;
//
//     protected override void OnModelCreating(ModelBuilder modelBuilder)
//     {
//         ArgumentNullException.ThrowIfNull(modelBuilder);
//         modelBuilder.ApplyConfigurationsFromAssembly(typeof(EntityCodeDbContext).Assembly);
//         modelBuilder.HasDefaultSchema(SchemaNames.Setting);
//     }
// }
