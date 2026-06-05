using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Chat.Data.Configurations;

public sealed class ChatChannelConfiguration : IEntityTypeConfiguration<ChatChannel>
{
    public void Configure(EntityTypeBuilder<ChatChannel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Channels");
        builder.HasKey(x => x.Id);
        // App-generated Guid v7. The child Members nav collection requires ValueGeneratedNever on its
        // own Id; setting it on the aggregate too is harmless and consistent.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Type).IsRequired().HasConversion<int>();
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Slug).HasMaxLength(220);
        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasFilter("\"Slug\" IS NOT NULL AND \"IsDeleted\" = FALSE");

        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.IsPrivate).IsRequired();
        builder.Property(x => x.DirectKey).HasMaxLength(80);
        builder.HasIndex(x => x.DirectKey)
            .IsUnique()
            .HasFilter("\"Type\" = 0 AND \"IsDeleted\" = FALSE");

        builder.Property(x => x.CreatedByUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.LastMessageAtUtc);

        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.IsDeleted);

        builder.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Members).AutoInclude();

        builder.Ignore(x => x.DomainEvents);
    }
}
