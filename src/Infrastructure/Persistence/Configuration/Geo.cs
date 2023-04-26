using Finbuckle.MultiTenant.EntityFrameworkCore;
using FSH.WebApi.Domain.Geo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.WebApi.Infrastructure.Persistence.Configuration;
public class GeoAdminUnitConfig : IEntityTypeConfiguration<GeoAdminUnit>
{
    public void Configure(EntityTypeBuilder<GeoAdminUnit> builder)
    {
        builder
            .ToTable("GeoAdminUnits", SchemaNames.Geo)
            .IsMultiTenant();
        builder
            .Property(b => b.Code)
                .HasMaxLength(256);
        builder
            .Property(b => b.Name)
                .HasMaxLength(1024);
        builder
             .Property(b => b.Description)
                 .HasMaxLength(2048);
    }
}

public class CountryConfig : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder
            .ToTable("Countrys", SchemaNames.Geo)
            .IsMultiTenant();
        builder
            .Property(b => b.Code)
                .HasMaxLength(256);
        builder
            .Property(b => b.Name)
                .HasMaxLength(1024);
        builder
             .Property(b => b.Description)
                 .HasMaxLength(2048);
    }
}

public class StateConfig : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder
            .ToTable("States", SchemaNames.Geo)
            .IsMultiTenant();
        builder
            .Property(b => b.Code)
                .HasMaxLength(256);
        builder
            .Property(b => b.Name)
                .HasMaxLength(1024);
        builder
             .Property(b => b.Description)
                 .HasMaxLength(2048);
    }
}

public class ProvinceConfig : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder
            .ToTable("Provinces", SchemaNames.Geo)
            .IsMultiTenant();
        builder
            .Property(b => b.Code)
                .HasMaxLength(256);
        builder
            .Property(b => b.Name)
                .HasMaxLength(1024);
        builder
             .Property(b => b.Description)
                 .HasMaxLength(2048);
    }
}

public class DistrictConfig : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder
            .ToTable("Districts", SchemaNames.Geo)
            .IsMultiTenant();
        builder
            .Property(b => b.Code)
                .HasMaxLength(256);
        builder
            .Property(b => b.Name)
                .HasMaxLength(1024);
        builder
             .Property(b => b.Description)
                 .HasMaxLength(2048);
    }
}

public class WardConfig : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder
            .ToTable("Wards", SchemaNames.Geo)
            .IsMultiTenant();
        builder
            .Property(b => b.Code)
                .HasMaxLength(256);
        builder
            .Property(b => b.Name)
                .HasMaxLength(1024);
        builder
             .Property(b => b.Description)
                 .HasMaxLength(2048);
    }
}

public class RegionConfig : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder
            .ToTable("Regions", SchemaNames.Geo)
            .IsMultiTenant();
        builder
            .Property(b => b.Code)
                .HasMaxLength(256);
        builder
            .Property(b => b.Name)
                .HasMaxLength(1024);
        builder
             .Property(b => b.Description)
                 .HasMaxLength(2048);
    }
}