using Microsoft.Extensions.DependencyInjection;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Caching;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Get.v1;
public sealed class GetPropertyTypeHandler(
    [FromKeyedServices("catalog:propertytypes")] IReadRepository<PropertyType> repository,
    ICacheService cache)
    : IRequestHandler<GetPropertyTypeRequest, PropertyTypeResponse>
{
    public async Task<PropertyTypeResponse> Handle(GetPropertyTypeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"PropertyType:{request.Id}",
            async () =>
            {
                var PropertyTypeItem = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (PropertyTypeItem == null) throw new PropertyTypeNotFoundException(request.Id);
                return new PropertyTypeResponse(PropertyTypeItem.Id, PropertyTypeItem.Name, PropertyTypeItem.Description);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
