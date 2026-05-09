using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.Meters.Get.v1;

public sealed class GetMeterHandler(
    [FromKeyedServices("water:meters")] IReadRepository<Meter> repository,
    ICacheService cache)
    : IRequestHandler<GetMeterRequest, MeterResponse>
{
    public async Task<MeterResponse> Handle(GetMeterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"meter:{request.Id}",
            async () =>
            {
                var meter = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (meter == null) throw new MeterNotFoundException(request.Id);
                return new MeterResponse(meter.Id, meter.MeterNumber, meter.Model, meter.InstallationDate, meter.LastReadingDate, meter.Status, meter.CustomerId);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
