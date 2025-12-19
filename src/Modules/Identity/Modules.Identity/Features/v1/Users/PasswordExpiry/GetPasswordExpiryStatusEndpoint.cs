using FSH.Modules.Identity.Contracts.v1.Users.PasswordExpiry;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users.PasswordExpiry;

public static class GetPasswordExpiryStatusEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/password-expiry-status", Handle)
            .WithName(nameof(GetPasswordExpiryStatusEndpoint))
            .WithOpenApi()
            .WithSummary("Get the current user's password expiry status")
            .WithDescription("Returns information about password expiry, warning status, and days remaining")
            .Produces<PasswordExpiryStatusDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<PasswordExpiryStatusDto> Handle(IMediator mediator, CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetPasswordExpiryStatusQuery(), cancellationToken);
    }
}
