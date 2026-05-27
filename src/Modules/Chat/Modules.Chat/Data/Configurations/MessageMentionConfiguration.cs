using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Chat.Data.Configurations;

public sealed class MessageMentionConfiguration : IEntityTypeConfiguration<MessageMention>
{
    public void Configure(EntityTypeBuilder<MessageMention> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("MessageMentions");
        builder.HasKey(x => x.Id);
        // Child reached only via parent nav collection — EF would otherwise track Modified
        // instead of Added when the parent's factory assigns a non-default Id.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.MessageId).IsRequired();
        builder.Property(x => x.MentionedUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.StartIndex).IsRequired();
        builder.Property(x => x.Length).IsRequired();

        builder.HasIndex(x => x.MentionedUserId);
        builder.HasIndex(x => x.MessageId);

        builder.Ignore(x => x.DomainEvents);
    }
}
