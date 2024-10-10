using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Setting.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
public sealed class CreateEntityCodeHandler(
    ILogger<CreateEntityCodeHandler> logger,
    [FromKeyedServices("setting:EntityCode")] IRepository<EntityCode> repository)
    : IRequestHandler<CreateEntityCodeCommand, CreateEntityCodeResponse>
{
    public async Task<CreateEntityCodeResponse> Handle(CreateEntityCodeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = EntityCode.Create(
            request.Order,
            request.Code,
            request.Name,
            request.Description,
            request.IsActive,
            request.Separator,
            request.Value,
            request.Type);

        await repository.AddAsync(item, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("EntityCode item created {ItemId}", item.Id);
        return new CreateEntityCodeResponse(item.Id);
    }
}
