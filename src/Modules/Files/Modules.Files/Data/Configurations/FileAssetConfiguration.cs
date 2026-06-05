using FSH.Modules.Files.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Files.Data.Configurations;

public sealed class FileAssetConfiguration : IEntityTypeConfiguration<FileAsset>
{
    public void Configure(EntityTypeBuilder<FileAsset> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("FileAssets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerType).IsRequired().HasMaxLength(64);
        builder.Property(x => x.OwnerId);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(128);
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(512);
        builder.Property(x => x.Visibility).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ScanStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.UploadDeadline);
        builder.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedOnUtc);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        // Schema-per-tenant (BaseDbContext) makes per-tenant narrowing implicit, so only an
        // Owner index is needed (not the row-level (TenantId, OwnerType, OwnerId)).
        builder.HasIndex(x => new { x.OwnerType, x.OwnerId })
            .HasDatabaseName("IX_FileAsset_Owner");
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_FileAsset_Status");
        builder.HasIndex(x => new { x.IsDeleted, x.DeletedOnUtc })
            .HasDatabaseName("IX_FileAsset_Deletion");
        // Unique on StorageKey across live rows only — a soft-deleted row's key should not block
        // a subsequent upload that happens to choose the same path (rare, but possible).
        builder.HasIndex(x => x.StorageKey)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("UX_FileAsset_StorageKey");

        builder.Ignore(x => x.DomainEvents);
    }
}
