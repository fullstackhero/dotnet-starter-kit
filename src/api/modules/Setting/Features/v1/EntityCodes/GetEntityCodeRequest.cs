using MediatR;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public class GetEntityCodeRequest : IRequest<GetEntityCodeResponse>
{
    public Guid Id { get; set; }
    public GetEntityCodeRequest(Guid id) => Id = id;
}
