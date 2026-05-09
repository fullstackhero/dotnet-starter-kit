using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Tariffs.Get.v1;

public class GetTariffRequest : IRequest<TariffResponse>
{
    public Guid Id { get; set; }
    public GetTariffRequest(Guid id) => Id = id;
}
