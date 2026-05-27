using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(160);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.ParentCategoryId);
        builder.HasIndex(x => x.IsDeleted);
        builder.Ignore(x => x.DomainEvents);
    }
}
