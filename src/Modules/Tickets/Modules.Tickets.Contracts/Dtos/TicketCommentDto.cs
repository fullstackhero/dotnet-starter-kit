namespace FSH.Modules.Tickets.Contracts.Dtos;

public sealed record TicketCommentDto(
    Guid Id,
    Guid TicketId,
    Guid AuthorUserId,
    string Body,
    DateTime CreatedAtUtc);
