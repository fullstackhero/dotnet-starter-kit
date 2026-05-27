using FSH.Framework.Core.Domain;

namespace FSH.Modules.Chat.Domain;

/// <summary>
/// A single resolved <c>@username</c> mention attached to a <see cref="Message"/>. Stores the
/// position range in the body so the UI can highlight the original substring without re-parsing.
/// </summary>
public sealed class MessageMention : BaseEntity<Guid>
{
    public Guid MessageId { get; private set; }
    public string MentionedUserId { get; private set; } = default!;
    public int StartIndex { get; private set; }
    public int Length { get; private set; }

    private MessageMention() { }

    internal static MessageMention Create(Guid messageId, string mentionedUserId, int startIndex, int length)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mentionedUserId);
        return new MessageMention
        {
            Id = Guid.CreateVersion7(),
            MessageId = messageId,
            MentionedUserId = mentionedUserId,
            StartIndex = startIndex,
            Length = length,
        };
    }
}
