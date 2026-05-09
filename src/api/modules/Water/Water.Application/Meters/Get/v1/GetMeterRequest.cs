using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Meters.Get.v1;

public class GetMeterRequest : IRequest<MeterResponse>
{
    public Guid Id { get; set; }
    public GetMeterRequest(Guid id) => Id = id;
}
