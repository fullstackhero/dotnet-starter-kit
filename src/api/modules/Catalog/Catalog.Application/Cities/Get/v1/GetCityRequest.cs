using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Cities.Get.v1;

public class GetCityRequest : IRequest<CityResponse>
{
    public Guid Id { get; set; }
    public GetCityRequest(Guid id) => Id = id;
}
