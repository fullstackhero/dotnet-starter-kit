using System.Security.Claims;

namespace FSH.Framework.Core.ExecutionContext;
public interface ICurrentUserInitializer
{
    void SetCurrentUser(ClaimsPrincipal user);

    void SetCurrentUserId(string userId);
}