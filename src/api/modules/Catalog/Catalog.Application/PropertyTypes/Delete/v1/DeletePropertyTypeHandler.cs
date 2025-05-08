using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Delete.v1;
public sealed class DeletePropertyTypeHandler(
    ILogger<DeletePropertyTypeHandler> logger,
    [FromKeyedServices("catalog:propertytypes")] IRepository<PropertyType> repository)
    : IRequestHandler<DeletePropertyTypeCommand>
{
    public async Task Handle(DeletePropertyTypeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var propertyType = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = propertyType ?? throw new PropertyTypeNotFoundException(request.Id);
        await repository.DeleteAsync(propertyType, cancellationToken);
        logger.LogInformation("PropertyType with id : {PropertyTypeId} deleted", propertyType.Id);
    }
}
