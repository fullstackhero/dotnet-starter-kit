using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.MeterReadings.Get.v1;

public sealed class GetMeterReadingHandler(
    [FromKeyedServices("water:meter-readings")] IReadRepository<MeterReading> repository,
    ICacheService cache)
    : IRequestHandler<GetMeterReadingRequest, MeterReadingResponse>
{
    public async Task<MeterReadingResponse> Handle(GetMeterReadingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"meter-reading:{request.Id}",
            async () =>
            {
                var meterReading = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (meterReading == null) throw new MeterReadingNotFoundException(request.Id);
                return new MeterReadingResponse(meterReading.Id, meterReading.MeterId, meterReading.ReadingDate, meterReading.ReadingValue, meterReading.PreviousReadingValue, meterReading.Consumption, meterReading.Source, meterReading.Notes);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
