using FluentValidation;
using FluentValidation.Results;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Core.Identity.Users.Features.ForgotPassword;
using FSH.Framework.Core.Tenant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Users.Endpoints;

public static class ForgotPasswordEndpoint
{
    internal static RouteHandlerBuilder MapForgotPasswordEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/forgot-password", async (HttpRequest request, [FromHeader(Name = TenantConstants.Identifier)] string tenant, ForgotPasswordCommand command, IValidator<ForgotPasswordCommand> validator, IUserService userService, CancellationToken cancellationToken) =>
        {
            ValidationResult result = await validator.ValidateAsync(command, cancellationToken);
            if (!result.IsValid)
            {
                return Results.ValidationProblem(result.ToDictionary());
            }

            // Obtain origin from request headers
            var origin = request.Headers["Origin"].ToString();
            if (string.IsNullOrEmpty(origin))
            {
                origin = "http://defaultOrigin.com"; // Default value if Origin header is not present
            }

            await userService.ForgotPasswordAsync(command, origin, cancellationToken);
            return Results.Ok("Password reset email sent.");
        })
        .WithName(nameof(ForgotPasswordEndpoint))
        .WithSummary("Forgot password")
        .WithDescription("Generates a password reset token and sends it via email.")
        .AllowAnonymous();
    }

}
