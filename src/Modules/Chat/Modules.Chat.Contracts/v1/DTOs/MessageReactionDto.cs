namespace FSH.Modules.Chat.Contracts.v1.DTOs;

public sealed record MessageReactionDto(
    Guid Id,
    Guid MessageId,
    string UserId,
    string Emoji,
    DateTime CreatedAtUtc);
