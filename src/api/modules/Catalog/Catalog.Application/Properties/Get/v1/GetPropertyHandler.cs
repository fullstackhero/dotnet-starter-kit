using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Properties.Get.v1;

public sealed class GetPropertyHandler(
    IReadRepository<Property> repository)
    : IRequestHandler<GetPropertyRequest, PropertyResponse>
{
    public async Task<PropertyResponse> Handle(GetPropertyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var property = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = property ?? throw new PropertyNotFoundException(request.Id);

        return new PropertyResponse(
            property.Id,
            property.Name,
            property.Description,
            property.Address,
            property.AskingPrice,
            property.Size,
            property.Rooms,
            property.Bathrooms,
            property.Neighborhood.Name,
            property.PropertyType.Name,
            property.ListedDate,
            property.SoldDate,
            property.SoldPrice,
            property.FeatureList);
    }
}