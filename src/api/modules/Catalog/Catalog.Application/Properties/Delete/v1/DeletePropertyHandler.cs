using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Properties.Delete.v1;
public sealed class DeletePropertyHandler(
    ILogger<DeletePropertyHandler> logger,
    [FromKeyedServices("catalog:properties")] IRepository<Property> repository)
    : IRequestHandler<DeletePropertyCommand>
{
    public async Task Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var property = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = property ?? throw new PropertyNotFoundException(request.Id);
        await repository.DeleteAsync(property, cancellationToken);
        logger.LogInformation("Property with id : {PropertyId} deleted", property.Id);
    }
}
