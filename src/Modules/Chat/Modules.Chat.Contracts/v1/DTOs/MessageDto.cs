namespace FSH.Modules.Chat.Contracts.v1.DTOs;

public sealed record MessageDto(
    Guid Id,
    Guid ChannelId,
    string AuthorUserId,
    string? Body,                  // null when DeletedAtUtc is set — UI renders [deleted] tombstone
    Guid? ParentMessageId,
    int ReplyCount,
    DateTime? EditedAtUtc,
    DateTime? DeletedAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<MessageAttachmentDto> Attachments,
    IReadOnlyList<MessageReactionDto> Reactions);
