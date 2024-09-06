using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;
public class GetDimensionRequest : IRequest<GetDimensionResponse>
{
    public Guid Id { get; set; }
    public GetDimensionRequest(Guid id) => Id = id;
}
