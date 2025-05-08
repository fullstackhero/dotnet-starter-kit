using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Properties.Create.v1;
public sealed class CreatePropertyHandler(
    ILogger<CreatePropertyHandler> logger,
    [FromKeyedServices("catalog:properties")] IRepository<Property> repository)
    : IRequestHandler<CreatePropertyCommand, CreatePropertyResponse>
{
    public async Task<CreatePropertyResponse> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var property = Property.Create(request.Name!, request.Description, request.NeighborhoodId, request.Address, request.AskingPrice, request.Size, request.Rooms, request.Bathrooms, request.PropertyTypeId, request.ListedDate, request.FeatureList);
        await repository.AddAsync(property, cancellationToken);
        logger.LogInformation("property created {PropertyId}", property.Id);
        return new CreatePropertyResponse(property.Id);
    }
}
