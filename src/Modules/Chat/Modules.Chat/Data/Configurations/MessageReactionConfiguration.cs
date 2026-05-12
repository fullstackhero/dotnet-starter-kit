using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Chat.Data.Configurations;

public sealed class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("MessageReactions");
        builder.HasKey(x => x.Id);
        // Child reached only via Message.Reactions nav — see project_ef_value_generation_for_nav_children.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.MessageId).IsRequired();
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Emoji).IsRequired().HasMaxLength(64);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        // A user can react with the same emoji only once per message.
        builder.HasIndex(x => new { x.MessageId, x.UserId, x.Emoji }).IsUnique()
            .HasDatabaseName("UX_MessageReactions_Message_User_Emoji");

        builder.Ignore(x => x.DomainEvents);
    }
}
