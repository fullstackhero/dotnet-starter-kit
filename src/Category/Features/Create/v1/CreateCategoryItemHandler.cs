using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Category.Domain;
using FSH.Framework.Core.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Category.Features.Create.v1;
 
public sealed class CreateCategoryItemHandler(
    ILogger<CreateCategoryItemHandler> logger,
    [FromKeyedServices("categoryItem")] IRepository<CategoryItem> repository)
    : IRequestHandler<CreateCategoryItemCommand, CreateCategoryItemResponse>
{
    public async Task<CreateCategoryItemResponse> Handle(CreateCategoryItemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = CategoryItem.Create(request.Name, request.Description);
        await repository.AddAsync(item, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("CategoryItem item created {CategoryItemId}", item.Id);
        return new CreateCategoryItemResponse(item.Id);
    }
}
