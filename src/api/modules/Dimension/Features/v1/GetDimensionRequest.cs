using MediatR;

namespace FSH.Starter.WebApi.Setting.Dimension.Features.v1;
public class GetDimensionRequest : IRequest<GetDimensionResponse>
{
    public Guid Id { get; set; }
    public GetDimensionRequest(Guid id) => Id = id;
}
