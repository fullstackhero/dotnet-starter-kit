using FSH.Starter.WebApi.Water.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Update.v1;

public sealed record UpdateMeterTroubleTicketCommand(
    Guid Id,
    string? IssueDescription = null,
    TicketStatus? Status = null,
    string? ResolutionNotes = null,
    DateTimeOffset? ResolvedDate = null) : IRequest<UpdateMeterTroubleTicketResponse>;
