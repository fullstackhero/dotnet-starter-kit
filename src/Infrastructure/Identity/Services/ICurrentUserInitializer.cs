using System.Security.Claims;

namespace DN.WebApi.Infrastructure.Identity.Services;

public interface ICurrentUserInitializer
{
    void SetCurrentUser(ClaimsPrincipal user);

    void SetCurrentUserId(string userId);
}