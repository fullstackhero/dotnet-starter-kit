using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Chat.Data.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ChannelId).IsRequired();
        builder.Property(x => x.AuthorUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Body).HasColumnType("text");
        builder.Property(x => x.ParentMessageId);
        builder.Property(x => x.ReplyCount).IsRequired();
        builder.Property(x => x.EditedAtUtc);
        builder.Property(x => x.DeletedAtUtc);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // Reverse-chronological paging by (ChannelId, Id) — Guid v7 is monotonically sortable
        // so Id desc is the time order. Index is descending on Id only.
        builder.HasIndex(x => new { x.ChannelId, x.Id }).IsDescending(false, true);
        builder.HasIndex(x => x.ParentMessageId).HasFilter("\"ParentMessageId\" IS NOT NULL");

        builder.HasOne<ChatChannel>()
            .WithMany()
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Attachments)
            .WithOne()
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Attachments).AutoInclude();

        builder.HasMany(x => x.Mentions)
            .WithOne()
            .HasForeignKey(m => m.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Mentions).AutoInclude();

        builder.Ignore(x => x.DomainEvents);
    }
}
