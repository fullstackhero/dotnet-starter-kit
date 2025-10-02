using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Category.Features.Delete.v1;
 
public static class DeleteCategoryItemEndpoint
{
    internal static RouteHandlerBuilder MapCategoryItemDeletionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapDelete("/{id:guid}", async (Guid id, ISender mediator) =>
            {
                await mediator.Send(new DeleteCategoryItemCommand(id));
                return Results.NoContent();
            })
            .WithName(nameof(DeleteCategoryItemEndpoint))
            .WithSummary("Deletes a category item")
            .WithDescription("Deleted a category item")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("Permissions.CategoryItems.Delete")
            .MapToApiVersion(new ApiVersion(1, 0));

    }
}
