using FluentValidation;
using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Identity.Core.Tokens;
using FSH.Framework.Shared.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Identity.Endpoints.v1.Tokens;
public static class TokenGeneration
{
    public sealed record Command(string Email, string Password) : ICommand<Response>;
    public sealed record Response(string Token, string RefreshToken, DateTime RefreshTokenExpiryTime);
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(p => p.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .EmailAddress();

            RuleFor(p => p.Password)
                .Cascade(CascadeMode.Stop)
                .NotEmpty();
        }
    }
    internal static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", async (
            Command command,
            string tenant,
            ICommandDispatcher dispatcher,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync<Command, Response>(command, cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(TokenGeneration))
        .WithSummary("Generate JWTs")
        .WithDescription("Generates access and refresh tokens.")
        .AllowAnonymous();
    }

    public sealed class Handler(ITokenService tokenService, HttpContext context) : ICommandHandler<Command, Response>
    {
        public async Task<Response> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            var request = new TokenGenerationRequest(command.Email, command.Password);
            string ip = context.GetIpAddress();
            var token = await tokenService.GenerateTokenAsync(request, ip, cancellationToken);
            return new Response(token.Token, token.RefreshToken, token.RefreshTokenExpiryTime);
        }
    }
}
