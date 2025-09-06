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

namespace Category.Features.Create.v1;
 
public static class CreateCategoryItemEndPoint
{
    internal static RouteHandlerBuilder MapCategoryItemCreationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", async (CreateCategoryItemCommand request, ISender mediator) =>
        {
            var response = await mediator.Send(request);
            return Results.CreatedAtRoute(nameof(CreateCategoryItemEndPoint), new { id = response.Id }, response);
        })
                .WithName(nameof(CreateCategoryItemEndPoint))
                .WithSummary("Creates a CategoryItem item")
                .WithDescription("Creates a CategoryItem item")
                .Produces<CreateCategoryItemResponse>(StatusCodes.Status201Created)
                .RequirePermission("Permissions.CategoryItems.Create")
                .MapToApiVersion(new ApiVersion(1, 0));

    }
}
