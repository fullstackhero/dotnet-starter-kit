using FSH.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Identity.Data.Configurations;

public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .ToTable("PasswordHistory", IdentityModuleConstants.SchemaName)
            .HasKey(ph => ph.Id);

        builder
            .Property(ph => ph.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder
            .Property(ph => ph.PasswordHash)
            .IsRequired();

        builder
            .Property(ph => ph.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configure the foreign key relationship
        builder
            .HasOne(ph => ph.User)
            .WithMany((FshUser u) => u.PasswordHistories)
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add index for efficient lookups
        builder.HasIndex(ph => ph.UserId);
        builder.HasIndex(ph => new { ph.UserId, ph.CreatedAt });
    }
}