using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Create.v1;
public sealed class CreatePropertyTypeHandler(
    ILogger<CreatePropertyTypeHandler> logger,
    [FromKeyedServices("catalog:propertytypes")] IRepository<PropertyType> repository)
    : IRequestHandler<CreatePropertyTypeCommand, CreatePropertyTypeResponse>
{
    public async Task<CreatePropertyTypeResponse> Handle(CreatePropertyTypeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var propertyType = PropertyType.Create(request.Name!, request.Description);
        await repository.AddAsync(propertyType, cancellationToken);
        logger.LogInformation("propertyType created {PropertyTypeId}", propertyType.Id);
        return new CreatePropertyTypeResponse(propertyType.Id);
    }
}
