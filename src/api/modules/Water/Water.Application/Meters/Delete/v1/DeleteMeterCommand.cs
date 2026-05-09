using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Meters.Delete.v1;

public sealed record DeleteMeterCommand(Guid Id) : IRequest;
