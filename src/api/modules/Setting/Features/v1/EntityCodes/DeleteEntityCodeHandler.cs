using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Exceptions;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public sealed class DeleteEntityCodeHandler(
    ILogger<DeleteEntityCodeHandler> logger,
    [FromKeyedServices("setting:EntityCode")] IRepository<EntityCode> repository)
    : IRequestHandler<DeleteEntityCodeCommand>
{
    public async Task Handle(DeleteEntityCodeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = item ?? throw new EntityCodeNotFoundException(request.Id);
        await repository.DeleteAsync(item, cancellationToken);
        logger.LogInformation("EntityCode item with id : {ItemId} deleted", item.Id);
    }
}
