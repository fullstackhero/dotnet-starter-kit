using FluentValidation;
using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Identity.Core.Tokens;
using FSH.Framework.Shared.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Identity.Endpoints.v1.Tokens;
public static class RefreshToken
{
    public sealed record Command(string Token, string RefreshToken) : ICommand<Response>;
    public sealed record Response(string Token, string RefreshToken, DateTime RefreshTokenExpiryTime);
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(p => p.Token).Cascade(CascadeMode.Stop).NotEmpty();
            RuleFor(p => p.RefreshToken).Cascade(CascadeMode.Stop).NotEmpty();
        }
    }
    internal static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/refresh", async (Command command,
            string tenant,
            ICommandDispatcher dispatcher,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync(command, cancellationToken);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(RefreshToken))
        .WithSummary("refresh JWTs")
        .WithDescription("refresh JWTs")
        .AllowAnonymous();
    }
    public sealed class Handler(ITokenService tokenService, HttpContext context) : ICommandHandler<Command, Response>
    {
        public async Task<Response> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            var request = new TokenRefreshRequest(command.Token, command.RefreshToken);
            string ip = context.GetIpAddress();
            var token = await tokenService.RefreshTokenAsync(request, ip, cancellationToken);
            return new Response(token.Token, token.RefreshToken, token.RefreshTokenExpiryTime);
        }
    }
}
