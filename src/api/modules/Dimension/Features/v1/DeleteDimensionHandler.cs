using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Dimension.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Setting.Dimension.Features.v1;
public sealed class DeleteDimensionHandler(
    ILogger<DeleteDimensionHandler> logger,
    [FromKeyedServices("setting:dimension")] IRepository<Dimension.Domain.Dimension> repository)
    : IRequestHandler<DeleteDimensionCommand>
{
    public async Task Handle(DeleteDimensionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = item ?? throw new DimensionNotFoundException(request.Id);
        await repository.DeleteAsync(item, cancellationToken);
        logger.LogInformation("Dimension item with id : {ItemId} deleted", item.Id);
    }
}
