using FSH.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Notifications.Data.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(1024);
        builder.Property(x => x.Link).HasMaxLength(512);
        builder.Property(x => x.Source).HasMaxLength(64).IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.ReadAtUtc);

        // Inbox query is always `WHERE UserId=? ORDER BY CreatedAtUtc DESC` with an optional
        // unread filter — single composite covers all paths.
        builder.HasIndex(x => new { x.UserId, x.ReadAtUtc, x.CreatedAtUtc })
            .HasDatabaseName("IX_Notifications_User_Read_Created");

        // Ignore framework domain-event collection — notifications don't raise events upward.
        builder.Ignore(x => x.DomainEvents);
    }
}
