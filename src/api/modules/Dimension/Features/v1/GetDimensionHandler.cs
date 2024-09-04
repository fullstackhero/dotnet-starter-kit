using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Dimension.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Dimension.Features.v1;
public sealed class GetDimensionHandler(
    [FromKeyedServices("setting:dimension")] IReadRepository<Dimension.Domain.Dimension> repository,
    ICacheService cache)
    : IRequestHandler<GetDimensionRequest, GetDimensionResponse>
{
    public async Task<GetDimensionResponse> Handle(GetDimensionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"dimension:{request.Id}",
            async () =>
            {
                var item = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (item == null) throw new DimensionNotFoundException(request.Id);
                return new GetDimensionResponse(
                    item.Id,
                    item.Order,
                    item.Code!,
                    item.Name!,
                    item.Description,
                    item.IsActive,
                    item.FullName,
                    item.NativeName,
                    item.FullNativeName,
                    item.Value,
                    item.Type,
                    item.FatherId!);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
