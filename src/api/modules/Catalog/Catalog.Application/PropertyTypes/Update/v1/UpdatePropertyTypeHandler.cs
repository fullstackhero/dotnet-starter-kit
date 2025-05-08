using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Update.v1;
public sealed class UpdatePropertyTypeHandler(
    ILogger<UpdatePropertyTypeHandler> logger,
    [FromKeyedServices("catalog:propertytypes")] IRepository<PropertyType> repository)
    : IRequestHandler<UpdatePropertyTypeCommand, UpdatePropertyTypeResponse>
{
    public async Task<UpdatePropertyTypeResponse> Handle(UpdatePropertyTypeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var PropertyType = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = PropertyType ?? throw new PropertyTypeNotFoundException(request.Id);
        var updatedProprtyType = PropertyType.Update(request.Name, request.Description);
        await repository.UpdateAsync(updatedProprtyType, cancellationToken);
        logger.LogInformation("PropertyType with id : {PropertyTypeId} updated.", PropertyType.Id);
        return new UpdatePropertyTypeResponse(PropertyType.Id);
    }
}
