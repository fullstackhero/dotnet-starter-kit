namespace FSH.Starter.WebApi.Water.Application.Tariffs.Get.v1;

public sealed record TariffResponse(
    Guid? Id,
    string Name,
    string? Description,
    DateTimeOffset EffectiveDate,
    DateTimeOffset? EndDate,
    decimal RatePerUnit,
    decimal FixedCharge,
    bool IsActive);
