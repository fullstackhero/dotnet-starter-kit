using FSH.Framework.Core.Domain;

namespace FSH.Modules.Chat.Domain;

public sealed class ChannelMember : BaseEntity<Guid>
{
    public Guid ChannelId { get; private set; }
    public string UserId { get; private set; } = default!;
    public ChannelMemberRole Role { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }
    public Guid? LastReadMessageId { get; private set; }
    public bool IsMuted { get; private set; }

    private ChannelMember() { }

    internal static ChannelMember Create(Guid channelId, string userId, ChannelMemberRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new ChannelMember
        {
            Id = Guid.CreateVersion7(),
            ChannelId = channelId,
            UserId = userId,
            Role = role,
            JoinedAtUtc = DateTime.UtcNow,
        };
    }

    internal void MarkRead(Guid messageId) => LastReadMessageId = messageId;

    internal void SetMuted(bool muted) => IsMuted = muted;

    internal void Promote(ChannelMemberRole role) => Role = role;
}
