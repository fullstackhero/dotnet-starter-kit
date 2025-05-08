using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Get.v1;

public sealed class GetPropertyTypeHandler(
    IReadRepository<PropertyType> repository,
    ICacheService cache)
    : IRequestHandler<GetPropertyTypeRequest, PropertyTypeResponse>
{
    public async Task<PropertyTypeResponse> Handle(GetPropertyTypeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"propertytype:{request.Id}",
            async () =>
            {
                var propertyTypeItem = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (propertyTypeItem == null) throw new PropertyTypeNotFoundException(request.Id);
                return new PropertyTypeResponse(propertyTypeItem.Id, propertyTypeItem.Name, propertyTypeItem.Description);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
