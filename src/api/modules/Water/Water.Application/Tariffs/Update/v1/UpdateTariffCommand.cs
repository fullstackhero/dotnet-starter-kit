using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Update.v1;

public sealed record UpdateTariffCommand(
    Guid Id,
    string? Name,
    string? Description = null,
    DateTimeOffset? EffectiveDate = null,
    DateTimeOffset? EndDate = null,
    decimal? RatePerUnit = null,
    decimal? FixedCharge = null,
    bool? IsActive = null) : IRequest<UpdateTariffResponse>;
