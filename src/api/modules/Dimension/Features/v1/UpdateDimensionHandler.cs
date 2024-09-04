using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Dimension.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Setting.Dimension.Features.v1;
public sealed class UpdateDimensionHandler(
    ILogger<UpdateDimensionHandler> logger,
    [FromKeyedServices("setting:dimension")] IRepository<Dimension.Domain.Dimension> repository)
    : IRequestHandler<UpdateDimensionCommand, UpdateDimensionResponse>
{
    public async Task<UpdateDimensionResponse> Handle(UpdateDimensionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = item ?? throw new DimensionNotFoundException(request.Id);

        var updatedItem = item.Update(
            request.Order,
            request.Code,
            request.Name,
            request.Description,
            request.IsActive,
            request.FullName,
            request.NativeName,
            request.FullNativeName,
            request.Value,
            request.Type,
            request.FatherId);

        await repository.UpdateAsync(updatedItem, cancellationToken);
        logger.LogInformation("Dimension Item updated {ItemId}", updatedItem.Id);
        return new UpdateDimensionResponse(updatedItem.Id);
    }
}
