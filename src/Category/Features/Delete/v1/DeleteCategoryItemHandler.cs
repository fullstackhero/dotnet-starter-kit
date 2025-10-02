using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Category.Domain;
using Category.Exceptions;
using FSH.Framework.Core.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Category.Features.Delete.v1;
 
public sealed class DeleteCategoryItemHandler(
    ILogger<DeleteCategoryItemHandler> logger,
    [FromKeyedServices("categoryItem")] IRepository<CategoryItem> repository)
    : IRequestHandler<DeleteCategoryItemCommand>
{
    public async Task Handle(DeleteCategoryItemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var categoryItem = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = categoryItem ?? throw new CategoryItemNotFoundException(request.Id);
        await repository.DeleteAsync(categoryItem, cancellationToken);
        logger.LogInformation("categoryItem with id : {CategoryItemId} deleted", categoryItem.Id);
    }
}
