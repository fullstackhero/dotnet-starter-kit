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

        // Available public files whose key predates the public/ and private/ roots (#1410) — exactly the rows
        // MigrateLegacyPublicFileKeysJob moves. Legacy private, pending and quarantined rows are left out on
        // purpose: the job never moves them, so they would keep the index from emptying. New keys always carry
        // a root, so once the job has run the index is empty and its per-start scan (whose predicate matches
        // this filter) is a probe of an empty index. Visibility.Public = 0, FileAssetStatus.Available = 1.
        builder.HasIndex(x => x.Id)
            .HasFilter("\"StorageKey\" NOT LIKE 'public/%' AND \"StorageKey\" NOT LIKE 'private/%' AND \"Visibility\" = 0 AND \"Status\" = 1")
            .HasDatabaseName("IX_FileAsset_LegacyKey");

        builder.Ignore(x => x.DomainEvents);
    }
}
