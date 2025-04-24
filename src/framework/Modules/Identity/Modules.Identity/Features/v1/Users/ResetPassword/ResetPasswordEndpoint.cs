using FluentValidation;
using FluentValidation.Results;
using FSH.Framework.Identity.Core.Users;
using FSH.Framework.Identity.Endpoints.v1.Users.ResetPassword;
using FSH.Modules.Common.Shared.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Users.Endpoints;

public static class ResetPasswordEndpoint
{
    internal static RouteHandlerBuilder MapResetPasswordEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/reset-password",
            async ([FromBody] ResetPasswordCommand command,
            [FromHeader(Name = MutiTenancyConstants.Identifier)] string tenant,
            IValidator<ResetPasswordCommand> validator,
            IUserService userService, CancellationToken cancellationToken) =>
        {
            ValidationResult result = await validator.ValidateAsync(command, cancellationToken);
            if (!result.IsValid)
            {
                return Results.ValidationProblem(result.ToDictionary());
            }

            await userService.ResetPasswordAsync(command.Email, command.Password, command.Token, cancellationToken);
            return Results.Ok("Password has been reset.");
        })
        .WithName(nameof(ResetPasswordEndpoint))
        .WithSummary("Reset password")
        .WithDescription("Resets the password using the token and new password provided.")
        .AllowAnonymous();
    }

}