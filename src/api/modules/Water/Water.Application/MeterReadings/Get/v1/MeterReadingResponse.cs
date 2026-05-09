using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.MeterReadings.Get.v1;

public sealed record MeterReadingResponse(
    Guid? Id,
    Guid MeterId,
    DateTimeOffset ReadingDate,
    decimal ReadingValue,
    decimal? PreviousReadingValue,
    decimal Consumption,
    ReadingSource Source,
    string? Notes);
