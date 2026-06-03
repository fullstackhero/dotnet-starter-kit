using FSH.Modules.Tickets.Contracts.Dtos;

namespace FSH.Modules.Tickets.Domain;

internal static class TicketMappings
{
    public static TicketDto ToDto(this Ticket t, int commentCount) => new(
        t.Id,
        t.Number,
        t.Title,
        t.Description,
        t.Status,
        t.Priority,
        t.ReporterUserId,
        t.AssignedToUserId,
        t.ResolutionNote,
        t.CreatedAtUtc,
        t.UpdatedAtUtc,
        t.ResolvedAtUtc,
        t.ClosedAtUtc,
        commentCount,
        t.DeletedOnUtc,
        t.DeletedBy);

    public static TicketCommentDto ToDto(this TicketComment c) => new(
        c.Id, c.TicketId, c.AuthorUserId, c.Body, c.CreatedAtUtc);
}
