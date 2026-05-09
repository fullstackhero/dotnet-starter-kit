using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Get.v1;

public sealed class GetTariffHandler(
    [FromKeyedServices("water:tariffs")] IReadRepository<Tariff> repository,
    ICacheService cache)
    : IRequestHandler<GetTariffRequest, TariffResponse>
{
    public async Task<TariffResponse> Handle(GetTariffRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"tariff:{request.Id}",
            async () =>
            {
                var tariff = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (tariff == null) throw new TariffNotFoundException(request.Id);
                return new TariffResponse(tariff.Id, tariff.Name, tariff.Description, tariff.EffectiveDate, tariff.EndDate, tariff.RatePerUnit, tariff.FixedCharge, tariff.IsActive);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
