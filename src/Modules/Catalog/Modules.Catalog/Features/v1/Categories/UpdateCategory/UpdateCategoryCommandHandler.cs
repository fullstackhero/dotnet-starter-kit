using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Categories;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<UpdateCategoryCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
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

        if (command.ParentCategoryId is { } parentId)
        {
            if (parentId == category.Id)
            {
                throw new CustomException(
                    "A category cannot be its own parent.",
                    (IEnumerable<string>?)null,
                    HttpStatusCode.BadRequest)
                {
                    MessageKey = "Catalog.CategoryCannotBeOwnParent",
                    ResourceSource = typeof(CatalogResources),
                };
            }

            // Walk parent chain to detect cycles (parent → ancestor of self)
            var visited = new HashSet<Guid> { category.Id };
            Guid? cursor = parentId;
            while (cursor is { } cur)
            {
                if (!visited.Add(cur))
                {
                    throw new CustomException(
                        "Setting this parent would create a cycle.",
                        (IEnumerable<string>?)null,
                        HttpStatusCode.BadRequest)
                    {
                        MessageKey = "Catalog.CategoryParentCycle",
                        ResourceSource = typeof(CatalogResources),
                    };
                }
                cursor = await dbContext.Categories
                    .Where(c => c.Id == cur)
                    .Select(c => c.ParentCategoryId)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        category.Update(command.Name, command.Description, command.ParentCategoryId);

        bool slugTaken = await dbContext.Categories
            .AnyAsync(c => c.Slug == category.Slug && c.Id != category.Id, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"Another category with name '{command.Name}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict)
            {
                MessageKey = "Catalog.AnotherCategoryNameAlreadyExists",
                MessageArgs = [command.Name],
                ResourceSource = typeof(CatalogResources),
            };
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return category.Id;
    }
}
