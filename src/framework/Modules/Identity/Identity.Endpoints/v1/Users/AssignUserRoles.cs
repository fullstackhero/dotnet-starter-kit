using FSH.Framework.Core.Identity.Users.Dtos;
using FSH.Framework.Core.Messaging.CQRS;
using FSH.Framework.Identity.Core.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Identity.Endpoints.v1.Users;
public static class AssignUserRoles
{
    public class Command : ICommand<string>
    {
        public required string UserId { get; init; }
        public IReadOnlyList<UserRoleDetailDto> UserRoles { get; init; } = Array.Empty<UserRoleDetailDto>();
    }
    internal class Handler(IUserService _userService) : ICommandHandler<Command, string>
    {
        public async Task<string> HandleAsync(Command request, CancellationToken cancellationToken = default) =>
            await _userService.AssignRolesAsync(request.UserId, request.UserRoles, cancellationToken);
    }
    internal static RouteHandlerBuilder MapEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/{id:guid}/roles", async (Command command,
            HttpContext context,
            string id,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithName(nameof(AssignUserRoles))
        .WithSummary("assign roles")
        .WithDescription("assign roles");
    }
}
