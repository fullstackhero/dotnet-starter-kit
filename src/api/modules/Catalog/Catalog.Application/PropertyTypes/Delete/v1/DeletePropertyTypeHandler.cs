using FSH.Framework.Core.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.PropertyTypes.Delete.v1;

public sealed class DeletePropertyTypeHandler(
    ILogger<DeletePropertyTypeHandler> logger,
    IRepository<PropertyType> repository)
    : IRequestHandler<DeletePropertyTypeCommand>
{
    public async Task Handle(DeletePropertyTypeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var propertyType = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (propertyType == null)
        {
            throw new PropertyTypeNotFoundException(request.Id);
        }

        await repository.DeleteAsync(propertyType, cancellationToken);
        logger.LogInformation("PropertyType deleted {PropertyTypeId}", propertyType.Id);
    }
}
