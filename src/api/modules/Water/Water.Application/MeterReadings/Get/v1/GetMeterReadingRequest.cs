using MediatR;

namespace FSH.Starter.WebApi.Water.Application.MeterReadings.Get.v1;

public class GetMeterReadingRequest : IRequest<MeterReadingResponse>
{
    public Guid Id { get; set; }
    public GetMeterReadingRequest(Guid id) => Id = id;
}
