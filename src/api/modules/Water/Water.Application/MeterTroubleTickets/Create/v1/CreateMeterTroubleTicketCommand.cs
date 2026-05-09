using System.ComponentModel;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Create.v1;

public sealed record CreateMeterTroubleTicketCommand(
    Guid MeterId,
    [property: DefaultValue("Meter reading error reported")] string IssueDescription,
    DateTimeOffset ReportedDate = default) : IRequest<CreateMeterTroubleTicketResponse>;
