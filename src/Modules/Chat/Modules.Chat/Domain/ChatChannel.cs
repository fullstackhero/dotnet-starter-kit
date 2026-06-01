using FSH.Framework.Core.Domain;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Chat.Domain.Events;

namespace FSH.Modules.Chat.Domain;

/// <summary>
/// A chat channel: a 1:1 DM, a group DM (3+), or a named channel (Slack-style).
/// Soft-deletable so archived channels can be restored by admins.
/// </summary>
public sealed class ChatChannel : AggregateRoot<Guid>, ISoftDeletable
{
    public ChannelType Type { get; private set; }
    public string? Name { get; private set; }
    public string? Slug { get; private set; }
    public string? Description { get; private set; }
    public bool IsPrivate { get; private set; }

    /// <summary>
    /// For <see cref="ChannelType.DirectMessage"/>, a sorted "{userA}:{userB}" key that makes the
    /// "find-or-create DM" lookup O(1) via a partial UNIQUE index. Null for non-DM channels.
    /// Set once at creation and never updated, so no swap race.
    /// </summary>
    public string? DirectKey { get; private set; }

    public string CreatedByUserId { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? LastMessageAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private readonly List<ChannelMember> _members = [];
    public IReadOnlyList<ChannelMember> Members => _members;

    /// <summary>
    /// Soft-deletes (archives) the channel as an explicit state change rather than an EF
    /// <c>Remove()</c>. Removing the aggregate cascades <see cref="Microsoft.EntityFrameworkCore.EntityState.Deleted"/>
    /// onto the <see cref="Members"/> collection; the audit interceptor only un-deletes <em>owned</em>
    /// references, so FK children like <see cref="ChannelMember"/> would be hard-deleted and lost on
    /// restore. Flipping the flag here leaves members untouched so restore is lossless.
    /// </summary>
    public void Archive(string deletedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedByUserId);
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private ChatChannel() { }

    public static ChatChannel CreateChannel(string name, string? description, bool isPrivate, string creatorUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(creatorUserId);

        var c = new ChatChannel
        {
            Id = Guid.CreateVersion7(),
            Type = ChannelType.Channel,
            Name = name.Trim(),
            Slug = Slugify(name),
            Description = description?.Trim(),
            IsPrivate = isPrivate,
            CreatedByUserId = creatorUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        c._members.Add(ChannelMember.Create(c.Id, creatorUserId, ChannelMemberRole.Admin));
        c.AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ChannelCreatedDomainEvent(c.Id, c.Type, c.Name, creatorUserId, id, ts)));
        return c;
    }

    public static ChatChannel CreateDirect(string userAId, string userBId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userAId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userBId);
        if (string.Equals(userAId, userBId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Cannot start a DM with yourself.", nameof(userBId));
        }

        var (lo, hi) = string.CompareOrdinal(userAId, userBId) < 0 ? (userAId, userBId) : (userBId, userAId);
        var c = new ChatChannel
        {
            Id = Guid.CreateVersion7(),
            Type = ChannelType.DirectMessage,
            IsPrivate = true,
            DirectKey = $"{lo}:{hi}",
            CreatedByUserId = userAId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        c._members.Add(ChannelMember.Create(c.Id, userAId, ChannelMemberRole.Member));
        c._members.Add(ChannelMember.Create(c.Id, userBId, ChannelMemberRole.Member));
        c.AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ChannelCreatedDomainEvent(c.Id, c.Type, null, userAId, id, ts)));
        return c;
    }

    public static ChatChannel CreateGroupDm(IReadOnlyList<string> userIds, string creatorUserId)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(creatorUserId);
        if (userIds.Count < 3)
        {
            throw new ArgumentException("Group DM requires at least 3 distinct members.", nameof(userIds));
        }
        if (userIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("All user ids must be non-empty.", nameof(userIds));
        }

        var c = new ChatChannel
        {
            Id = Guid.CreateVersion7(),
            Type = ChannelType.GroupMessage,
            IsPrivate = true,
            CreatedByUserId = creatorUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        foreach (var uid in userIds.Distinct(StringComparer.Ordinal))
        {
            var role = string.Equals(uid, creatorUserId, StringComparison.Ordinal)
                ? ChannelMemberRole.Admin
                : ChannelMemberRole.Member;
            c._members.Add(ChannelMember.Create(c.Id, uid, role));
        }
        c.AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ChannelCreatedDomainEvent(c.Id, c.Type, null, creatorUserId, id, ts)));
        return c;
    }

    public void Rename(string name, string? description)
    {
        if (Type != ChannelType.Channel)
        {
            throw new InvalidOperationException("Only named Channels can be renamed.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Slug = Slugify(name);
        Description = description?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPrivate(bool isPrivate)
    {
        if (Type != ChannelType.Channel)
        {
            throw new InvalidOperationException("Only named Channels can change privacy.");
        }
        IsPrivate = isPrivate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public ChannelMember AddMember(string userId, string addedByUserId, ChannelMemberRole role = ChannelMemberRole.Member)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (Type == ChannelType.DirectMessage)
        {
            throw new InvalidOperationException("Direct messages have fixed membership.");
        }
        if (_members.Any(m => string.Equals(m.UserId, userId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"User {userId} is already a member.");
        }

        var member = ChannelMember.Create(Id, userId, role);
        _members.Add(member);
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ChannelMemberAddedDomainEvent(Id, userId, addedByUserId, id, ts)));
        return member;
    }

    public void RemoveMember(string userId, string removedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (Type == ChannelType.DirectMessage)
        {
            throw new InvalidOperationException("Direct messages have fixed membership.");
        }
        var member = _members.FirstOrDefault(m => string.Equals(m.UserId, userId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"User {userId} is not a member.");
        _members.Remove(member);
        UpdatedAtUtc = DateTime.UtcNow;
        AddDomainEvent(DomainEvent.Create((id, ts) =>
            new ChannelMemberRemovedDomainEvent(Id, userId, removedByUserId, id, ts)));
    }

    public void MarkRead(string userId, Guid messageId)
    {
        var member = _members.FirstOrDefault(m => string.Equals(m.UserId, userId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"User {userId} is not a member.");
        member.MarkRead(messageId);
    }

    public void TouchLastMessage(DateTime utcNow)
    {
        LastMessageAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    private static string Slugify(string value)
    {
        var trimmed = value.Trim();
#pragma warning disable CA1308 // slug is canonical lowercase, not security-sensitive
        var lower = trimmed.ToLowerInvariant();
#pragma warning restore CA1308
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var collapsed = new string(chars).Trim('-');
        while (collapsed.Contains("--", StringComparison.Ordinal))
        {
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        }
        return collapsed;
    }
}
