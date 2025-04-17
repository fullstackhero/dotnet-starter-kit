using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Identity.Contracts.v1.Users.AssignUserRoles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Identity.v1.Users.AssignUserRoles;
public static class AssignUserRolesEndpoint
{
    internal static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/{id:guid}/roles", async (AssignUserRolesCommand command,
            HttpContext context,
            string id,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName(nameof(AssignUserRolesEndpoint))
        .WithSummary("assign roles")
        .WithDescription("assign roles");
    }
}