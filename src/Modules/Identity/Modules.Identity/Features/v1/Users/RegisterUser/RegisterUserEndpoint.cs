using FSH.Framework.Shared.Identity;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.v1.Users.RegisterUser;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.RegisterUser;

public static class RegisterUserEndpoint
{
    internal static RouteHandlerBuilder MapRegisterUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/register", async (RegisterUserCommand command,
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var origin = $"{context.Request.Scheme}://{context.Request.Host.Value}{context.Request.PathBase.Value}";
            command.Origin = origin;
            var result = await mediator.Send(command, cancellationToken);
            // TODO: Return TypedResults.Created() once the Blazor NSwag client is regenerated
            // with a config that maps POST /register to 201 Created.
            return TypedResults.Ok(result);
        })
        .WithName("RegisterUser")
        .WithSummary("Register user")
        .RequirePermission(IdentityPermissionConstants.Users.Create)
        .WithDescription("Create a new user account.")
        .Produces<RegisterUserResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
