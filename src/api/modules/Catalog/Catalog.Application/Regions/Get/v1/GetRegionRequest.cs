using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Regions.Get.v1;

public class GetRegionRequest : IRequest<RegionResponse>
{
    public Guid Id { get; set; }
    public GetRegionRequest(Guid id) => Id = id;
}