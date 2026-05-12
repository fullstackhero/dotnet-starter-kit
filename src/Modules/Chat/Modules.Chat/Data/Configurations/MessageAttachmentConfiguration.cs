using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Chat.Data.Configurations;

public sealed class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("MessageAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.MessageId).IsRequired();
        builder.Property(x => x.Url).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(255);
        builder.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(512);
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.HasIndex(x => x.MessageId);
    }
}
