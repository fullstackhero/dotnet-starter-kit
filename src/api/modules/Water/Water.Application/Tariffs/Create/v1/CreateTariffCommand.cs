using System.ComponentModel;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Create.v1;

public sealed record CreateTariffCommand(
    [property: DefaultValue("Standard Residential")] string Name,
    [property: DefaultValue("Standard residential water tariff")] string? Description = null,
    [property: DefaultValue("2025-01-01")] DateTimeOffset EffectiveDate = default,
    [property: DefaultValue(null)] DateTimeOffset? EndDate = null,
    [property: DefaultValue(1.50)] decimal RatePerUnit = 0,
    [property: DefaultValue(10.00)] decimal FixedCharge = 0) : IRequest<CreateTariffResponse>;
