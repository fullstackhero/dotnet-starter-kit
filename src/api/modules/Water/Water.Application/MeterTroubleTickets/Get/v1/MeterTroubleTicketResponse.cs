using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Get.v1;

public sealed record MeterTroubleTicketResponse(
    Guid? Id,
    Guid MeterId,
    DateTimeOffset ReportedDate,
    string IssueDescription,
    TicketStatus Status,
    DateTimeOffset? ResolvedDate,
    string? ResolutionNotes);
