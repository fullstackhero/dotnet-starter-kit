using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Web.Frontend;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Identity.Contracts.v1.Users.RegisterUser;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.SelfRegistration;

public static class SelfRegisterUserEndpoint
{
    internal static RouteHandlerBuilder MapSelfRegisterUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/self-register", async (RegisterUserCommand command,
            [FromHeader(Name = MultitenancyConstants.Identifier)] string tenant,
            IFrontendOriginResolver originResolver,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            // Self-service flow: the confirmation link lands on the SPA the user registered from.
            command.Origin = originResolver.ResolveForCurrentRequest();
            var result = await mediator.Send(command, cancellationToken);
            return TypedResults.Created($"/api/v1/identity/users/{result.UserId}", result);
        })
        .WithName("SelfRegisterUser")
        .WithSummary("Self register user")
        .WithDescription("Allow a user to self-register. Anonymous; tenant identified via the tenant header.")
        .AllowAnonymous()
        .WithIdempotency()
        .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
