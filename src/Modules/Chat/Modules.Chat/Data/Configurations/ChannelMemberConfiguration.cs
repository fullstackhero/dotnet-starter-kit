using FSH.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Chat.Data.Configurations;

public sealed class ChannelMemberConfiguration : IEntityTypeConfiguration<ChannelMember>
{
    public void Configure(EntityTypeBuilder<ChannelMember> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ChannelMembers");
        builder.HasKey(x => x.Id);
        // Required: parent-nav-added children with non-default app-set Ids must not be
        // ValueGeneratedOnAdd or EF tracks them as Modified instead of Added.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ChannelId).IsRequired();
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Role).IsRequired().HasConversion<int>();
        builder.Property(x => x.JoinedAtUtc).IsRequired();
        builder.Property(x => x.LastReadMessageId);
        builder.Property(x => x.IsMuted).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.ChannelId }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
