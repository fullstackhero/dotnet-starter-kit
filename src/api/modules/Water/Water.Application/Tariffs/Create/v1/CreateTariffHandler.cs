using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Create.v1;

public sealed class CreateTariffHandler(
    ILogger<CreateTariffHandler> logger,
    [FromKeyedServices("water:tariffs")] IRepository<Tariff> repository)
    : IRequestHandler<CreateTariffCommand, CreateTariffResponse>
{
    public async Task<CreateTariffResponse> Handle(CreateTariffCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tariff = Tariff.Create(request.Name, request.Description, request.EffectiveDate, request.EndDate, request.RatePerUnit, request.FixedCharge);
        await repository.AddAsync(tariff, cancellationToken);
        logger.LogInformation("tariff created {TariffId}", tariff.Id);
        return new CreateTariffResponse(tariff.Id);
    }
}
