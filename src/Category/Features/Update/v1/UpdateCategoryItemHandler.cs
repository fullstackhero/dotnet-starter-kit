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

namespace Category.Features.Update.v1;
 
public sealed class UpdateCategoryItemHandler(
    ILogger<UpdateCategoryItemHandler> logger,
    [FromKeyedServices("categoryItem")] IRepository<CategoryItem> repository)
    : IRequestHandler<UpdateCategoryItemCommand, UpdateCategoryItemResponse>
{
    public async Task<UpdateCategoryItemResponse> Handle(UpdateCategoryItemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var categoryItem = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = categoryItem ?? throw new CategoryItemNotFoundException(request.Id);
        var updatedCategoryItem = categoryItem.Update(request.Name, request.Description);
        await repository.UpdateAsync(updatedCategoryItem, cancellationToken);
        logger.LogInformation("categoryItem item updated {CategoryItemItemId}", updatedCategoryItem.Id);
        return new UpdateCategoryItemResponse(updatedCategoryItem.Id);
    }
}
