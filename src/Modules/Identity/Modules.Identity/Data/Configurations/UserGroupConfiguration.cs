using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Identity.Data.Configurations;

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("UserGroups", IdentityModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(ug => new { ug.UserId, ug.GroupId });

        builder
            .Property(ug => ug.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder
            .Property(ug => ug.AddedBy)
            .HasMaxLength(450);

        builder
            .Property(ug => ug.AddedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder
            .HasOne(ug => ug.User)
            .WithMany()
            .HasForeignKey(ug => ug.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ug => ug.Group)
            .WithMany(g => g.UserGroups)
            .HasForeignKey(ug => ug.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ug => ug.UserId);
        builder.HasIndex(ug => ug.GroupId);
    }
}