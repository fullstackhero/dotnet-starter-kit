using FSH.Framework.Core.Exceptions;
using FSH.Framework.Identity.Contracts.v1.Users.UpdateUser;
using FSH.Framework.Identity.Core.Users;
using FSH.Framework.Shared.Authorization;
using FSH.Framework.Shared.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace FSH.Framework.Infrastructure.Identity.Users.Endpoints;
public static class UpdateUserEndpoint
{
    internal static RouteHandlerBuilder MapUpdateUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/profile", ([FromBody] UpdateUserCommand request, ClaimsPrincipal user, IUserService service) =>
        {
            if (user.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException();
            }
            return service.UpdateAsync(request.Id,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.Image,
                request.DeleteCurrentImage);
        })
        .WithName(nameof(UpdateUserEndpoint))
        .WithSummary("update user profile")
        .RequirePermission("Permissions.Users.Update")
        .WithDescription("update user profile");
    }
}