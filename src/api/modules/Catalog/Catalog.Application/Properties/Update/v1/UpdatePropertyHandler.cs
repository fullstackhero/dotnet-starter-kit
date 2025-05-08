using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Properties.Update.v1;

public sealed class UpdatePropertyHandler(
    ILogger<UpdatePropertyHandler> logger,
    IRepository<Property> repository)
    : IRequestHandler<UpdatePropertyCommand, UpdatePropertyResponse>
{
    public async Task<UpdatePropertyResponse> Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var property = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = property ?? throw new PropertyNotFoundException(request.Id);
        var updatedProperty = property.Update(request.Name, request.Description, request.NeighborhoodId, request.Address, request.AskingPrice, request.Size, request.Rooms, request.Bathrooms, request.PropertyTypeId, request.ListedDate, request.SoldDate, request.SoldPrice, request.FeatureList);
        await repository.UpdateAsync(updatedProperty, cancellationToken);
        logger.LogInformation("Property with id : {PropertyId} updated.", property.Id);
        return new UpdatePropertyResponse(property.Id);
    }
}
