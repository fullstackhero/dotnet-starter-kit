using FSH.Starter.WebApi.Water.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Meters.Update.v1;

public sealed record UpdateMeterCommand(
    Guid Id,
    string? Model = null,
    MeterStatus? Status = null,
    DateTimeOffset? LastReadingDate = null) : IRequest<UpdateMeterResponse>;
