using MediatR;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Delete.v1;

public sealed record DeleteMeterTroubleTicketCommand(Guid Id) : IRequest;
