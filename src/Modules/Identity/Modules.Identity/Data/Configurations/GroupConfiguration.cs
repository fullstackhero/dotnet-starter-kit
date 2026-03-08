using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Identity.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("Groups", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(g => g.Id);

        builder
            .Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder
            .Property(g => g.Description)
            .HasMaxLength(1024);

        builder
            .Property(g => g.CreatedBy)
            .HasMaxLength(450);

        builder
            .Property(g => g.ModifiedBy)
            .HasMaxLength(450);

        builder
            .Property(g => g.DeletedBy)
            .HasMaxLength(450);

        builder
            .Property(g => g.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(g => g.Name);
        builder.HasIndex(g => g.IsDefault);
        builder.HasIndex(g => g.IsDeleted);
    }
}
