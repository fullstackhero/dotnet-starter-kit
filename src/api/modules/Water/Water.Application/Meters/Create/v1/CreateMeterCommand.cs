using System.ComponentModel;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Meters.Create.v1;

public sealed record CreateMeterCommand(
    [property: DefaultValue("MTR-0001")] string MeterNumber,
    [property: DefaultValue("Model X100")] string? Model = null,
    DateTimeOffset InstallationDate = default,
    Guid CustomerId = default) : IRequest<CreateMeterResponse>;
