using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public class GetEntityCodesHandler(
    [FromKeyedServices("setting:EntityCode")] IReadRepository<EntityCode> repository)
    : IRequestHandler<GetEntityCodesRequest, List<EntityCodeDto>>
{
    public async Task<List<EntityCodeDto>> Handle(GetEntityCodesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new GetEntityCodesSpecs( request);

        return await repository.ListAsync(spec, cancellationToken);
    }
}
