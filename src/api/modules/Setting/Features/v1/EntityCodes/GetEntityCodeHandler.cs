using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Exceptions;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public sealed class GetEntityCodeHandler(
    [FromKeyedServices("setting:EntityCode")] IReadRepository<EntityCode> repository,
    ICacheService cache)
    : IRequestHandler<GetEntityCodeRequest, GetEntityCodeResponse>
{
    public async Task<GetEntityCodeResponse> Handle(GetEntityCodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"EntityCode:{request.Id}",
            async () =>
            {
                var item = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (item == null) throw new EntityCodeNotFoundException(request.Id);
                return new GetEntityCodeResponse(
                    item.Id,
                    item.Order,
                    item.Code!,
                    item.Name!,
                    item.Description,
                    item.IsActive,
                    item.Separator,
                    item.Value,
                    item.Type);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
