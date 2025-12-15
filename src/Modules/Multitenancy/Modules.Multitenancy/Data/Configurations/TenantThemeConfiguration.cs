using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Multitenancy.Data.Configurations;

public class TenantThemeConfiguration : IEntityTypeConfiguration<TenantTheme>
{
    public void Configure(EntityTypeBuilder<TenantTheme> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TenantThemes", MultitenancyConstants.Schema);

        builder.HasKey(t => t.Id);

        builder.HasIndex(t => t.TenantId)
            .IsUnique();

        builder.Property(t => t.TenantId)
            .HasMaxLength(64)
            .IsRequired();

        // Light Palette
        builder.Property(t => t.PrimaryColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.SecondaryColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.TertiaryColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.BackgroundColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.SurfaceColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.ErrorColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.WarningColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.SuccessColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.InfoColor).HasMaxLength(9).IsRequired();

        // Dark Palette
        builder.Property(t => t.DarkPrimaryColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.DarkSecondaryColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.DarkTertiaryColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.DarkBackgroundColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.DarkSurfaceColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.DarkErrorColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.DarkWarningColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.DarkSuccessColor).HasMaxLength(9).IsRequired();
        builder.Property(t => t.DarkInfoColor).HasMaxLength(9).IsRequired();

        // Brand Assets (URLs can be long with S3/CDN paths)
        builder.Property(t => t.LogoUrl).HasMaxLength(2048);
        builder.Property(t => t.LogoDarkUrl).HasMaxLength(2048);
        builder.Property(t => t.FaviconUrl).HasMaxLength(2048);

        // Typography
        builder.Property(t => t.FontFamily).HasMaxLength(200).IsRequired();
        builder.Property(t => t.HeadingFontFamily).HasMaxLength(200).IsRequired();
        builder.Property(t => t.FontSizeBase).IsRequired();
        builder.Property(t => t.LineHeightBase).IsRequired();

        // Layout
        builder.Property(t => t.BorderRadius).HasMaxLength(20).IsRequired();
        builder.Property(t => t.DefaultElevation).IsRequired();

        // Is Default
        builder.Property(t => t.IsDefault).IsRequired();

        // Audit
        builder.Property(t => t.CreatedOnUtc).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(256);
        builder.Property(t => t.LastModifiedBy).HasMaxLength(256);
    }
}
