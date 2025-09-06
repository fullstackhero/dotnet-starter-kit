using System.Security.Claims;

namespace FSH.Framework.Core.Identity.Users.Abstractions;
public interface ICurrentUserInitializer
{
    void SetCurrentUser(ClaimsPrincipal user);

    void SetCurrentUserId(string userId);
}
