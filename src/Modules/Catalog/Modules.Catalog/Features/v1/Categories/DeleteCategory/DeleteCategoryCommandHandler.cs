using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Categories;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Categories.DeleteCategory;

public sealed class DeleteCategoryCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<DeleteCategoryCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == command.CategoryId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Category {command.CategoryId} not found.")
            {
                MessageKey = "Catalog.CategoryNotFound",
                MessageArgs = [command.CategoryId],
                ResourceSource = typeof(CatalogResources),
            };

        bool hasChildren = await dbContext.Categories
            .AnyAsync(c => c.ParentCategoryId == category.Id, cancellationToken)
            .ConfigureAwait(false);
        if (hasChildren)
        {
            throw new CustomException(
                "Cannot delete a category that has child categories. Move or remove the children first.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict)
            {
                MessageKey = "Catalog.CategoryHasChildren",
                ResourceSource = typeof(CatalogResources),
            };
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
