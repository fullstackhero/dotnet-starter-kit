using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Frontend;
using FSH.Framework.Web.Idempotency;
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
            IFrontendOriginResolver originResolver,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            // Operator-driven flow: an admin registers a tenant user, so the confirmation link must
            // land on the recipient's app (the default front-end), not the operator's Origin.
            command.Origin = originResolver.ResolveDefault();
            var result = await mediator.Send(command, cancellationToken);
            return TypedResults.Created($"/api/v1/identity/users/{result.UserId}", result);
        })
        .WithName("RegisterUser")
        .WithSummary("Register user")
        .RequirePermission(IdentityPermissions.Users.Create)
        .WithIdempotency()
        .WithDescription("Create a new user account.")
        .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status400BadRequest);
    }
}