using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Impersonation.EndImpersonation;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Impersonation.EndImpersonation;

public static class EndImpersonationEndpoint
{
    internal static RouteHandlerBuilder MapEndImpersonationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/impersonation/end",
            [Authorize] async Task<Results<Ok<TokenResponse>, ProblemHttpResult>>
            ([FromServices] IMediator mediator,
             CancellationToken ct) =>
            {
                var token = await mediator.Send(new EndImpersonationCommand(), ct);
                return TypedResults.Ok(token);
            })
            .WithName("EndImpersonation")
            .WithSummary("End user impersonation")
            .WithDescription("Returns a fresh access + refresh token for the original actor based on the act_sub/act_tenant claims embedded in the impersonation token. Callable by any authenticated impersonation session.")
            .Produces<TokenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
