using FSH.Framework.Core.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Setting.Dimension.Features.v1;
public sealed class CreateDimensionHandler(
    ILogger<CreateDimensionHandler> logger,
    [FromKeyedServices("setting:dimension")] IRepository<Dimension.Domain.Dimension> repository)
    : IRequestHandler<CreateDimensionCommand, CreateDimensionResponse>
{
    public async Task<CreateDimensionResponse> Handle(CreateDimensionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = Dimension.Domain.Dimension.Create(
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

        await repository.AddAsync(item, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Dimension item created {ItemId}", item.Id);
        return new CreateDimensionResponse(item.Id);
    }
}
