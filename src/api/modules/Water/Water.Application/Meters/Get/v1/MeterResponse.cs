using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.Meters.Get.v1;

public sealed record MeterResponse(
    Guid? Id,
    string MeterNumber,
    string? Model,
    DateTimeOffset InstallationDate,
    DateTimeOffset? LastReadingDate,
    MeterStatus Status,
    Guid CustomerId);
