namespace FSH.Modules.Chat.Contracts.v1.DTOs;

public sealed record ChannelMemberDto(
    Guid Id,
    string UserId,
    int Role,                    // 0 = Member, 1 = Admin
    DateTime JoinedAtUtc,
    Guid? LastReadMessageId,
    bool IsMuted);
