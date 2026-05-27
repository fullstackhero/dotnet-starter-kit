using FSH.Framework.Core.Domain;

namespace FSH.Modules.Chat.Domain;

/// <summary>
/// One reaction (emoji) by one user on one message. Uniqueness is enforced via a composite index
/// on <c>(MessageId, UserId, Emoji)</c> so a user can only react with the same emoji once.
/// </summary>
public sealed class MessageReaction : BaseEntity<Guid>
{
    public Guid MessageId { get; private set; }
    public string UserId { get; private set; } = default!;
    public string Emoji { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    private MessageReaction() { }

    internal static MessageReaction Create(Guid messageId, string userId, string emoji)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);
        return new MessageReaction
        {
            Id = Guid.CreateVersion7(),
            MessageId = messageId,
            UserId = userId,
            Emoji = emoji,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}
