using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Update.v1;

public sealed class UpdateTariffHandler(
    ILogger<UpdateTariffHandler> logger,
    [FromKeyedServices("water:tariffs")] IRepository<Tariff> repository)
    : IRequestHandler<UpdateTariffCommand, UpdateTariffResponse>
{
    public async Task<UpdateTariffResponse> Handle(UpdateTariffCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tariff = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = tariff ?? throw new TariffNotFoundException(request.Id);
        var updatedTariff = tariff.Update(request.Name, request.Description, request.EffectiveDate, request.EndDate, request.RatePerUnit, request.FixedCharge, request.IsActive);
        await repository.UpdateAsync(updatedTariff, cancellationToken);
        logger.LogInformation("tariff with id : {TariffId} updated.", tariff.Id);
        return new UpdateTariffResponse(tariff.Id);
    }
}
