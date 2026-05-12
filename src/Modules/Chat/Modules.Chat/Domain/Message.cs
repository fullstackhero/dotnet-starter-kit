using FSH.Framework.Core.Domain;
using FSH.Modules.Chat.Domain.Events;

namespace FSH.Modules.Chat.Domain;

/// <summary>
/// A single chat message. NOT <c>ISoftDeletable</c> — soft delete is just a <see cref="DeletedAtUtc"/>
/// stamp that the UI renders as a "[deleted]" tombstone so thread coherence is preserved.
/// </summary>
public sealed class Message : AggregateRoot<Guid>
{
    public Guid ChannelId { get; private set; }
    public string AuthorUserId { get; private set; } = default!;
    public string? Body { get; private set; }
    public Guid? ParentMessageId { get; private set; }
    public int ReplyCount { get; private set; }
    public DateTime? EditedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private readonly List<MessageAttachment> _attachments = [];
    public IReadOnlyList<MessageAttachment> Attachments => _attachments;

    private readonly List<MessageMention> _mentions = [];
    public IReadOnlyList<MessageMention> Mentions => _mentions;

    private Message() { }

    public static Message Create(
        Guid channelId,
        string authorUserId,
        string body,
        Guid? parentMessageId = null,
        IReadOnlyList<ParsedMention>? mentions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        if (channelId == Guid.Empty)
        {
            throw new ArgumentException("ChannelId is required.", nameof(channelId));
        }

        var m = new Message
        {
            Id = Guid.CreateVersion7(),
            ChannelId = channelId,
            AuthorUserId = authorUserId,
            Body = body.Trim(),
            ParentMessageId = parentMessageId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        if (mentions is not null)
        {
            foreach (var pm in mentions)
            {
                m._mentions.Add(MessageMention.Create(m.Id, pm.MentionedUserId, pm.StartIndex, pm.Length));
            }
        }
        m.AddDomainEvent(DomainEvent.Create((id, ts) =>
            new MessageCreatedDomainEvent(channelId, m.Id, authorUserId, parentMessageId, id, ts)));
        return m;
    }

    /// <summary>
    /// Input shape for <see cref="Create"/>: a resolved mention with the original position range
    /// in the body so the UI can render the highlight without re-parsing.
    /// </summary>
    public readonly record struct ParsedMention(string MentionedUserId, int StartIndex, int Length);

    public void Edit(string newBody, string editingUserId)
    {
        if (DeletedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Cannot edit a deleted message.");
        }
        if (!string.Equals(AuthorUserId, editingUserId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only the author can edit a message.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(newBody);

        Body = newBody.Trim();
        EditedAtUtc = DateTime.UtcNow;
        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new MessageEditedDomainEvent(ChannelId, Id, AuthorUserId, id, ts)));
    }

    public void SoftDelete(string deletingUserId, bool isModerator)
    {
        if (DeletedAtUtc.HasValue) return;
        if (!isModerator && !string.Equals(AuthorUserId, deletingUserId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only the author or a moderator can delete.");
        }
        DeletedAtUtc = DateTime.UtcNow;
        Body = null;
        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new MessageDeletedDomainEvent(ChannelId, Id, AuthorUserId, id, ts)));
    }

    public MessageAttachment AddAttachment(Guid? fileAssetId, string url, string contentType, string fileName, long sizeBytes)
    {
        var attachment = MessageAttachment.Create(Id, fileAssetId, url, contentType, fileName, sizeBytes);
        _attachments.Add(attachment);
        return attachment;
    }

    internal void IncrementReplyCount() => ReplyCount++;

    internal void DecrementReplyCount() => ReplyCount = Math.Max(0, ReplyCount - 1);
}
