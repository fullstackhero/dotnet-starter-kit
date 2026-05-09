using System.ComponentModel;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.MeterReadings.Create.v1;

public sealed record CreateMeterReadingCommand(
    Guid MeterId,
    DateTimeOffset ReadingDate,
    [property: DefaultValue(100.0)] decimal ReadingValue,
    decimal? PreviousReadingValue = null,
    [property: DefaultValue(ReadingSource.Manual)] ReadingSource Source = ReadingSource.Manual,
    string? Notes = null) : IRequest<CreateMeterReadingResponse>;
