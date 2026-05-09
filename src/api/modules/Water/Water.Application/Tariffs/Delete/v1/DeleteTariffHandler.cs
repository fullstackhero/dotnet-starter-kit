using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Delete.v1;

public sealed class DeleteTariffHandler(
    ILogger<DeleteTariffHandler> logger,
    [FromKeyedServices("water:tariffs")] IRepository<Tariff> repository)
    : IRequestHandler<DeleteTariffCommand>
{
    public async Task Handle(DeleteTariffCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tariff = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = tariff ?? throw new TariffNotFoundException(request.Id);
        await repository.DeleteAsync(tariff, cancellationToken);
        logger.LogInformation("tariff with id : {TariffId} deleted", tariff.Id);
    }
}
