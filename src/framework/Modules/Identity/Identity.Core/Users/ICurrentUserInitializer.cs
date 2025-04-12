using System.Security.Claims;

namespace FSH.Framework.Identity.Core.Users;
public interface ICurrentUserInitializer
{
    void SetCurrentUser(ClaimsPrincipal user);

    void SetCurrentUserId(string userId);
}
