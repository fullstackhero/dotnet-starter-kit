using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Update.v1;

public sealed class UpdatePropertyTypeHandler(
    ILogger<UpdatePropertyTypeHandler> logger,
    IRepository<PropertyType> repository)
    : IRequestHandler<UpdatePropertyTypeCommand, UpdatePropertyTypeResponse>
{
    public async Task<UpdatePropertyTypeResponse> Handle(UpdatePropertyTypeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var propertyType = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = propertyType ?? throw new PropertyTypeNotFoundException(request.Id);
        var updatedPropertyType = propertyType.Update(request.Name, request.Description);
        await repository.UpdateAsync(updatedPropertyType, cancellationToken);
        logger.LogInformation("PropertyType with id : {PropertyTypeId} updated.", propertyType.Id);
        return new UpdatePropertyTypeResponse(propertyType.Id);
    }
}
