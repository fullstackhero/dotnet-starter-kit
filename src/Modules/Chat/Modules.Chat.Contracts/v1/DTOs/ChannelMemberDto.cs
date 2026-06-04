namespace FSH.Modules.Chat.Contracts.v1.DTOs;

public sealed record ChannelMemberDto(
    Guid Id,
    string UserId,
    ChannelMemberRole Role,
    DateTime JoinedAtUtc,
    Guid? LastReadMessageId,
    bool IsMuted);
