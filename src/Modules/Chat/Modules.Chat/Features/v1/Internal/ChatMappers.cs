using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Domain;

namespace FSH.Modules.Chat.Features.v1.Internal;

internal static class ChatMappers
{
    public static ChannelMemberDto ToDto(this ChannelMember m) =>
        new(m.Id, m.UserId, (int)m.Role, m.JoinedAtUtc, m.LastReadMessageId, m.IsMuted);

    public static ChannelDto ToDto(this ChatChannel c, int unreadCount = 0) =>
        new(
            c.Id,
            (int)c.Type,
            c.Name,
            c.Slug,
            c.Description,
            c.IsPrivate,
            c.CreatedByUserId,
            c.CreatedAtUtc,
            c.UpdatedAtUtc,
            c.LastMessageAtUtc,
            unreadCount,
            c.Members.Select(m => m.ToDto()).ToList());

    public static MessageAttachmentDto ToDto(this MessageAttachment a) =>
        new(a.Id, a.FileAssetId, a.Url, a.ContentType, a.OriginalFileName, a.SizeBytes);

    public static MessageReactionDto ToDto(this MessageReaction r) =>
        new(r.Id, r.MessageId, r.UserId, r.Emoji, r.CreatedAtUtc);

    public static MessageDto ToDto(this Message m) =>
        new(
            m.Id,
            m.ChannelId,
            m.AuthorUserId,
            m.Body,
            m.ParentMessageId,
            m.ReplyCount,
            m.EditedAtUtc,
            m.DeletedAtUtc,
            m.CreatedAtUtc,
            m.Attachments.Select(a => a.ToDto()).ToList(),
            m.Reactions.Select(r => r.ToDto()).ToList(),
            m.IsPinned,
            m.PinnedByUserId,
            m.PinnedAtUtc);
}
