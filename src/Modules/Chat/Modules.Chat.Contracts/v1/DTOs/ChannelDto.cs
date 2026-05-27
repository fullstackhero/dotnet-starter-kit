namespace FSH.Modules.Chat.Contracts.v1.DTOs;

public sealed record ChannelDto(
    Guid Id,
    int Type,                            // 0=DM, 1=GroupDM, 2=Channel
    string? Name,
    string? Slug,
    string? Description,
    bool IsPrivate,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? LastMessageAtUtc,
    int UnreadCount,
    IReadOnlyList<ChannelMemberDto> Members);
