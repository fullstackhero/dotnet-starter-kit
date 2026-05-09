using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Framework.Infrastructure.Tenant;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Constants;

namespace FSH.Starter.WebApi.Water.Infrastructure.Persistence;

public sealed class WaterDbContext : FshDbContext
{
    public WaterDbContext(IMultiTenantContextAccessor<FshTenantInfo> multiTenantContextAccessor, DbContextOptions<WaterDbContext> options, IPublisher publisher, IOptions<DatabaseOptions> settings)
        : base(multiTenantContextAccessor, options, publisher, settings)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Meter> Meters { get; set; } = null!;
    public DbSet<MeterReading> MeterReadings { get; set; } = null!;
    public DbSet<Tariff> Tariffs { get; set; } = null!;
    public DbSet<Bill> Bills { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<MeterTroubleTicket> MeterTroubleTickets { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WaterDbContext).Assembly);
        modelBuilder.HasDefaultSchema(SchemaNames.Water);
    }
}
