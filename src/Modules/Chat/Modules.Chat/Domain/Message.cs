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
    public bool IsPinned { get; private set; }
    public string? PinnedByUserId { get; private set; }
    public DateTime? PinnedAtUtc { get; private set; }

    private readonly List<MessageAttachment> _attachments = [];
    public IReadOnlyList<MessageAttachment> Attachments => _attachments;

    private readonly List<MessageMention> _mentions = [];
    public IReadOnlyList<MessageMention> Mentions => _mentions;

    private readonly List<MessageReaction> _reactions = [];
    public IReadOnlyList<MessageReaction> Reactions => _reactions;

    private Message() { }

    public static Message Create(
        Guid channelId,
        string authorUserId,
        string? body,
        Guid? parentMessageId = null,
        IReadOnlyList<ParsedMention>? mentions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorUserId);
        if (channelId == Guid.Empty)
        {
            throw new ArgumentException("ChannelId is required.", nameof(channelId));
        }

        // Body is optional at the aggregate level — the SendMessage validator
        // enforces "body OR at least one attachment" since attachments attach
        // AFTER Create via AddAttachment.
        var trimmed = string.IsNullOrWhiteSpace(body) ? null : body.Trim();

        var m = new Message
        {
            Id = Guid.CreateVersion7(),
            ChannelId = channelId,
            AuthorUserId = authorUserId,
            Body = trimmed,
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

    /// <summary>
    /// Toggle-on a reaction. Returns the new <see cref="MessageReaction"/>, or <c>null</c> if the
    /// (user, emoji) pair already exists — the unique index would reject the duplicate row.
    /// </summary>
    public MessageReaction? AddReaction(string userId, string emoji)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);
        if (DeletedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Cannot react to a deleted message.");
        }
        var trimmed = emoji.Trim();
        if (_reactions.Any(r => string.Equals(r.UserId, userId, StringComparison.Ordinal)
                             && string.Equals(r.Emoji, trimmed, StringComparison.Ordinal)))
        {
            return null;
        }
        var reaction = MessageReaction.Create(Id, userId, trimmed);
        _reactions.Add(reaction);
        return reaction;
    }

    /// <summary>
    /// Toggle-off a reaction. Returns <c>true</c> if a row was removed; <c>false</c> if the user
    /// hadn't reacted with that emoji.
    /// </summary>
    public bool RemoveReaction(string userId, string emoji)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);
        var trimmed = emoji.Trim();
        var existing = _reactions.FirstOrDefault(r =>
            string.Equals(r.UserId, userId, StringComparison.Ordinal)
            && string.Equals(r.Emoji, trimmed, StringComparison.Ordinal));
        if (existing is null) return false;
        _reactions.Remove(existing);
        return true;
    }

    internal void IncrementReplyCount() => ReplyCount++;

    internal void DecrementReplyCount() => ReplyCount = Math.Max(0, ReplyCount - 1);

    /// <summary>
    /// Pin the message to its channel. Idempotent — re-pinning by a different user updates the
    /// PinnedByUserId / PinnedAtUtc stamp but doesn't produce a duplicate event. Pinning a
    /// soft-deleted message is rejected; pinning a reply is permitted (channels can pin a
    /// specific thread reply, e.g. an answer).
    /// </summary>
    public void Pin(string pinningUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pinningUserId);
        if (DeletedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Cannot pin a deleted message.");
        }
        if (IsPinned && string.Equals(PinnedByUserId, pinningUserId, StringComparison.Ordinal))
        {
            // Already pinned by this user — no-op, no event.
            return;
        }

        IsPinned = true;
        PinnedByUserId = pinningUserId;
        PinnedAtUtc = DateTime.UtcNow;
        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new MessagePinnedDomainEvent(ChannelId, Id, pinningUserId, id, ts)));
    }

    /// <summary>
    /// Unpin the message. Idempotent — unpinning an already-unpinned message is a no-op.
    /// </summary>
    public void Unpin(string unpinningUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unpinningUserId);
        if (!IsPinned) return;

        IsPinned = false;
        PinnedByUserId = null;
        PinnedAtUtc = null;
        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new MessageUnpinnedDomainEvent(ChannelId, Id, unpinningUserId, id, ts)));
    }
}
