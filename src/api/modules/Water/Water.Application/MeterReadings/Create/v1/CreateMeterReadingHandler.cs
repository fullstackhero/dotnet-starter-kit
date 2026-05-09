using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.MeterReadings.Create.v1;

public sealed class CreateMeterReadingHandler(
    ILogger<CreateMeterReadingHandler> logger,
    [FromKeyedServices("water:meter-readings")] IRepository<MeterReading> repository)
    : IRequestHandler<CreateMeterReadingCommand, CreateMeterReadingResponse>
{
    public async Task<CreateMeterReadingResponse> Handle(CreateMeterReadingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var meterReading = MeterReading.Create(request.MeterId, request.ReadingDate, request.ReadingValue, request.PreviousReadingValue, request.Source, request.Notes);
        await repository.AddAsync(meterReading, cancellationToken);
        logger.LogInformation("meter reading created {MeterReadingId}", meterReading.Id);
        return new CreateMeterReadingResponse(meterReading.Id);
    }
}
