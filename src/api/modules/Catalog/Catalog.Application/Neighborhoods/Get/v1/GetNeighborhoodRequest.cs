using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Neighborhoods.Get.v1;

public class GetNeighborhoodRequest : IRequest<NeighborhoodResponse>
{
    public Guid Id { get; set; }
    public GetNeighborhoodRequest(Guid id) => Id = id;
}
