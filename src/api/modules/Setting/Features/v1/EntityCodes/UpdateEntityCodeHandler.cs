using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Exceptions;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public sealed class UpdateEntityCodeHandler(
    ILogger<UpdateEntityCodeHandler> logger,
    [FromKeyedServices("setting:EntityCode")] IRepository<EntityCode> repository)
    : IRequestHandler<UpdateEntityCodeCommand, UpdateEntityCodeResponse>
{
    public async Task<UpdateEntityCodeResponse> Handle(UpdateEntityCodeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = item ?? throw new EntityCodeNotFoundException(request.Id);

        var updatedItem = item.Update(
            request.Order,
            request.Code,
            request.Name,
            request.Description,
            request.IsActive,
            request.Separator,
            request.Value,
            request.Type);

        await repository.UpdateAsync(updatedItem, cancellationToken);
        logger.LogInformation("EntityCode Item updated {ItemId}", updatedItem.Id);
        return new UpdateEntityCodeResponse(updatedItem.Id);
    }
}
