using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public class GetDimensionsHandler(
    [FromKeyedServices("setting:dimension")] IReadRepository<Dimension> repository)
    : IRequestHandler<GetDimensionsRequest, List<DimensionDto>>
{
    public async Task<List<DimensionDto>> Handle(GetDimensionsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var spec = new GetDimensionsSpecs(request);
        
        return await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
    }
}
